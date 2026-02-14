using MediaBrowser.Model.Plugins;
using System;
using System.Collections.Generic;

namespace Emby.AITranslationScraper
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        // AI翻译配置
        public bool EnableAITranslation { get; set; } = false;
        public string TranslationProvider { get; set; } = "deepseek"; // deepseek/openai/google
        public string ApiKey { get; set; } = string.Empty;
        public string ApiEndpoint { get; set; } = string.Empty;
        public int TranslationTimeout { get; set; } = 10; // 超时秒数
        public int TranslationRetryCount { get; set; } = 2;
        public bool EnableTranslationCache { get; set; } = true;

        // 数据源配置
        public List<string> CustomDataSources { get; set; } = new List<string>();
        public List<string> EnabledDataSources { get; set; } = new List<string>();
        public List<string> DomainKeywords { get; set; } = new List<string>();
        public string MatchMode { get; set; } = "contains"; // contains/equals/regex

        // 演员元数据配置
        public bool EnableActorMetadata { get; set; } = true;
        public bool EnableActorImageCache { get; set; } = true;
        public string ActorImagePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EmbyActorImages");

        // 备份配置
        public bool EnableBackup { get; set; } = true;
        public string BackupPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EmbyScraperBackup");
        public int BackupRetentionDays { get; set; } = 7;
        public bool AutoBackup { get; set; } = true;
        public int AutoBackupIntervalHours { get; set; } = 24;

        // 代理配置
        public bool EnableProxy { get; set; } = false;
        public string ProxyType { get; set; } = "http"; // http/https/socks
        public string ProxyHost { get; set; } = string.Empty;
        public int ProxyPort { get; set; } = 8080;
        public string ProxyUsername { get; set; } = string.Empty;
        public string ProxyPassword { get; set; } = string.Empty;
        public List<string> ProxyWhitelist { get; set; } = new List<string>();

        // 人脸识别配置
        public bool EnableFaceRecognition { get; set; } = true;
        public float FaceConfidenceThreshold { get; set; } = 0.7f;
        public string FaceModelPath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "AITranslationScraper", "Models");
        public int FaceCropWidth { get; set; } = 200;
        public int FaceCropHeight { get; set; } = 200;

        // 刮削配置
        public string FileNameRegex { get; set; } = @"(?<title>.+)\.\w+$";
        public float MatchSimilarityThreshold { get; set; } = 0.8f;
        public bool EnableBulkScraping { get; set; } = true;
    }
}