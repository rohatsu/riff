// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using System.Collections.Generic;
using System.Threading;

namespace RIFF.Core
{
    /// <summary>
    /// Main context for all components, only one per process
    /// </summary>
    internal class RFComponentContext
    {
        public List<RFActiveComponent> ActiveComponents { get; set; }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        // infrastructure providers
        public IRFCatalog Catalog { get; set; }

        public IRFDispatchStore DispatchStore { get; set; }
        public IRFEngine Engine { get; set; }

        public IRFLog Log { get { return RFStatic.Log; } }

        // ephemeral store
        public Dictionary<string, RFCatalogEntry> MemoryStore { get; set; } // TODO: distribute

        public RFSystemConfig SystemConfig { get; set; }

        // configuration
        public IRFUserConfig UserConfig { get; set; }

        public IRFUserLog UserLog { get; set; }

        public IRFUserRole UserRole { get; set; }

        public RFComponentContext()
        {
            ActiveComponents = new List<RFActiveComponent>();
            MemoryStore = new Dictionary<string, RFCatalogEntry>();
        }

        public RFProcessingContext GetProcessingContext(string processingKey, IRFInstructionSink instructionManager, IRFEventSink eventManager, RFDispatchQueueMonitorBase workQueueMonitor)
        {
            return RFProcessingContext.Create(this, processingKey, instructionManager, eventManager, workQueueMonitor);
        }

        public IRFReadingContext GetReadingContext()
        {
            return RFProcessingContext.Create(this, null, null, null, null); // could implement lightweight version
        }

        public void Shutdown()
        {
            foreach(var ac in ActiveComponents)
            {
                ac.Shutdown();
            }
        }
    }
}
