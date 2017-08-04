// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com

namespace RIFF.Core
{
    internal abstract class RFCatalog : RFStore<RFCatalogKey, RFCatalogEntry>, IRFCatalog
    {
        protected RFCatalog(RFComponentContext context)
            : base(context)
        {
        }
    }
}
