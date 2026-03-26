using Microsoft.AspNetCore.Mvc;

namespace HuanyuAPI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class PlaysisServiceController : Controller
    {

        public ActionResult<string> Greeting()
        {
            return "Hi! HuanyuAPI running!";
        }
    }
}
