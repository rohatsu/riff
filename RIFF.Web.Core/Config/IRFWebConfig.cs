// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2019 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Web.Core.Helpers;
using System.Collections.Generic;

namespace RIFF.Web.Core.Config
{
    public class RFMenu
    {
        public List<RFMenuItem> Items { get; set; }

        public string Name { get; set; }
    }

    public interface IRFWebConfig
    {
        Dictionary<string, string[]> GetAdditionalScriptsBundles();

        string[] GetCustomScriptsBundle();

        string[] GetCustomStylesBundle();

        IEnumerable<RFEnum> GetInputFileKeys();

        RFMenu GetMenu(IRFUserRole userRole, string username, bool presentationMode);
    }
}
