// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com

namespace RIFF.Core
{
    public interface IRFEnvironment
    {
        IRFSystemContext Start();

        void Stop();
    }
}
