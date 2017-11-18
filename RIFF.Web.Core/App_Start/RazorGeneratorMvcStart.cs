using RazorGenerator.Mvc;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;

[assembly: WebActivatorEx.PostApplicationStartMethod(typeof(RIFF.Web.Core.RazorGeneratorMvcStart), "Start")]

namespace RIFF.Web.Core
{
    public static class RazorGeneratorMvcStart
    {
        public static void Start()
        {
            var engine = new PrecompiledMvcEngine(typeof(RazorGeneratorMvcStart).Assembly)
            {
                UsePhysicalViewsIfNewer = HttpContext.Current.Request.IsLocal
            };

            ViewEngines.Engines.Insert(0, engine);

            // StartPage lookups are done by WebPages.
            VirtualPathFactoryManager.RegisterVirtualPathFactory(engine);
        }
    }
}
