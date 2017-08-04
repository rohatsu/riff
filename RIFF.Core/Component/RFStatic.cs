// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com

using System.Threading;

namespace RIFF.Core
{
    public static class RFStatic
    {
        public static IRFLog Log { get; set; }
        public static bool IsShutdown { get; set; }
        private static CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();
        public static CancellationToken CancellationToken => CancellationTokenSource.Token;

        public static void SetShutdown()
        {
            IsShutdown = true;
            CancellationTokenSource.Cancel();
        }
    }
}
