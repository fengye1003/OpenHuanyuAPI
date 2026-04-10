using HuanyuAPI.Essencial_Repos;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

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
                if (!Directory.Exists("./PlaysisUsers/UID/" + uid + "/"))
                {
                    Log.SaveLog(l.FetchString("logActionBlocked", new Dictionary<string, string>
                    {
                        { "{user}", ip },
                        { "{name}",  route },
                        { "{details}", $"False UID. PwHash: {passwordHash}; uid :{uid}" }
                    }));
                    return BadRequest("NotFoundUID");
                }
                else
                {
                    if (System.IO.File.ReadAllText("./PlaysisUsers/UID/" + uid + "/passwordhash.txt") != passwordHash)
                    {
                        Log.SaveLog(l.FetchString("logActionBlocked", new Dictionary<string, string>
                        {
                            { "{user}", ip },
                            { "{name}",  route },
                            { "{details}", $"False password: {passwordHash}; uid :{uid}" }
                        }));
                        return BadRequest("InvalidPasswordHash");
                    }
                    else
                    {
                        if (!System.IO.File.Exists("./PlaysisUsers/UID/" + uid + "/secret.txt"))
                        {
                            System.IO.File.Create("./PlaysisUsers/UID/" + uid + "/secret.txt").Close();
                        }
                        var secret = GenerateSecret(16);
                        if (LoggedInUsers.TryGetValue(uid, out string? _))
                        {
                            LoggedInUsers.Remove(uid);
                        }
                        LoggedInUsers.Add(uid, secret);
                        System.IO.File.WriteAllText("./PlaysisUsers/UID/" + uid + "/secret.txt", secret);

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
                if (IsAdminMethod(uid) && IsValidSecretMethod(uid, secret))
                {
                    InternalForceRegister(regUsername, regUID, regPasswordHash);

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
                    Log.SaveLog(l.FetchString("logActionBlocked", new Dictionary<string, string>
                    {
                        { "{user}", ip },
                        { "{name}",  route },
                        { "{details}", $"uid:{uid}; secret:{secret}. Failed." }
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

        [HttpGet]
        public ActionResult<string> RegisterByAdminRawApi(string uid, string password, string regUsername, string regUID, string regPassword)
        {
            string route = "PlaysisServiceController.RegisterByAdmin";
            string ip = Common.GetClientIp(HttpContext);
            if (AuthMethod(uid,GetHash(password)))
            {
                InternalForceRegister(regUsername, regUID, GetHash(regPassword));
                
                Log.SaveLog(l.FetchString("logActionWithResult", new Dictionary<string, string>
                    {
                        { "{user}", ip },
                        { "{name}",  route },
                        { "{result}", $"uid :{uid}. New user info: {regUID} pwHash={GetHash(regPassword)}, username={regUsername}" }
                    }));
                return "ok";
            }
            else
            {
                Log.SaveLog(l.FetchString("logActionBlocked", new Dictionary<string, string>
                    {
                        { "{user}", ip },
                        { "{name}",  route },
                        { "{details}", $"uid:{uid}; rawPassword:{password}. Failed." }
                    }));
                return "AuthError";
            }
        }

        public static void InternalForceRegister(string regUsername, string regUID, string regPasswordHash)
        {
            if (!Directory.Exists("./PlaysisUsers/UID/" + regUID + "/"))
            {
                Directory.CreateDirectory("./PlaysisUsers/UID/" + regUID + "/");
            }
            if (!System.IO.File.Exists("./PlaysisUsers/UID/" + regUID + "/passwordhash.txt"))
            {
                System.IO.File.Create("./PlaysisUsers/UID/" + regUID + "/passwordhash.txt").Close();
            }
            if (!System.IO.File.Exists("./PlaysisUsers/UID/" + regUID + "/username.txt"))
            {
                System.IO.File.Create("./PlaysisUsers/UID/" + regUID + "/username.txt").Close();
            }
            System.IO.File.WriteAllText("./PlaysisUsers/UID/" + regUID + "/passwordhash.txt", regPasswordHash);
            System.IO.File.WriteAllText("./PlaysisUsers/UID/" + regUID + "/username.txt", regUsername);
        }

        public ActionResult<string> ChangeUsername(string uid, string secret, string newUsername)
        {
            string route = "PlaysisServiceController.ChangeUsername";
            string ip = Common.GetClientIp(HttpContext);
            try
            {
                if (IsValidSecretMethod(uid, secret))
                {
                    System.IO.File.WriteAllText("./PlaysisUsers/UID/" + uid + "/username.txt", newUsername);
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
                    Log.SaveLog(l.FetchString("logActionBlocked", new Dictionary<string, string>
                    {
                        { "{user}", ip },
                        { "{name}",  route },
                        { "{details}", $"uid:{uid}; secret:{secret}. Failed." }
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
                var result = System.IO.File.ReadAllText("./PlaysisUsers/UID/" + uid + "/username.txt");
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
        public ActionResult<string> IsAdmin(string uid)
        {
            string route = "PlaysisServiceController.IsAdmin";
            string ip = Common.GetClientIp(HttpContext);
            try
            {
                CreateInstanceIfBoot();
                if (IsAdminMethod(uid))
                {
                    Log.SaveLog(l.FetchString("logActionWithResult", new Dictionary<string, string>
                    {
                        { "{user}", ip },
                        { "{name}",  route },
                        { "{result}", $"uid:{uid}. Success." }
                    }));
                    return "1";
                }
                else
                {
                    Log.SaveLog(l.FetchString("logActionWithResult", new Dictionary<string, string>
                    {
                        { "{user}", ip },
                        { "{name}",  route },
                        { "{result}", $"uid:{uid}. Failed." }
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
                        { "{exception}", $"uid :{uid}. Ex: {ex}" }
                    }));
                return "InternalError";
            }
        }

        [HttpGet]
        public ActionResult<string> Status()
        {

            CreateInstanceIfBoot();
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

        static bool AuthMethod(string uid, string passwordHash)
        {
            try
            {
                if (System.IO.File.ReadAllText(
                    "./PlaysisUsers/UID/" + 
                    uid + "/passwordhash.txt") == passwordHash)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        static bool IsAdminMethod(string uid)
        {
            if (System.IO.File.Exists("./PlaysisUsers/UID/" + uid + "/.admin"))
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
        
        static string GetHash(string input)
        {
            //Create a byte array from source data.
            var tmpSource = ASCIIEncoding.ASCII.GetBytes(input);
            //Compute hash based on source data.
            var tmpHash = MD5.HashData(tmpSource);
            //Debug.Log(ByteArrayToString(tmpHash));
            return ByteArrayToString(tmpHash);
        }

        static string ByteArrayToString(byte[] arrInput)
        {
            int i;
            StringBuilder sOutput = new StringBuilder(arrInput.Length);
            for (i = 0; i < arrInput.Length; i++)
            {
                sOutput.Append(arrInput[i].ToString("X2"));
            }
            return sOutput.ToString();
        }
    }
}
