using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GoshBot.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EchoController : ControllerBase
    {
        // GET
        public string Get()
        {
            return "ok:" + DateTime.Now;
        }
    }
}