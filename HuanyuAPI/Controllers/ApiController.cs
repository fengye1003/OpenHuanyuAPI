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
    
        [HttpGet]
        public ActionResult<string> VersionInfo()
        {
            var asm = Assembly.GetExecutingAssembly();
            var version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

            string commit = "";
            var plus = version.IndexOf('+');
            if (plus >= 0 && plus + 1 < version.Length) commit = version.Substring(plus + 1);

            var buildTime = "";
            try
            {
                var path = asm.Location;
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                    buildTime = System.IO.File.GetLastWriteTimeUtc(path).ToString("yyyy-MM-ddTHH:mm:ssZ");
            }
            catch { }

            var json = "{" +
                       $"\"version\":\"{version}\"," +
                       $"\"commit\":\"{commit}\"," +
                       $"\"builtUtc\":\"{buildTime}\"," +
                       $"\"processStartedUtc\":\"{Program.StartedUtc:yyyy-MM-ddTHH:mm:ssZ}\"" +
                       "}";
            return json;
        }
    }}

