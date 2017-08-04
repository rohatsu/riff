// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com

namespace RIFF.Core
{
    public interface IRFEngineBuilder
    {
        RFEngineDefinition BuildEngine(string database, string environment);
    }
}
