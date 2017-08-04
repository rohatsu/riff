// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using System.Collections.Generic;

namespace RIFF.Framework
{
    public class RFRequestActivity : RFActivity
    {
        protected RFEngineDefinition _config;
        protected IRFProcessingContext _parentContext;

        public RFRequestActivity(IRFProcessingContext context, RFEngineDefinition config) : base(context, "system")
        {
            _config = config;
            _parentContext = context;
        }

        public RFProcessingTracker Run(bool isGraph, string processName, RFEngineProcessorParam parameters, RFUserLogEntry userLogEntry)
        {
            var instructions = new List<RFInstruction>();
            if (!isGraph)
            {
                instructions.Add(new RFParamProcessInstruction(processName, parameters));
            }
            else if (parameters is RFEngineProcessorGraphInstanceParam)
            {
                instructions.Add(new RFGraphProcessInstruction((parameters as RFEngineProcessorGraphInstanceParam).Instance, processName));
            }

            _parentContext.UserLog.LogEntry(userLogEntry);
            return _parentContext.SubmitRequest(null, instructions);
        }

        public RFProcessingTracker Submit(IEnumerable<RFCatalogEntry> inputs, RFUserLogEntry userLogEntry)
        {
            _parentContext.UserLog.LogEntry(userLogEntry);
            return _parentContext.SubmitRequest(inputs, null);
        }

        public RFProcessingTracker Submit(IEnumerable<RFCatalogEntry> inputs, IEnumerable<RFInstruction> instructions, RFUserLogEntry userLogEntry)
        {
            _parentContext.UserLog.LogEntry(userLogEntry);
            return _parentContext.SubmitRequest(inputs, instructions);
        }
    }
}
