// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RIFF.Core
{
    [DataContract]
    public class RFSingleCommandTrigger : IRFEngineTrigger
    {
        [IgnoreDataMember]
        protected Func<RFEvent, RFInstruction> _triggerFunc;

        public RFSingleCommandTrigger(Func<RFEvent, RFInstruction> triggerFunc)
        {
            _triggerFunc = triggerFunc;
        }

        public List<RFInstruction> React(RFEvent e)
        {
            var instructions = new List<RFInstruction>();
            var i = _triggerFunc(e);
            if (i != null)
            {
                instructions.Add(i);
            }
            return instructions;
        }
    }
}
