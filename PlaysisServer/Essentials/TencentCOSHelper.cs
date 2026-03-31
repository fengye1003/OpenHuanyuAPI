using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Mail;
using COSSTS;
using COSXML;
using COSXML.Auth;
using COSXML.Model.Tag;
using Newtonsoft.Json;
using Formatting = System.Xml.Formatting;


namespace PlaysisServer.Essentials
{
    public class TencentCOSHelper
    {
        //永久密钥，替换相关参数后执行生成所需信息
        string secretId = (string)Program.Config["tencentApiSecretId"]!;
        string secretKey = (string)Program.Config["tencentApiSecretKey"]!;

        string bucket = (string)Program.Config["bucket"]!;
        string appId = (string)Program.Config["appId"]!;
        string region = (string)Program.Config["region"]!;
        public string filename;
        string method = "put";
        int time = 1800;

        Boolean limitContentLength = true; // 限制上传文件大小



        public Dictionary<string, object> getConfig()
        {
            Dictionary<string, object> config = new Dictionary<string, object>();
            string[] allowActions = new string[] {  // 允许的操作范围，这里以上传操作为例
                "name/cos:PutObject"
            };

            string key = filename;
            string resource = $"qcs::cos:{region}:uid/{appId}:{bucket}/{key}";

            var condition = new Dictionary<string, object>();

            // 3. 限制上传文件大小(只对简单上传生效)
            if (limitContentLength)
            {
                condition["numeric_less_than_equal"] = new Dictionary<string, long>
                {
                    { "cos:content-length", Convert.ToInt32((string)Program.Config["limitUploadMB"]!) * 1024 * 1024 } // 上传大小限制不能超过 5MB
                };
            }

            var policy = new Dictionary<string, object>
            {
                { "version", "2.0" },
                { "statement", new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            { "action", allowActions },
                            { "effect", "allow" },
                            { "resource", new List<string>
                                {
                                    resource,
                                }
                            },
                            { "condition", condition }
                        }
                    }
                }
            };

            // 序列化为 JSON 并输出
            string jsonPolicy = JsonConvert.SerializeObject(policy);

            config.Add("bucket", bucket);
            config.Add("region", region);
            config.Add("durationSeconds", time);

            config.Add("secretId", secretId);
            config.Add("secretKey", secretKey);
            config.Add("key", key);
            config.Add("policy", jsonPolicy);
            return config;
        }

        // 获取联合身份临时访问凭证 https://cloud.tencent.com/document/product/1312/48195
        public Dictionary<string, object> GetCredential()
        {
            var config = getConfig();
            //获取临时密钥
            Dictionary<string, object> credential = STSClient.genCredential(config);
            Dictionary<string, object> credentials = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject((object)credential["Credentials"]));
            Dictionary<string, object> credentials1 = new Dictionary<string, object>();
            credentials1.Add("tmpSecretId", credentials["TmpSecretId"]);
            credentials1.Add("tmpSecretKey", credentials["TmpSecretKey"]);
            credentials1.Add("sessionToken", credentials["Token"]);
            Dictionary<string, object> dictionary1 = new Dictionary<string, object>();
            dictionary1.Add("credentials", credentials1);
            dictionary1.Add("startTime", credential["StartTime"]);
            dictionary1.Add("requestId", credential["RequestId"]);
            dictionary1.Add("expiration", credential["Expiration"]);
            dictionary1.Add("expiredTime", credential["ExpiredTime"]);
            dictionary1.Add("bucket", config["bucket"]);
            dictionary1.Add("region", config["region"]);
            dictionary1.Add("key", config["key"]);
            return dictionary1;
        }

        public string GetPresignedUrl(string hash)
        {
            TencentCOSHelper m = new TencentCOSHelper();
            m.filename = hash;
            Dictionary<string, object> result = m.GetCredential();
            Dictionary<string, object> credentials = (Dictionary<string, object>)result["credentials"];
            string tmpSecretId = (string)credentials["tmpSecretId"];
            string tmpSecretKey = (string)credentials["tmpSecretKey"];
            string sessionToken = (string)credentials["sessionToken"];
            string bucket = (string)result["bucket"];
            string region = (string)result["region"];
            string key = (string)result["key"];
            long expiredTime = (long)result["expiredTime"];
            int startTime = (int)result["startTime"];

            QCloudCredentialProvider cosCredentialProvider = new DefaultSessionQCloudCredentialProvider(
                tmpSecretId, tmpSecretKey, expiredTime, sessionToken);

            CosXmlConfig config = new CosXmlConfig.Builder()
                .IsHttps(true)  //设置默认 HTTPS 请求
                .SetRegion(region)  //设置一个默认的存储桶地域
                .SetDebugLog(true)  //显示日志
                .Build();  //创建 CosXmlConfig 对象
            CosXml cosXml = new CosXmlServer(config, cosCredentialProvider);

            PreSignatureStruct preSignatureStruct = new PreSignatureStruct();

            preSignatureStruct.appid = m.appId;//"1250000000";

            preSignatureStruct.region = region;//"COS_REGION";

            preSignatureStruct.bucket = bucket;//"examplebucket-1250000000";
            preSignatureStruct.key = filename; //对象键
            preSignatureStruct.httpMethod = "PUT"; //HTTP 请求方法
            preSignatureStruct.isHttps = true; //生成 HTTPS 请求 URL
            preSignatureStruct.signDurationSecond = 600; //请求签名时间为 600s
            preSignatureStruct.headers = null; //签名中需要校验的 header
            preSignatureStruct.queryParameters = null; //签名中需要校验的 URL 中请求参数
            //上传预签名 URL (使用永久密钥方式计算的签名 URL)
            Dictionary<string, string> queryParameters = new Dictionary<string, string>();
            queryParameters.Add("x-cos-security-token", sessionToken);
            Dictionary<string, string> headers = new Dictionary<string, string>();
            //string authorization = cosXml.GenerateSign(m.method, key, queryParameters, headers, expiredTime - startTime, expiredTime - startTime);

            //string host = "https://" + bucket + ".cos." + region + ".myqcloud.com";
            ////Dictionary<string, object> response = new Dictionary<string, object>();
            ////response.Add("cosHost", host);
            ////response.Add("cosKey", key);
            ////response.Add("authorization", authorization);
            ////response.Add("securityToken", sessionToken);
            ////Console.WriteLine($"{JsonConvert.SerializeObject(response)}");
            //List<string> resultList = new();
            //resultList.Add(host + "/" + hash);
            //resultList.Add(authorization);
            //resultList.Add(sessionToken);

            string url = cosXml.GenerateSignURL(preSignatureStruct);

            return url;
        }
    }
}