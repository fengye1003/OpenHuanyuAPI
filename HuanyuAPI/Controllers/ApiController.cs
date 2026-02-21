using HuanyuAPI.Essencial_Repos;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text.RegularExpressions;

namespace HuanyuAPI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ApiController : Controller
    {
        Localization l = Program.l;
        [HttpGet]
        public ActionResult<string> Version()
        {
            var version = Assembly
    .GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
    ?.InformationalVersion;
            string route = "ApiController.Version";
            string ip = Common.GetClientIp(HttpContext);
            Log.SaveLog(l.FetchString("logAction", new Dictionary<string, string>
            {
                { "{user}", ip },
                { "{name}",  route }
            }));
            return "Huanyu Api System " + version!;
        }
    }
}
