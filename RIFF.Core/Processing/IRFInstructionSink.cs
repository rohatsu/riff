// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com

namespace RIFF.Core
{
    public interface IRFInstructionSink
    {
        void QueueInstruction(object issuedBy, RFInstruction i, string processingKey);
    }
}
