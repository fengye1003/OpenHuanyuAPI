using HuanyuAPI.Essencial_Repos;
using System.Collections;

namespace HuanyuAPI
{
    public class Program
    {
        static bool DoLogCleanUp;
        static bool EnableLogs;
        static bool EnableLogsWriting;
        static int MaxLogEntries;
        static int Port;
        const int DEFAULT_PORT = 5000;
        static string languageStr = "unknown";
        const string DEFAULT_LANG_STR = "enus";
        public static Localization l = new();
        static Hashtable Config = [];
        static Hashtable ConfigStandard = new()
        {
            { "type", "HuanyuApiConfig"},
            { "language", "unknown"},
            { "enableLogs", "true"},
            { "enableLogsWriting", "true"},
            { "maxLogEntries", "0"},
            { "autoBuild", "false"},
            { "port", "0"},
            { "resetAppOnBoot", "true"}
        };
        public static void Main(string[] args)
        {
            Log.EnableLogs = false;
            Log.EnableWriting = false;
            string PropertiesPath = "./Properties/Main.properties";
            Config = PropertiesHelper.AutoCheck(ConfigStandard, PropertiesPath);
            if ((string)Config["type"]! != "HuanyuApiConfig")
            {
                throw new Exception($"Invalid properties file! A HuanyuApiConfig type is required but read a {(string)Config["type"]!} type file.\n" +
                    $"If this is unexpected, simply delete {PropertiesPath} and program will rebuild correct file in an initialization.");
            }
            languageStr = (string)Config["language"]!;

            #region FirstRunSettings
            if ((string)Config["port"]! == "0" || (string)Config["resetAppOnBoot"]! == "true")
            {
                // Initialize AppConfig
                if ((string)Config["autoBuild"]! == "true")
                {
                    if (languageStr == "unknown")
                    {
                        languageStr = DEFAULT_LANG_STR;
                        Config["language"] = DEFAULT_LANG_STR;
                    }
                    if ((string)Config["port"]! == "0")
                    {
                        Config["port"] = DEFAULT_PORT;
                    }
                    if ((string)Config["resetAppOnBoot"]! == "true")
                    {
                        Config = ConfigStandard;
                        Config["resetAppOnBoot"] = "false";
                        Config["language"] = DEFAULT_LANG_STR;
                        Config["port"] = DEFAULT_PORT;
                    }
                    PropertiesHelper.Save(PropertiesPath, Config);
                }
                else
                {
                    if (languageStr == "unknown")
                    {
                        foreach (var s in Localization.FetchAllTexts("SelectLanguage"))
                        {
                            Console.WriteLine(s);
                        }
                        foreach (var s in Localization.FetchAllTexts("LanguageKey"))
                        {
                            Console.WriteLine(s);
                        }
                        while (languageStr == "unknown")
                        {
                            var lang = Console.ReadLine();
                            switch (lang)
                            {
                                case "zhcn":
                                    languageStr = "zhcn";
                                    Config["language"] = languageStr;
                                    l.ApplyLanguage = Localization.Language.zh_CN;
                                    break;
                                case "enus":
                                    languageStr = "enus";
                                    Config["language"] = languageStr;
                                    l.ApplyLanguage = Localization.Language.en_US;
                                    break;
                                default:
                                    foreach (var s in Localization.FetchAllTexts("unsupportedLanguageOnSetting"))
                                    {
                                        Console.WriteLine(s + lang);
                                    }
                                    break;
                            }
                        }
                        
                    }
                    bool setflag = false;
                    while (!setflag)
                    {
                        Console.Write(l.FetchString("inputPortNum"));
                        string? result = Console.ReadLine();
                        try
                        {
                            Port = Convert.ToInt32(result);
                            Config["port"] = Port.ToString();
                            setflag = true;
                            if (Port <= 0 || Port > 65535)
                            {
                                Console.WriteLine(l.FetchString("portOutOfRangeError") + +Port);
                                setflag = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{l.FetchString("propertyConvertError")}: {ex}");
                            // throw;
                        }
                    }
                    setflag = false;
                    while (!setflag)
                    {
                        Console.Write(l.FetchString("inputEnableLogs"));
                        string? result = Console.ReadLine();
                        if (result == "1")
                        {
                            Config["enableLogs"] = "true";
                            EnableLogs = true;
                            setflag = true;
                        }
                        else if (result == "0")
                        {
                            Config["enableLogs"] = "false";
                            EnableLogs = false;
                            setflag = true;
                        }
                        else Console.WriteLine(l.FetchString("inputError"));
                    }
                    setflag = false;
                    if (EnableLogs)
                    {
                        while (!setflag)
                        {
                            Console.Write(l.FetchString("inputEnableLogWrite"));
                            string? result = Console.ReadLine();
                            if (result == "1")
                            {
                                Config["enableLogsWriting"] = "true";
                                EnableLogsWriting = true;
                                setflag = true;
                            }
                            else if (result == "0")
                            {
                                Config["enableLogsWriting"] = "false";
                                EnableLogsWriting = false;
                                setflag = true;
                            }
                            else Console.WriteLine(l.FetchString("inputError"));
                        }
                        setflag = false;
                        if (EnableLogsWriting)
                        {
                            while (setflag == false)
                            {
                                Console.Write(l.FetchString("inputMaxLogEntries"));
                                string? result = Console.ReadLine();
                                try
                                {
                                    MaxLogEntries = Convert.ToInt32(result);
                                    Config["maxLogEntries"] = MaxLogEntries.ToString();
                                    setflag=true;
                                    if (MaxLogEntries < 0)
                                    {
                                        Console.WriteLine(l.FetchString("inputError"));
                                        setflag = false;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"{l.FetchString("propertyConvertError")}: {ex}");
                                    // throw;
                                }
                            }
                        }
                    }
                    Config["resetAppOnBoot"] = "false";
                    PropertiesHelper.Save(PropertiesPath, Config);
                    Log.SaveLog(l.FetchString("initializeComplete"));
                }
            }
            #endregion

            #region ReadConfig
            l = new(languageStr);
            DoLogCleanUp = (string)Config["maxLogEntries"]! != "0";
            EnableLogs = (string)Config["enableLogs"]! == "true";
            Log.EnableLogs = EnableLogs;
            EnableLogsWriting = (string)Config["enableLogsWriting"]! == "true";
            Log.EnableWriting = EnableLogsWriting;

            try
            {
                MaxLogEntries = Convert.ToInt32((string)Config["maxLogEntries"]!);
            }
            catch (Exception ex)
            {
                MaxLogEntries = Convert.ToInt32((string)ConfigStandard["maxLogEntries"]!);
                Config["maxLogEntries"] = (string)ConfigStandard["maxLogEntries"]!;
                PropertiesHelper.Save(PropertiesPath, Config);
                Log.SaveLog($"{l.FetchString("propertyConvertError")}: {ex}");
            }
            try
            {
                Port = Convert.ToInt32((string)Config["port"]!);
                if (Port < 0 || Port > 65535) 
                {
                    Log.SaveLog(l.FetchString("portOutOfRangeError") + Port);
                    Port = 5000;
                    Config["port"] = DEFAULT_PORT;
                    PropertiesHelper.Save(PropertiesPath, Config);
                }
            }
            catch (Exception ex)
            {
                Port = 5000;
                Config["port"] = DEFAULT_PORT;
                PropertiesHelper.Save(PropertiesPath, Config);
                Localization l = new(languageStr);
                Log.EnableLogs = true;
                Log.SaveLog($"{l.FetchString("propertyConvertError")}: {ex}");
            }
            #endregion
            if (DoLogCleanUp)
            {
                Log.DoLogCleanUp(MaxLogEntries!);
            }
            int port = Port;
            string[] PortArg = new string[] { "--urls", "http://*:" + port };

            var builder = WebApplication.CreateBuilder(PortArg);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            //app.UseHttpsRedirection();

            //app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
