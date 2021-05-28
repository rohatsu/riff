using RazorEngine.Templating;
using System.Threading.Tasks;
using System.Web;

namespace RIFF.Framework
{
    /// <summary>
    /// Use this base class for Razor templates to avoid Intellisense errors
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RFTemplateBase<T> : TemplateBase<T>
    {
        //public HttpContextBase Context { get; set; }

        public override Task ExecuteAsync()
        {
            Execute();
            return Task.CompletedTask;
        }

        public virtual void Execute()
        {
        }
    }
}
