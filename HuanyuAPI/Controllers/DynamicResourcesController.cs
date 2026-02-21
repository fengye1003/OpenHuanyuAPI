using HuanyuAPI.Essencial_Repos;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

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
            string ip = Common.GetClientIp(HttpContext);
            Log.SaveLog(l.FetchString("logAction", new Dictionary<string, string>
            {
                { "{user}", ip },
                { "{name}",  route }
            }));
            return "Hello, my friend!";
            
        }

        [HttpGet]
        public IActionResult GetRandomImg(string? ImgStorage)
        {
            string route = "DynamicResourcesController.GetRandomImg";
            string ip = Common.GetClientIp(HttpContext);

            if (ImgStorage == null)
            {
                ImgStorage = "default.txt";
            }
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "ImgStorages");
            var fullPath = Path.GetFullPath(Path.Combine(basePath, ImgStorage!));
            
            if (!fullPath.StartsWith(basePath))
            {
                Log.SaveLog(l.FetchString("logActionBlocked", new Dictionary<string, string>
                {
                    { "{user}", ip },
                    { "{name}",  route },
                    { "{reason}", l.FetchString("pathAuthError") },
                    { "{details}", fullPath }
                }));
                return StatusCode(403);
            }
            if (!Regex.IsMatch(ImgStorage, @"^[a-zA-Z0-9_-]+\.txt$"))
            {
                Log.SaveLog(l.FetchString("logActionBlocked", new Dictionary<string, string>
                {
                    { "{user}", ip },
                    { "{name}",  route },
                    { "{reason}", l.FetchString("pathAuthError") },
                    { "{details}", fullPath }
                }));
                return StatusCode(403);
            }
            List<string> lines;
            try
            {
                lines = System.IO.File.ReadAllLines(fullPath)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();
            }
            catch (Exception ex)
            {
                Log.SaveLog(l.FetchString("logActionWithException", new Dictionary<string, string>
                {
                    { "{user}", ip },
                    { "{name}",  route + "/" + ImgStorage },
                    { "{exception}", ex.ToString() },
                }));
                return StatusCode(404);
            }
            var randomUrl = lines[new Random().Next(lines.Count)];

            Log.SaveLog(l.FetchString("logActionWithResult", new Dictionary<string, string>
                {
                    { "{user}", ip },
                    { "{name}",  route + "/" + ImgStorage },
                    { "{result}", randomUrl },
                }));
            return Redirect(randomUrl);
        }

        
    }
}
