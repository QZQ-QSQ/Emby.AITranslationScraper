using System;
using System.Net;
using System.Net.Sockets;

namespace Emby.AITranslationScraper.Core.Proxy
{
    public static class ProxyHandler
    {
        // 获取配置的代理对象
        public static IWebProxy GetProxy(PluginConfiguration config)
        {
            if (!config.EnableProxy || string.IsNullOrEmpty(config.ProxyHost))
            {
                return null;
            }

            var proxyUri = new Uri($"{config.ProxyType}://{config.ProxyHost}:{config.ProxyPort}");
            var webProxy = new WebProxy(proxyUri)
            {
                BypassProxyOnLocal = true
            };

            // 代理认证
            if (!string.IsNullOrEmpty(config.ProxyUsername) && !string.IsNullOrEmpty(config.ProxyPassword))
            {
                webProxy.Credentials = new NetworkCredential(config.ProxyUsername, config.ProxyPassword);
            }

            // 白名单处理（跳过指定域名的代理）
            webProxy.BypassList = config.ProxyWhitelist.ToArray();

            return webProxy;
        }

        // 测试代理连通性
        public static bool TestProxy(PluginConfiguration config)
        {
            try
            {
                var proxy = GetProxy(config);
                if (proxy == null) return false;

                using (var client = new WebClient { Proxy = proxy })
                {
                    client.DownloadString("https://www.baidu.com");
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}