// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System.Collections.Generic;

namespace RIFF.Core
{
    internal abstract class RFEventReactor
    {
        public abstract List<RFInstruction> React(RFEvent e);
    }

    /*
    internal class RFIntervalReactor : RFEventReactor
    {
        protected IRFReadingContext _context;
        protected RFCatalogKey _intervalKey;

        public RFIntervalReactor(RFCatalogKey intervalKey, IRFReadingContext context)
        {
            _context = context;
            _intervalKey = intervalKey;
        }

        public override List<RFInstruction> React(RFEvent e)
        {
            if (e is RFIntervalEvent)
            {
                return new List<RFInstruction> { new RFIntervalInstruction(e as RFIntervalEvent) };
            }
            return null;
        }
    }*/

    internal class RFTriggerReactor : RFEventReactor
    {
        public IRFEngineTrigger Trigger { get; private set; }
        protected IRFReadingContext _context;

        public RFTriggerReactor(IRFEngineTrigger trigger, IRFReadingContext context)
        {
            _context = context;
            Trigger = trigger;
        }

        public override List<RFInstruction> React(RFEvent e)
        {
            return Trigger.React(e);
        }
    }
}
