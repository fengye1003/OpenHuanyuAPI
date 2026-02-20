namespace HuanyuAPI
{
    public class Localization
    {
        const string UndefinedKey = "_UNDEFINED";
        #region Languages
        static Dictionary<string, string> ZH_CN = new()
        {
            { UndefinedKey, "错误 - 找不到文本"},
            { "SelectLanguage", "请选择你的语言。"},
            { "LanguageKey", "输入 zhcn 并回车 - 简体中文"},
            { "inputError", "输入有误！请重试！"},
            { "propertyConvertError", "配置项转换为数值时出错，正在使用初始值重写文件。详情"},
            { "portOutOfRangeError", "端口数值不可用！端口号必须介于1~65535！正在使用初始值重写文件。引发错误的端口号："},
            { "unsupportedLanguageOnSetting", "不支持的语言！请从以上列表中选择一个语言并重试。引发错误的字符串："},
            { "inputPortNum", "\n\n确认端口号\n" +
                "请输入你希望运行在的端口号，端口号应当介于1~65535间。\n" +
                "端口号："},
            { "inputEnableLogs", "\n\n是否启用日志输出？\n" +
                "如果启用日志，当API被调用时、发生错误时和用户输入反馈将被输出到控制台以供查阅。\n" +
                "如需启用日志，请输入1，否则请输入0.\n" +
                "输入你的选择："},
            { "inputEnableLogWrite", "\n\n是否允许将日志保存到本地？\n" +
                "如果允许，日志将被保存到运行目录的 Log 文件夹下。\n" +
                "如需允许保存，请输入1，否则请输入0.\n" +
                "输入你的选择："},
            { "inputMaxLogEntries", "\n\n是否开启日志自动清理？\n" +
                "自动清理可以帮助您节省空间，自动删除若干天以外的过时日志。\n" +
                "如需开启日志自动清理，请输入需要保存的日志文件个数（每天一个文件）。输入 0 将会禁用此功能。\n" +
                "输入参数："},
            { "initializeComplete", "\n\n成功完成配置所有配置项！已保存配置。即将启动主程序..."},
            { "logAction", "来自{user}的用户访问了{name}。"}
        };
        static Dictionary<string, string> EN_US = new()
        {
            { UndefinedKey, "ERROR - No text available."},
            { "SelectLanguage", "Please select your language."},
            { "LanguageKey", "Input enus and enter - English"},
            { "inputError", "Invalid input. Please try again."},
            { "propertyConvertError", "Error converting a property to Int32, replacing it with default value. Details"},
            { "portOutOfRangeError", "Port number unavailable! Port number must be in [1,65535]. Replacing it with default value. Number causing this: "},
            { "unsupportedLanguageOnSetting", "Unsupported language. Please check all available in the list above and try again. Input causing error: "},
            { "inputPortNum", "\n\nSet port number\n" +
                "Enter port number the app should runs on. Port number should be in [1,65535].\n" +
                "Port："},
            { "inputEnableLogs", "\n\nEnable Logs Output?\n" +
                "If enabled, the console will print API-using event, error logs and users' action report.\n" +
                "Input 1 to enable. Input 0 to disable.\n" +
                "Input your choice: "},
            { "inputEnableLogWrite", "\n\nEnable Writing Logs to Disk?\n" +
                "If enabled, log files will be written to Log folder in running directory.\n" +
                "Input 1 to enable. Input 0 to disable.\n" +
                "Input your choice: "},
            { "inputMaxLogEntries", "\n\nEnable Logs Auto-Cleanup?\n" +
                "Auto cleanup can help you saving disk space by deleting expired logfiles after several days.\n" +
                "Input how many logfiles you want to save to enable this function (One new file will be created per day). Input 0 to disable this function.\n" +
                "Input value: "},
            { "initializeComplete", "\n\nCompleted all configurations! Config saved. Launching main program..."},
            { "logAction", "User from {user} accessed {name}."}
        };
        #endregion
        public enum Language
        {
            zh_CN,
            en_US
        }
        static List<Dictionary<string, string>> SupportedLanguageDics = new List<Dictionary<string, string>>
        {
            ZH_CN!,
            EN_US!
        };
        public Language ApplyLanguage;
        public Localization(Language lang)
        {
            ApplyLanguage = lang;
        }
        public Localization(string lang)
        {
            switch (lang)
            {
                case "zhcn":
                    ApplyLanguage = Language.zh_CN;
                    break;
                case "enus":
                    ApplyLanguage = Language.en_US;
                    break;
                default:
                    ApplyLanguage = Language.en_US;
                    break;
            }
        }
        public Localization()
        {

        }
        public string FetchString(string StringName)
        {
            string result;
            switch (ApplyLanguage)
            {
                case Language.zh_CN:
                    if (!ZH_CN.TryGetValue(StringName, out result!)) return ZH_CN[UndefinedKey];
                    return result;
                case Language.en_US:
                    if (!EN_US.TryGetValue(StringName, out result!)) return EN_US[UndefinedKey];
                    return result;
                default:
                    return "Unsupported Language Text";
            }
        }
        public static List<string> FetchAllTexts(string StringName)
        {
            List<string> result = new List<string>();
            foreach (Dictionary<string, string> dic in SupportedLanguageDics)
            {
                string fetchStr = "UNDEFINED";
                if (!dic.TryGetValue(StringName, out fetchStr!))
                    dic.TryGetValue(UndefinedKey, out fetchStr!);
                result.Add(fetchStr!);
            }
            return result;
        }

        
    }
}
