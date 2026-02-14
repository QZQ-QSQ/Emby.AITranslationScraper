using Newtonsoft.Json;
using RestSharp;
using System;
using System.Threading.Tasks;

namespace Emby.AITranslationScraper.Core.AITranslation
{
    public class TranslationClient
    {
        private readonly PluginConfiguration _config;
        private readonly TranslationCache _cache;

        public TranslationClient(PluginConfiguration config)
        {
            _config = config;
            _cache = new TranslationCache();
        }

        // 统一翻译入口
        public async Task<string> TranslateAsync(string text, string targetLang = "zh-CN")
        {
            // 优先从缓存获取
            if (_config.EnableTranslationCache)
            {
                var cached = _cache.GetCachedTranslation(text, _config.TranslationProvider);
                if (!string.IsNullOrEmpty(cached))
                {
                    return cached;
                }
            }

            string result = _config.TranslationProvider switch
            {
                "deepseek" => await TranslateWithDeepSeekAsync(text, targetLang),
                "openai" => await TranslateWithOpenAIAsync(text, targetLang),
                "google" => await TranslateWithGoogleAsync(text, targetLang),
                _ => throw new NotSupportedException($"不支持的翻译接口：{_config.TranslationProvider}")
            };

            // 学术化处理（中性化脏话/性相关内容）
            result = AcademicFilter(result);

            // 缓存结果
            if (_config.EnableTranslationCache)
            {
                _cache.CacheTranslation(text, _config.TranslationProvider, result);
            }

            return result;
        }

        // DeepSeek翻译
        private async Task<string> TranslateWithDeepSeekAsync(string text, string targetLang)
        {
            var client = new RestClient(_config.ApiEndpoint ?? "https://api.deepseek.com/v1/chat/completions");
            // 配置代理
            client.Proxy = ProxyHandler.GetProxy(_config);

            var request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {_config.ApiKey}");
            request.AddHeader("Content-Type", "application/json");

            var body = new
            {
                model = "deepseek-chat",
                messages = new[]
                {
                    new { role = "user", content = $"以学术、中性的角度翻译以下文本（避免脏话、性相关的直白表述），目标语言：{targetLang}：{text}" }
                },
                temperature = 0.1
            };

            request.AddJsonBody(body);

            // 重试逻辑
            for (int i = 0; i < _config.TranslationRetryCount; i++)
            {
                try
                {
                    var response = await client.PostAsync(request);
                    if (response.IsSuccessful)
                    {
                        var data = JsonConvert.DeserializeObject<dynamic>(response.Content);
                        return data.choices[0].message.content.ToString().Trim();
                    }
                }
                catch (Exception ex)
                {
                    if (i == _config.TranslationRetryCount - 1)
                    {
                        throw new Exception($"DeepSeek翻译失败：{ex.Message}");
                    }
                    await Task.Delay(1000 * (i + 1)); // 指数退避
                }
            }

            throw new Exception("DeepSeek翻译重试次数耗尽");
        }

        // OpenAI翻译（适配逻辑同DeepSeek，仅调整请求参数）
        private async Task<string> TranslateWithOpenAIAsync(string text, string targetLang)
        {
            var client = new RestClient(_config.ApiEndpoint ?? "https://api.openai.com/v1/chat/completions");
            client.Proxy = ProxyHandler.GetProxy(_config);

            var request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {_config.ApiKey}");
            request.AddHeader("Content-Type", "application/json");

            var body = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "user", content = $"以学术、中性的角度翻译以下文本（避免脏话、性相关的直白表述），目标语言：{targetLang}：{text}" }
                },
                temperature = 0.1
            };

            request.AddJsonBody(body);

            // 重试逻辑（同DeepSeek）
            for (int i = 0; i < _config.TranslationRetryCount; i++)
            {
                try
                {
                    var response = await client.PostAsync(request);
                    if (response.IsSuccessful)
                    {
                        var data = JsonConvert.DeserializeObject<dynamic>(response.Content);
                        return data.choices[0].message.content.ToString().Trim();
                    }
                }
                catch (Exception ex)
                {
                    if (i == _config.TranslationRetryCount - 1)
                    {
                        throw new Exception($"OpenAI翻译失败：{ex.Message}");
                    }
                    await Task.Delay(1000 * (i + 1));
                }
            }

            throw new Exception("OpenAI翻译重试次数耗尽");
        }

        // 谷歌翻译（简化版，可根据实际API调整）
        private async Task<string> TranslateWithGoogleAsync(string text, string targetLang)
        {
            // 此处为简化实现，实际需对接谷歌翻译API
            var client = new RestClient("https://translation.googleapis.com/language/translate/v2");
            client.Proxy = ProxyHandler.GetProxy(_config);

            var request = new RestRequest();
            request.AddParameter("key", _config.ApiKey);
            request.AddParameter("q", text);
            request.AddParameter("target", targetLang);

            var response = await client.GetAsync(request);
            if (response.IsSuccessful)
            {
                var data = JsonConvert.DeserializeObject<dynamic>(response.Content);
                var rawResult = data.data.translations[0].translatedText.ToString();
                return AcademicFilter(rawResult);
            }

            throw new Exception("谷歌翻译请求失败");
        }

        // 学术化过滤（中性化处理）
        private string AcademicFilter(string text)
        {
            // 示例规则：替换脏话/性相关直白表述为中性学术词汇
            var filterRules = new Dictionary<string, string>
            {
                { "妈的", "不恰当的语气词" },
                { "操", "不恰当的动词" },
                { "性交", "性行为" },
                { "嫖娼", "性交易行为" }
                // 可扩展更多规则
            };

            foreach (var rule in filterRules)
            {
                text = text.Replace(rule.Key, rule.Value);
            }

            return text;
        }
    }
}