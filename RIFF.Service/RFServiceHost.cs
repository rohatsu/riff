// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using log4net.Config;
using RIFF.Core;
using System;
using System.Diagnostics;
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

        public RFServiceHost()
        {
            InitializeComponent();
            rfEventLog.Source = "RIFF";
            rfEventLog.Log = "Application";
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

                if (RFSettings.GetAppSetting("UseMSMQ", true))
                {
                    CleanUpMSMQ(Environment.MachineName, engine.Environment);
                }

                _environment = RFEnvironments.StartLocal(engine.Environment, engineConfig, engine.Database, new string[] { engine.Assembly });
                _context = _environment.Start();

                var wcfService = new RFService(_context, engineConfig, engine.Database);
                _serviceHost = new ServiceHost(wcfService);
                _serviceHost.Open();
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
