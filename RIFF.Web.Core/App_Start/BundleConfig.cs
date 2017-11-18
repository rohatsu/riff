// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using System.Web.Optimization;

namespace RIFF.Web.Core
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/core").Include(
                        "~/Scripts/jquery-{version}.js",
                        "~/Scripts/cldr.js",
                        "~/Scripts/cldr/event.js",
                        "~/Scripts/cldr/supplemental.js",
                        "~/Scripts/cldr/unresolved.js",
                        "~/Scripts/globalize.js",
                        "~/Scripts/globalize/message.js",
                        "~/Scripts/globalize/number.js",
                        "~/Scripts/globalize/date.js",
                        "~/Scripts/globalize/currency.js",
                        //"~/Scripts/jquery.globalize/globalize.js",
                        //"~/Scripts/jquery.globalize/cultures/globalize.culture.en-GB.js",
                        "~/Scripts/knockout-{version}.js",
                        "~/Scripts/knockout.mapping-latest.js",
                        "~/Scripts/jszip.js",
                        "~/Scripts/dx.viz-web.js",
                        "~/Scripts/RIFF/RIFF.js"
                        ));

            bundles.Add(new ScriptBundle("~/bundles/codemirror").Include(
                        "~/Scripts/codemirror/lib/codemirror.js",
                        "~/Scripts/codemirror/mode/xml/xml.js",
                        "~/Scripts/codemirror/addon/dialog/dialog.js",
                        "~/Scripts/codemirror/addon/search/searchcursor.js",
                        "~/Scripts/codemirror/addon/search/search.js",
                        "~/Scripts/codemirror/addon/scroll/annotatescrollbar.js",
                        "~/Scripts/codemirror/addon/search/matchesonscrollbar.js",
                        "~/Scripts/codemirror/addon/scroll/simplescrollbars.js",
                        "~/Scripts/codemirror/addon/edit/matchtags.js",
                        "~/Scripts/codemirror/addon/fold/foldcode.js",
                        "~/Scripts/codemirror/addon/fold/foldgutter.js",
                        "~/Scripts/codemirror/addon/fold/xml-fold.js",
                        "~/Scripts/codemirror/addon/runmode/colorize.js",
                        "~/Scripts/codemirror/addon/display/panel.js",
                        "~/Scripts/codemirror/addon/display/fullscreen.js"
                        ));

            var customScripts = RIFF.Web.Core.App_Start.RIFFStart.Config.GetCustomScriptsBundle();
            bundles.Add(new ScriptBundle("~/bundles/custom").Include(customScripts ?? new string[0]));

            var additionalScripts = RIFF.Web.Core.App_Start.RIFFStart.Config.GetAdditionalScriptsBundles();
            if (additionalScripts != null)
            {
                foreach (var cs in additionalScripts)
                {
                    bundles.Add(new ScriptBundle(cs.Key).Include(cs.Value));
                }
            }

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            bundles.Add(new StyleBundle("~/Content/core").Include(
                        "~/Content/dx.common.css",
                        "~/Content/dx.rohatsu.css",
                        //"~/Content/dx.light.compact.css",
                        "~/Scripts/codemirror/lib/codemirror.css",
                        "~/Scripts/codemirror/addon/dialog/dialog.css",
                        "~/Scripts/codemirror/addon/search/matchesonscrollbar.css",
                        "~/Scripts/codemirror/addon/fold/foldgutter.css",
                        "~/Scripts/codemirror/addon/scroll/simplescrollbars.css",
                        "~/Scripts/codemirror/addon/display/fullscreen.css",
                        "~/Content/site.css"));

            var customStyles = RIFF.Web.Core.App_Start.RIFFStart.Config.GetCustomStylesBundle();
            bundles.Add(new StyleBundle("~/Content/custom").Include(customStyles ?? new string[0]));

#if(DEBUG)
            BundleTable.EnableOptimizations = false;
#else
            BundleTable.EnableOptimizations = true;
#endif
        }
    }
}
