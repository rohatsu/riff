// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System.Collections.Generic;

namespace RIFF.Core
{
    public abstract class RFEngineConsoleBase : IRFEngineConsole
    {
        protected string _connectionString;
        protected IRFProcessingContext _context;
        protected RFEngineDefinition _engineConfig;

        public void Initialize(IRFProcessingContext context, RFEngineDefinition engineConfig, string connectionString)
        {
            _context = context;
            _engineConfig = engineConfig;
            _connectionString = connectionString;

            Initialize();
        }

        public virtual void Initialize() { }
        public abstract bool RunCommand(string[] tokens, List<string> queueCommands);
    }
}
