using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.IO;

namespace Emby.AITranslationScraper
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        // 插件唯一标识
        public override Guid Id => new Guid("8F2E1A0D-3B7C-4567-890A-1234567890AB");
        
        // 插件名称
        public override string Name => "AI翻译刮削插件";
        
        // 插件描述
        public override string Description => "适配多AI翻译接口的Emby元数据刮削插件，支持多数据源、人脸识别、备份等功能";
        
        // 插件版本
        public override Version Version => new Version("1.0.0");

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) 
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        // 单例实例
        public static Plugin Instance { get; private set; }

        // 配置页面
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "AITranslationScraperConfig",
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.ConfigurationPage.html"
                }
            };
        }
    }
}