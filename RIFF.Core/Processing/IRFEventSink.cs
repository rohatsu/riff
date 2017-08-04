// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com

namespace RIFF.Core
{
    public interface IRFEventSink
    {
        void RaiseEvent(object raisedBy, RFEvent e, string processingKey);
    }
}
