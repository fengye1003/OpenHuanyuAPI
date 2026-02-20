using Microsoft.AspNetCore.Mvc;
using HuanyuAPI.Essencial_Repos;

namespace HuanyuAPI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class DynamicResourcesController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Greeting()
        {
            return "Hello, my friend!";
            Log.SaveLog
        }
    }
}
