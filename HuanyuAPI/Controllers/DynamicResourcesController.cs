using Microsoft.AspNetCore.Mvc;
using HuanyuAPI.Essencial_Repos;

namespace HuanyuAPI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class DynamicResourcesController : ControllerBase
    {
        Localization l = Program.l;
        [HttpGet]
        public ActionResult<string> Greeting()
        {
            string route = "DynamicResourcesController.Greeting";
            string ip = HttpContext.Connection.RemoteIpAddress!.MapToIPv4().ToString();
            Log.SaveLog(l.FetchString("logAction").Replace("{user}", ip).Replace("{name}", route));
            return "Hello, my friend!";
            
        }
    }
}
