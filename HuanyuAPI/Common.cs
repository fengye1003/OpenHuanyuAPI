namespace HuanyuAPI
{
    public class Common
    {
        public static string GetClientIp(HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrEmpty(ip) || ip == "127.0.0.1")
            {
                ip = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            }

            if (string.IsNullOrEmpty(ip) || ip == "127.0.0.1")
            {
                ip = context.Request.Headers["X-Forwarded-For"]
                    .FirstOrDefault()?.Split(',').FirstOrDefault();
            }

            return ip ?? "127.0.0.1";
        }
    }
}
