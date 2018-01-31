// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using log4net.Config;
using RIFF.Core;
using RIFF.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Messaging;
using System.Security;
using System.ServiceModel;
using System.ServiceProcess;

namespace RIFF.Service
{
    public partial class RFServiceHost : ServiceBase
    {
        protected IRFSystemContext _context;
        protected IRFEnvironment _environment;
        protected ServiceHost _serviceHost;
        protected string[] _args;

        public RFServiceHost(string[] args)
        {
            InitializeComponent();
            rfEventLog.Source = "RIFF";
            rfEventLog.Log = "Application";
            _args = args;
        }

        public void StartEnvironment()
        {
            try
            {
                Console.WriteLine("Welcome to RIFF {0}", RFCore.sVersion);

                // currently only a single engine is supported
                var engine = RIFFSection.GetDefaultEngine();
                var engineConfig = engine.BuildEngineConfiguration();

                try
                {
                    rfEventLog.WriteEntry(String.Format("Starting engine {0} in environment {1} from {2} (RFCore {3})", engine.EngineName, engine.Environment, engine.Assembly, RFCore.sVersion), EventLogEntryType.Information);
                }
                catch (SecurityException)
                {
                    RFStatic.Log.Error(this, "EventLog source has not been created. Please run \"RIFF.Service.exe /install\" as Administrator to create.");
                    return;
                }

                if (_args != null && _args.Length > 0)
                {
                    _environment = RFEnvironments.StartConsole(engine.Environment, engineConfig, engine.Database, new string[] { engine.Assembly });
                    _context = _environment.Start();

                    if (_args[0] == "command")
                    {
                        // run console command
                        var engineConsole = engineConfig.Console;
                        if (engineConsole != null)
                        {
                            engineConsole.Initialize(_context, engineConfig, engine.Database);
                        }
                        var executor = new RFConsoleExecutor(engineConfig, _context, engine, engineConsole);
                        executor.ExecuteCommand(String.Join(" ", _args.Skip(1)));
                    }
                    else
                    {
                        // run named service
                        var param = String.Join(" ", _args);
                        var tokens = new Interfaces.Formats.CSV.CSVParser(param, ' ').Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
                        var serviceName = tokens[0];
                        var serviceParam = tokens.Length > 1 ? tokens[1] : null;
                        RFStatic.Log.Info(this, $"Starting service: {serviceName}" + (serviceParam != null ? $"with param: {serviceParam}" : string.Empty));

                        _context.RaiseEvent(this, new RFServiceEvent { ServiceName = serviceName, ServiceCommand = "start", ServiceParams = serviceParam });
                    }
                }
                else
                {
                    if (RFSettings.GetAppSetting("UseMSMQ", true))
                    {
                        CleanUpMSMQ(Environment.MachineName, engine.Environment);
                    }

                    // WCF service
                    _environment = RFEnvironments.StartLocal(engine.Environment, engineConfig, engine.Database, new string[] { engine.Assembly });
                    _context = _environment.Start();

                    _context.RaiseEvent(this, new RFServiceEvent { ServiceName = RFSchedulerService.SERVICE_NAME, ServiceCommand = "start", ServiceParams = null });

                    var wcfService = new RFService(_context, engineConfig, engine.Database);
                    _serviceHost = new ServiceHost(wcfService);
                    _serviceHost.Open();
                }
            }
            catch (Exception ex)
            {
                rfEventLog.WriteEntry("OnStart Error: " + ex.Message, EventLogEntryType.Error);
                RFStatic.Log.Exception(this, ex, "Error initializing RFService.");
                throw;
            }
        }

        protected void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (RFStatic.Log != null)
            {
                if (e != null)
                {
                    if (e.IsTerminating)
                    {
                        RFStatic.Log.Critical(this, "Unhandled Exception - shutting down: {0}", e.ExceptionObject != null ? e.ExceptionObject.ToString() : "?");
                    }
                    else
                    {
                        RFStatic.Log.Critical(this, "Unhandled Exception - recovered: {0}", e.ExceptionObject != null ? e.ExceptionObject.ToString() : "?");
                    }
                }
                else
                {
                    RFStatic.Log.Critical(this, "Unhandled Exception without additional info");
                }
            }
        }

        protected override void OnStart(string[] args)
        {
            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            log4net.GlobalContext.Properties["LogName"] = _args != null && _args.Length > 0 ? _args[0] : "Service";
            XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo("log4net.config"));
            StartEnvironment();
        }

        protected override void OnStop()
        {
            rfEventLog.WriteEntry("Stopping", EventLogEntryType.Information);
            StopEnvironment();
            if (_serviceHost != null)
            {
                _serviceHost.Close();
            }
        }

        protected void StopEnvironment()
        {
            try
            {
                _environment.Stop();
            }
            catch (Exception ex)
            {
                RFStatic.Log.Exception(this, "Error stopping service", ex);
            }
        }

        private void CleanUpMSMQ(string machineName, string environment)
        {
            try
            {
                foreach (var queue in MessageQueue.GetPrivateQueuesByMachine(machineName))
                {
                    if (queue.QueueName.Contains("riff_") && queue.QueueName.EndsWith("_" + environment, StringComparison.InvariantCultureIgnoreCase))
                    {
                        try
                        {
                            MessageQueue.Delete(queue.Path);
                        }
                        catch (Exception ex)
                        {
                            RFStatic.Log.Warning(this, "Delete Queue: + ", ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RFStatic.Log.Warning(this, "CleanUpMSMQ: " + ex.Message);
            }
        }
    }
}
