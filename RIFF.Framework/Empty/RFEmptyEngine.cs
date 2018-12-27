// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;

namespace RIFF.Framework
{
    public class RFEmptyEngine : IRFEngineBuilder
    {
        public RFEngineDefinition BuildEngine(string database, string environment = "")
        {
            // define a key root for all objects
            var keyDomain = new RFSimpleKeyDomain("empty");

            // create the engine
            var engineConfig = RFEngineDefinition.Create("emptyengine", keyDomain);
            var graph = engineConfig.CreateGraph("emptygraph");
            graph.AddProcess("emptyprocess", "Dummy process.", () => new RFNullProcessor());

            return engineConfig;
        }
    }
}
