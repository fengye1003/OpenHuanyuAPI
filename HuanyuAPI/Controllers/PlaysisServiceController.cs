using HuanyuAPI.Essencial_Repos;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace HuanyuAPI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class PlaysisServiceController : Controller
    {
        static Hashtable? ActiveConfig = null;
        Localization l = Program.l;
        static Hashtable htStandard = new()
        {
            { "type", "HuanyuApiConfig.PlaysisConfig" },
            { "status", "running" },//Maintain
        };
        static Dictionary<string, string> LoggedInUsers = new();

        public string GenerateSecret(int length)
        {
            const string KeyDic = "QWERTYUIOPASDFGHJKLZXCVBNMqwertyuiopasdfghjklzxcvbnm1234567890";
            string result = "";
            Random r = new();
            for (int i = 0; i < length; i++)
            {
                var index = r.Next(KeyDic.Length);
                result += KeyDic[index];
            }
            return result;
        }
        static void CreateInstanceIfBoot()
        {
            if (ActiveConfig == null)
            {
                ActiveConfig = PropertiesHelper.AutoCheck(htStandard, "./Properties/Playsis.properties");
            }
        }

        [HttpGet]
        public ActionResult<string> Greeting()
        {
            string route = "PlaysisServiceController.Greeting";
            string ip = Common.GetClientIp(HttpContext);
            Log.SaveLog(l.FetchString("logAction", new Dictionary<string, string>
            {
                { "{user}", ip },
                { "{name}",  route }
            }));
            CreateInstanceIfBoot();
            return "Hi! HuanyuAPI running!";
        }

        [HttpGet]
        public IActionResult Login(string uid, string passwordHash)
        {
            string route = "PlaysisServiceController.Login";
            string ip = Common.GetClientIp(HttpContext);
            
            CreateInstanceIfBoot();
            try
            {
                if (!Directory.Exists("./PlaysisUsers/"))
                {
                    Directory.CreateDirectory("./PlaysisUsers/");
                }
                if (!Directory.Exists("./PlayersisUsers/UID/" + uid + "/"))
                {
                    Log.SaveLog(l.FetchString("logActionBlocked", new Dictionary<string, string>
                    {
                        { "{user}", ip },
                        { "{name}",  route },
                        { "{reason}", $"False UID. PwHash: {passwordHash}; uid :{uid}" }
                    }));
                    return BadRequest("NotFoundUID");
                }
                else
                {
                    if (System.IO.File.ReadAllText("./PlayersisUsers/UID/" + uid + "/passwordhash.txt") != passwordHash)
                    {
                        Log.SaveLog(l.FetchString("logActionBlocked", new Dictionary<string, string>
                        {
                            { "{user}", ip },
                            { "{name}",  route },
                            { "{reason}", $"False password: {passwordHash}; uid :{uid}" }
                        }));
                        return BadRequest("InvalidPasswordHash");
                    }
                    else
                    {
                        if (!System.IO.File.Exists("./PlayersisUsers/UID/" + uid + "/secret.txt"))
                        {
                            System.IO.File.Create("./PlayersisUsers/UID/" + uid + "/secret.txt");
                        }
                        var secret = GenerateSecret(16);
                        if (LoggedInUsers.TryGetValue(uid, out string? _))
                        {
                            LoggedInUsers.Remove(uid);
                        }
                        LoggedInUsers.Add(uid, secret);
                        System.IO.File.WriteAllText("./PlayersisUsers/UID/" + uid + "/secret.txt", secret);

                        Log.SaveLog(l.FetchString("logActionWithResult", new Dictionary<string, string>
                        {
                            { "{user}", ip },
                            { "{name}",  route },
                            { "{result}", $"UID = {uid}, secret = {secret}" }
                        }));
                        return Ok(secret);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.SaveLog(l.FetchString("logActionWithException", new Dictionary<string, string>
                    {
                        { "{user}", ip },
                        { "{name}",  route },
                        { "{exception}", $"PwHash: {passwordHash}; uid :{uid}. Ex: {ex}" }
                    }));
                return BadRequest("InternalError");
            }
        }

        [HttpGet]
        public ActionResult<string> IsValidSecret(string uid, string secret)
        {
            string route = "PlaysisServiceController.IsValidSecret";
            string ip = Common.GetClientIp(HttpContext);
            try
            {
                CreateInstanceIfBoot();
                if (IsValidSecretMethod(uid, secret))
                {
                    Log.SaveLog(l.FetchString("logActionWithResult", new Dictionary<string, string>
                    {
                        { "{user}", ip },
                        { "{name}",  route },
                        { "{result}", $"uid:{uid}; secret:{secret}. Success." }
                    }));
                    return "1";
                }
                else
                {
                    Log.SaveLog(l.FetchString("logActionWithResult", new Dictionary<string, string>
                    {
                        { "{user}", ip },
                        { "{name}",  route },
                        { "{result}", $"uid:{uid}; secret:{secret}. Failed." }
                    }));
                    return "0";
                }
            }
            catch (Exception ex)
            {
                Log.SaveLog(l.FetchString("logActionWithException", new Dictionary<string, string>
                    {
                        { "{user}", ip },
                        { "{name}",  route },
                        { "{exception}", $"Secret: {secret}; uid :{uid}. Ex: {ex}" }
                    }));
                return "InternalError";
            }
            
        }

        [HttpGet]
        public ActionResult<string> RegisterByAdmin(string uid, string secret, string regUsername, string regUID, string regPasswordHash)
        {
            string route = "PlaysisServiceController.RegisterByAdmin";
            string ip = Common.GetClientIp(HttpContext);
            try
            {
                if (IsAdmin(uid) && IsValidSecretMethod(uid, secret))
                {
                    if (!Directory.Exists("./PlayersisUsers/UID/" + regUID + "/"))
                    {
                        Directory.CreateDirectory("./PlayersisUsers/UID/" + regUID + "/");
                    }
                    if (!System.IO.File.Exists("./PlayersisUsers/UID/" + regUID + "/passwordhash.txt"))
                    {
                        System.IO.File.Create("./PlayersisUsers/UID/" + regUID + "/passwordhash.txt");
                    }
                    if (!System.IO.File.Exists("./PlayersisUsers/UID/" + regUID + "/username.txt"))
                    {
                        System.IO.File.Create("./PlayersisUsers/UID/" + regUID + "/username.txt");
                    }
                    System.IO.File.WriteAllText("./PlayersisUsers/UID/" + regUID + "/passwordhash.txt", regPasswordHash);
                    System.IO.File.WriteAllText("./PlayersisUsers/UID/" + regUID + "/username.txt", regUsername);
                    Log.SaveLog(l.FetchString("logActionWithResult", new Dictionary<string, string>
                    {
                        { "{user}", ip },
                        { "{name}",  route },
                        { "{result}", $"Secret: {secret}; uid :{uid}. New user info: {regUID} pw={regPasswordHash}, username={regUsername}" }
                    }));
                    return "ok";
                }
                else
                {
                    Log.SaveLog(l.FetchString("logActionWithBlocked", new Dictionary<string, string>
                    {
                        { "{user}", ip },
                        { "{name}",  route },
                        { "{reason}", $"uid:{uid}; secret:{secret}. Failed." }
                    }));
                    return "InvalidOperation";
                }
            }
            catch (Exception ex)
            {
                Log.SaveLog(l.FetchString("logActionWithException", new Dictionary<string, string>
                    {
                        { "{user}", ip },
                        { "{name}",  route },
                        { "{exception}", $"Secret: {secret}; uid :{uid}. Ex: {ex}" }
                    }));
                return "InternalError";
            }
            
        }

        public ActionResult<string> ChangeUsername(string uid, string secret, string newUsername)
        {
            string route = "PlaysisServiceController.ChangeUsername";
            string ip = Common.GetClientIp(HttpContext);
            try
            {
                if (IsValidSecretMethod(uid, secret))
                {
                    System.IO.File.WriteAllText("./PlayersisUsers/UID/" + uid + "/username.txt", newUsername);
                    Log.SaveLog(l.FetchString("logActionWithResult", new Dictionary<string, string>
                    {
                        { "{user}", ip },
                        { "{name}",  route },
                        { "{result}", $"Secret: {secret}; uid :{uid}. New username: {newUsername}" }
                    }));
                    return "ok";
                }
                else
                {
                    Log.SaveLog(l.FetchString("logActionWithBlocked", new Dictionary<string, string>
                    {
                        { "{user}", ip },
                        { "{name}",  route },
                        { "{reason}", $"uid:{uid}; secret:{secret}. Failed." }
                    }));
                    return "InvalidOperation";
                }
            }
            catch (Exception ex)
            {
                Log.SaveLog(l.FetchString("logActionWithException", new Dictionary<string, string>
                {
                    { "{user}", ip },
                    { "{name}",  route },
                    { "{exception}", $"Secret: {secret}; uid :{uid}; new username={newUsername}. Ex: {ex}" }
                }));
                return "InternalError";
            }
        }

        [HttpGet]
        public ActionResult<string> GetUsername(string uid)
        {
            string route = "PlaysisServiceController.GetUsername";
            string ip = Common.GetClientIp(HttpContext);
            try
            {
                var result = System.IO.File.ReadAllText("./PlayersisUsers/UID/" + uid + "/username.txt");
                Log.SaveLog(l.FetchString("logActionWithResult", new Dictionary<string, string>
                {
                    { "{user}", ip },
                    { "{name}",  route },
                    { "{result}", $"uid :{uid}. username={result}" }
                }));
                return result;
            }
            catch (Exception ex)
            {
                Log.SaveLog(l.FetchString("logActionWithException", new Dictionary<string, string>
                {
                    { "{user}", ip },
                    { "{name}",  route },
                    { "{exception}", $"uid :{uid}. May not exist. Ex: {ex}" }
                }));
                return "NULL";
                //throw;
            }
        }

        [HttpGet]
        public ActionResult<string> Status()
        {
            string route = "PlaysisServiceController.Status";
            string ip = Common.GetClientIp(HttpContext);
            var result = (string)ActiveConfig!["status"]!;
            Log.SaveLog(l.FetchString("logActionWithResult", new Dictionary<string, string>
            {
                { "{user}", ip },
                { "{name}",  route },
                { "{result}",  result}
            }));
            CreateInstanceIfBoot();
            return result;
        }

        static bool IsAdmin(string uid)
        {
            if (System.IO.File.Exists("./PlayersisUsers/UID/" + uid + "/.admin"))
            {
                return true;
            }
            return false;
        }

        static bool IsValidSecretMethod(string uid, string secret)
        {
            if (LoggedInUsers.TryGetValue(uid, out string? trueSecret) && secret == trueSecret)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
