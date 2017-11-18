// ROHATSU RIFF FRAMEWORK / copyright (c) 2014-2017 rohatsu software studios limited / www.rohatsu.com
using RIFF.Core;
using RIFF.Web.Core.Helpers;
using System.Web.Http;

namespace RIFF.Web.Core.Controllers
{
    [RFControllerAuthorize(AccessLevel = RFAccessLevel.Read, ResponseType = ResponseType.Page)] // every RIFF controller needs at least Read permission to run
    public abstract class RIFFApiController : ApiController
    {
        public string Username
        {
            get
            {
                if (_userName != null)
                {
                    return _userName;
                }
                else
                {
                    _userName = RFUser.GetUserName(User);
                    return _userName;
                }
            }
        }

        protected IRFProcessingContext Context { get { return _context; } }

        protected RFEngineDefinition EngineConfig { get { return _engineConfig; } }

        protected IRFLog Log { get { return _context.SystemLog; } }

        private readonly IRFProcessingContext _context;
        private readonly RFEngineDefinition _engineConfig;
        private string _userName;

        protected RIFFApiController(IRFProcessingContext context, RFEngineDefinition engineConfig)
        {
            _context = context;
            _engineConfig = engineConfig;
        }
    }
}
