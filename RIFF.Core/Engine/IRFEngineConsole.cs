// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System.Collections.Generic;

namespace RIFF.Core
{
    public interface IRFEngineConsole
    {
        void Initialize(IRFProcessingContext context, RFEngineDefinition engineConfig, string connectionString);

        bool RunCommand(string[] tokens, List<string> queueCommands);
    }
}
