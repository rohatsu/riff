// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
namespace RIFF.Core
{
    public abstract class RFSubGraph
    {
        public string GraphName { get; private set; }

        protected string _connectionString;
        protected RFEngineDefinition _engineConfig;
        protected RFGraphDefinition _graph;
        protected IRFUserConfig _userConfig;

        protected RFSubGraph(string graphName, RFEngineDefinition engineConfig, IRFUserConfig userConfig, string connectionString)
        {
            _engineConfig = engineConfig;
            _userConfig = userConfig;
            _connectionString = connectionString;
            GraphName = graphName;
        }

        public abstract void AddProcesses();

        public void AddToEngine()
        {
            // create a graph within the engine
            _graph = _engineConfig.CreateGraph(GraphName);

            AddProcesses();
        }
    }
}
