using RazorEngine.Templating;
using System.Web;

namespace RIFF.Framework
{
    /// <summary>
    /// Use this base class for Razor templates to avoid Intellisense errors
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RFTemplateBase<T> : TemplateBase<T>
    {
        public HttpContextBase Context { get; set; }
    }
}
