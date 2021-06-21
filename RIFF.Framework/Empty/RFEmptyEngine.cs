// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System.Collections.Generic;

namespace RIFF.Framework
{
    public class RFEmptyConsole : IRFEngineConsole
    {
        public void Initialize(IRFProcessingContext context, RFEngineDefinition engineConfig, string connectionString)
        {
        }

        public bool RunCommand(string[] tokens, List<string> queueCommands)
        {
            return false;
        }
    }

    public class RFEmptyProcess : RFEngineProcessor<RFEngineProcessorGraphInstanceParam>
    {
        private int _timeout;

        public RFEmptyProcess(int timeout)
        {
            _timeout = timeout;
        }

        public override RFProcessingResult Process()
        {
            Log.Info($">> Starting ${ProcessName} ({_timeout}s)");
            System.Threading.Thread.Sleep(_timeout * 1000);
            Log.Info($">> Finishing ${ProcessName}");
            return RFProcessingResult.Success(true);
        }
    }

    public class RFEmptyEngine : IRFEngineBuilder
    {
        public RFEngineDefinition BuildEngine(string database, string environment = "")
        {
            // define a key root for all objects
            var keyDomain = new RFSimpleKeyDomain("empty");

            // create the engine
            var engineConfig = RFEngineDefinition.Create("emptyengine", keyDomain);
            engineConfig.Console = new RFEmptyConsole();
            var graph = engineConfig.CreateGraph("emptygraph");
            graph.AddProcess("emptyprocess", "Dummy process.", () => new RFNullProcessor());

            engineConfig.AddScheduledTask("Task 1", new RFIntervalSchedule(System.TimeSpan.FromMinutes(1), System.TimeSpan.FromSeconds(0)).Single(), RFWeeklyWindow.AllWeek(), engineConfig.AddProcess("Process 1", string.Empty, () => new RFEmptyProcess(30)), false);
            engineConfig.AddScheduledTask("Task 2", new RFIntervalSchedule(System.TimeSpan.FromMinutes(1), System.TimeSpan.FromSeconds(1)).Single(), RFWeeklyWindow.AllWeek(), engineConfig.AddProcess("Process 2", string.Empty, () => new RFEmptyProcess(3)), false);
            engineConfig.AddScheduledTask("Task 3", new RFIntervalSchedule(System.TimeSpan.FromMinutes(1), System.TimeSpan.FromSeconds(2)).Single(), RFWeeklyWindow.AllWeek(), engineConfig.AddProcess("Process 3", string.Empty, () => new RFEmptyProcess(3)), false);
            engineConfig.AddScheduledTask("Task 4", new RFIntervalSchedule(System.TimeSpan.FromMinutes(1), System.TimeSpan.FromSeconds(3)).Single(), RFWeeklyWindow.AllWeek(), engineConfig.AddProcess("Process 4", string.Empty, () => new RFEmptyProcess(3)), false);
            engineConfig.AddScheduledTask("Task 5", new RFIntervalSchedule(System.TimeSpan.FromMinutes(1), System.TimeSpan.FromSeconds(4)).Single(), RFWeeklyWindow.AllWeek(), engineConfig.AddProcess("Process 5", string.Empty, () => new RFEmptyProcess(3)), false);
            engineConfig.AddScheduledTask("Task 6", new RFIntervalSchedule(System.TimeSpan.FromMinutes(1), System.TimeSpan.FromSeconds(5)).Single(), RFWeeklyWindow.AllWeek(), engineConfig.AddProcess("Process 6", string.Empty, () => new RFEmptyProcess(3)), false);

            return engineConfig;
        }
    }
}
