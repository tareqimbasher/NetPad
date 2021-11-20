using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.Controllers
{
    [ApiController]
    [Route("session")]
    public class SessionController : Controller
    {
        private readonly ISession _session;

        public SessionController(ISession session)
        {
            _session = session;
        }

        [HttpGet("scripts")]
        public IEnumerable<Script> GetOpenScripts()
        {
            return _session.OpenScripts;
        }
    }
}
