using HuanyuAPI.Essencial_Repos;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace HuanyuAPI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class GenerateController : Controller
    {
        Localization l = Program.l;

        [HttpGet]
        public IActionResult RoddyGenImg(string key, string prompts)
        {
            string route = "GenerateController.RoddyGenImg";
            string ip = Common.GetClientIp(HttpContext);

            string api = "https://ark.cn-beijing.volces.com/api/v3/images/generations";

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");

            var payload = new Dictionary<string, object>
            {
                ["model"] = "doubao-seedream-5-0-260128",
                ["prompt"] = prompts,
                ["sequential_image_generation"] = "disabled",
                ["response_format"] = "url",
                ["size"] = "2K",
                ["stream"] = false,
                ["watermark"] = false

            };
            //var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = client.PostAsJsonAsync(
                api,
                payload
            );
            string result = response.Result.Content.ReadAsStringAsync().Result;

            string imageUrl;
            try
            {
                var doc = JsonDocument.Parse(result);

                imageUrl = doc!
                    .RootElement!
                    .GetProperty("data")[0]!
                    .GetProperty("url")!
                    .GetString()!;
            }
            catch (Exception ex)
            {
                Log.SaveLog(l.FetchString("logActionWithException", new Dictionary<string, string>
                {
                    { "{user}", ip },
                    { "{name}",  route + "/" + key + "&" + prompts },
                    { "{exception}", ex.ToString() },
                }));
                return BadRequest($"Origin Json result : {result}\n" +
                    $"Exception: " + ex);
            }
            Log.SaveLog(l.FetchString("logActionWithResult", new Dictionary<string, string>
                {
                    { "{user}", ip },
                    { "{name}",  route + "/" + prompts },
                    { "{result}", imageUrl },
                }));
            return Redirect(imageUrl);
        }
    }
}
