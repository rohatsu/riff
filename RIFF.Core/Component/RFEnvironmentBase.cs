// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com

namespace RIFF.Core
{
    internal abstract class RFEnvironmentBase : IRFEnvironment
    {
        protected RFComponentContext _context;

        public abstract IRFSystemContext Start();

        public virtual void Stop()
        {
        }
    }
}
