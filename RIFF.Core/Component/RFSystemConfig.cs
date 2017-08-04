// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com

namespace RIFF.Core
{
    internal class RFSystemConfig
    {
        public string DocumentStoreConnectionString { get; set; }

        public RFSchedulerRange Downtime { get; set; }
        public string Environment { get; set; }

        public int IntervalLength { get; set; }

        public RFProcessingMode ProcessingMode { get; set; }
    }
}
