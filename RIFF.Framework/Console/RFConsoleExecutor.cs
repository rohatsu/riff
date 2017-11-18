// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RIFF.Framework
{
    public class RFConsoleExecutor
    {
        public bool _isExiting;
        private readonly RFEngineDefinition _config;
        private readonly IRFProcessingContext _context;
        private readonly EngineConfigElement _engine;
        private readonly IRFEngineConsole _engineConsole;

        public RFConsoleExecutor(
            RFEngineDefinition config,
            IRFProcessingContext context,
            EngineConfigElement engine,
            IRFEngineConsole engineConsole)
        {
            _config = config;
            _context = context;
            _engine = engine;
            _engineConsole = engineConsole;
            _isExiting = false;
        }

        public void ExecuteCommand(string input)
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                var tokens = new RIFF.Interfaces.Formats.CSV.CSVParser(input, ' ').Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();

                switch (tokens[0])
                {
                    case "importupdates":
                        {
                            if (tokens.Length < 2)
                            {
                                Console.WriteLine("Usage: importupdates,<path>");
                                break;
                            }

                            var c = RFCatalogMaintainer.ImportCatalogUpdates(_context, tokens[1]);
                            Console.WriteLine("Imported {0} documents", c);
                            break;
                        }
                    case "exportupdates":
                        {
                            if (tokens.Length < 3)
                            {
                                Console.WriteLine("Usage: exportupdates,<startDate>,<path>");
                                break;
                            }
                            var startDate = RFDate.Parse(tokens[1], "yyyy-MM-dd");

                            var c = RFCatalogMaintainer.ExportCatalogUpdates(_context, tokens[2], startDate, null, null);
                            Console.WriteLine("Exported {0} documents", c);
                            break;
                        }
                    case "run":
                    case "runsequential":
                        {
                            if (tokens.Length == 1)
                            {
                                Console.WriteLine("Usage: run,<fullProcessName>,<graphInstance>,<startDate>,[endDate]");
                                break;
                            }
                            var processName = tokens[1];
                            if (tokens.Length > 2)
                            {
                                var graphInstanceName = tokens[2];
                                var startDate = RFDate.Parse(tokens[3], "yyyy-MM-dd");
                                var endDate = startDate;
                                if (tokens.Length > 4)
                                {
                                    endDate = RFDate.Parse(tokens[4], "yyyy-MM-dd");
                                }
                                var instructions = new List<RFInstruction>();
                                while (startDate <= endDate)
                                {
                                    var graphInstance = new RFGraphInstance
                                    {
                                        Name = graphInstanceName,
                                        ValueDate = startDate
                                    };
                                    instructions.Add(new RFGraphProcessInstruction(graphInstance, processName));
                                    startDate = startDate.OffsetDays(1);
                                }
                                var ra = new RFRequestActivity(_context, _config);
                                var tracker = ra.Submit(null, instructions, null);
                                while (!tracker.IsComplete)
                                {
                                    Thread.Sleep(100);
                                }
                                Console.WriteLine("Finished: #{0} cycles, #{1} keys, last run {2}.", tracker.FinishedCycles, tracker.KeyCount, tracker.CurrentProcess);
                                foreach (var message in tracker.Messages)
                                {
                                    Console.WriteLine("Message: {0}: {1}", message.Key, message.Value);
                                }
                            }
                            else
                            {
                                // non-graph
                                var instruction = new RFParamProcessInstruction(
                                    processName, new RFEngineProcessorKeyParam(RFGenericCatalogKey.Create(_config.KeyDomain, "dummy", "dummy", null)));

                                var ra = new RFRequestActivity(_context, _config);
                                var tracker = ra.Submit(null, new List<RFInstruction> { instruction }, null);
                                while (!tracker.IsComplete)
                                {
                                    Thread.Sleep(100);
                                }
                                Console.WriteLine("Finished: #{0} cycles, #{1} keys, last run {2}.", tracker.FinishedCycles, tracker.KeyCount, tracker.CurrentProcess);
                                foreach (var message in tracker.Messages)
                                {
                                    Console.WriteLine("Message: {0}: {1}", message.Key, message.Value);
                                }
                            }
                            break;
                        }
                    case "error":
                        {
                            _context.SystemLog.Error(this, "System Log error message");
                            _context.SystemLog.Exception(this, "System Log exception message", new Exception("Test exception"));
                            break;
                        }
                    case "version":
                        {
                            var runLicense = RFPublicRSA.GetHost(_config.LicenseTokens.Key, _config.LicenseTokens.Value);
                            Console.WriteLine("RIFF Framework {0} | (c) rohatsu software studios limited | www.rohatsu.com", RFCore.sVersion);
                            Console.WriteLine("Licensed to '{0}' ({1})", runLicense.Key, runLicense.Value.ToString(RFCore.sDateFormat));
                            Console.WriteLine("Loaded engine {0} from {1} in environment {2}", _engine?.EngineName, _engine?.Assembly, _engine.Environment);
                            break;
                        }
                    case "email":
                        {
                            if (tokens.Length > 1)
                            {
                                var e = new RFGenericEmail(new RFEmailConfig
                                {
                                    Enabled = true,
                                    To = tokens[1]
                                }, string.Format("<html><body>Test email from RIFF System.<p/>Sent on {0} from {1}.</body></html>", DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss \"GMT\"zzz"), Environment.MachineName));
                                e.Send("RIFF Test e-mail");
                            }
                            else
                            {
                                Console.WriteLine("provide email address as parameter");
                            }
                        }
                        break;
                    case "rebuildgraph":
                        {
                            if (tokens.Length < 4)
                            {
                                Console.WriteLine("Usage: rebuild,<graphNameOrBlank>,<graphInstance>,<startDate>,[endDate]");
                                break;
                            }
                            var graphName = tokens[1];
                            var graphInstanceName = tokens[2];
                            if (tokens.Length > 3)
                            {
                                var startDate = RFDate.Parse(tokens[3], "yyyy-MM-dd");
                                var endDate = startDate;
                                if (tokens.Length > 4)
                                {
                                    endDate = RFDate.Parse(tokens[4], "yyyy-MM-dd");
                                }
                                var instructions = new List<RFInstruction>();

                                // queue all graph processes
                                foreach (var vd in RFDate.Range(startDate, endDate, d => true))
                                {
                                    var instance = new RFGraphInstance
                                    {
                                        Name = graphInstanceName,
                                        ValueDate = vd
                                    };

                                    foreach (var g in this._config.Graphs.Values.Where(g => graphName.IsBlank() || g.GraphName.StartsWith(graphName, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        foreach (var gp in g.Processes.Values)
                                        {
                                            var processName = RFGraphDefinition.GetFullName(gp.GraphName, gp.Name);
                                            instructions.Add(new RFGraphProcessInstruction(instance, processName));
                                        }
                                    }
                                }

                                if (instructions.Any())
                                {
                                    var tracker = new RFRequestActivity(_context, _config).Submit(null, instructions, null);
                                    while (!tracker.IsComplete)
                                    {
                                        Thread.Sleep(100);
                                    }
                                    Console.WriteLine("Finished: #{0} cycles, #{1} keys, last run {2}.", tracker.FinishedCycles, tracker.KeyCount, tracker.CurrentProcess);
                                    foreach (var message in tracker.Messages)
                                    {
                                        Console.WriteLine("Message: {0}: {1}", message.Key, message.Value);
                                    }
                                }
                            }
                        }
                        break;

                    case "exit":
                    case "quit":
                        _isExiting = true;
                        break;

                    default:
                        {
                            if (_engineConsole != null)
                            {
                                var queueCommands = new List<string>();
                                if (!_engineConsole.RunCommand(tokens, queueCommands))
                                {
                                    Console.WriteLine(String.Format("Unrecognized command '{0}'", tokens[0]));
                                }
                                foreach (var c in queueCommands)
                                {
                                    ExecuteCommand(c);
                                }
                            }
                        }
                        break;
                }
            }
        }
    }
}
