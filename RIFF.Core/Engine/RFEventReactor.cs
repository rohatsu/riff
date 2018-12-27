// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System.Collections.Generic;
using System.Runtime.Serialization;

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

    [DataContract]
    public class RFServiceEvent : RFEvent
    {
        public const string START_COMMAND = "start";
        public const string STOP_COMMAND = "stop";

        [DataMember]
        public string ServiceName { get; set; }

        [DataMember]
        public string ServiceCommand { get; set; }

        [DataMember]
        public string ServiceParams { get; set; }
    }

    internal class RFServiceReactor : RFEventReactor
    {
        public string ServiceName { get; set; }

        public RFBackgroundServiceComponent Service { get; set; }

        public override List<RFInstruction> React(RFEvent e)
        {
            if (e is RFServiceEvent se && se.ServiceName.Equals(ServiceName, System.StringComparison.OrdinalIgnoreCase))
            {
                return Service.Command(se.ServiceCommand.ToLower(), se.ServiceParams);
            }
            return null;
        }
    }
}
