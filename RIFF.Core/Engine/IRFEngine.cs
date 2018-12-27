// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com

namespace RIFF.Core
{
    internal interface IRFEngine
    {
        void Initialize(IRFProcessingContext serviceContext);

        RFProcessingResult Process(RFInstruction i, IRFProcessingContext processingContext);

        void React(RFEvent e, IRFProcessingContext processingContext);
    }
}
