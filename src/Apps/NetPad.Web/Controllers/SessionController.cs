using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NetPad.Queries;
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

        [HttpGet("queries")]
        public IEnumerable<Query> GetOpenQueries()
        {
            return _session.OpenQueries;
        }
    }
}
