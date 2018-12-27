// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System;

namespace RIFF.Core
{
    internal abstract class RFPassiveComponent
    {
        public IRFLog Log { get { return RFStatic.Log; } }

        protected string _componentID;

        protected RFComponentContext _context;

        public RFPassiveComponent(RFComponentContext context)
        {
            _context = context;
            _componentID = RFComponentCounter.GetNextComponentID();
        }

        public override string ToString()
        {
            return String.Format("{0}:{1}", _componentID, GetType().Name);
        }
    }
}
