using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Emby.AITranslationScraper.Core.Scraper
{
    public class MetadataScraper
    {
        private readonly PluginConfiguration _config;
        private readonly TranslationClient _translationClient;
        private readonly DataSourceClient _dataSourceClient;

        public MetadataScraper(PluginConfiguration config)
        {
            _config = config;
            _translationClient = new TranslationClient(config);
            _dataSourceClient = new DataSourceClient(config);
        }

        // 按文件名刮削元数据
        public async Task<Dictionary<string, object>> ScrapeByFileNameAsync(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            // 解析文件名（提取标题）
            var title = ParseFileName(fileName);
            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentException("无法从文件名解析出标题");
            }

            // 多数据源获取元数据
            var metadataList = new List<Dictionary<string, object>>();
            foreach (var source in _config.EnabledDataSources)
            {
                try
                {
                    var metadata = await _dataSourceClient.GetMetadataAsync(source, title);
                    if (metadata != null && IsMetadataMatch(metadata, title))
                    {
                        metadataList.Add(metadata);
                    }
                }
                catch
                {
                    // 忽略单个数据源失败
                }
            }

            // 无匹配元数据则丢弃
            if (!metadataList.Any())
            {
                return null;
            }

            // 合并元数据 + AI翻译
            var mergedMetadata = MergeMetadata(metadataList);
            if (_config.EnableAITranslation)
            {
                mergedMetadata["Title"] = await _translationClient.TranslateAsync(mergedMetadata["Title"].ToString());
                mergedMetadata["Overview"] = await _translationClient.TranslateAsync(mergedMetadata["Overview"].ToString());
            }

            return mergedMetadata;
        }

        // 解析文件名（正则）
        private string ParseFileName(string fileName)
        {
            var regex = new Regex(_config.FileNameRegex);
            var match = regex.Match(fileName);
            return match.Success ? match.Groups["title"].Value : null;
        }

        // 元数据匹配校验
        private bool IsMetadataMatch(Dictionary<string, object> metadata, string title)
        {
            var metadataTitle = metadata["Title"].ToString();
            var similarity = CalculateSimilarity(title, metadataTitle);
            return similarity >= _config.MatchSimilarityThreshold;
        }

        // 计算字符串相似度（简单版）
        private float CalculateSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0;
            var maxLen = Math.Max(s1.Length, s2.Length);
            var matchCount = s1.Zip(s2, (c1, c2) => c1 == c2).Count(x => x);
            return (float)matchCount / maxLen;
        }

        // 合并多数据源元数据
        private Dictionary<string, object> MergeMetadata(List<Dictionary<string, object>> metadataList)
        {
            var merged = new Dictionary<string, object>();
            // 优先取第一个数据源的基础信息，补充其他数据源的图片/演员信息
            merged = metadataList.First().ToDictionary(kv => kv.Key, kv => kv.Value);

            foreach (var metadata in metadataList.Skip(1))
            {
                // 补充演员信息
                if (metadata.ContainsKey("Actors") && !merged.ContainsKey("Actors"))
                {
                    merged["Actors"] = metadata["Actors"];
                }

                // 补充图片
                if (metadata.ContainsKey("Images") && !merged.ContainsKey("Images"))
                {
                    merged["Images"] = metadata["Images"];
                }
                else if (metadata.ContainsKey("Images") && merged.ContainsKey("Images"))
                {
                    var existingImages = (List<string>)merged["Images"];
                    var newImages = (List<string>)metadata["Images"];
                    merged["Images"] = existingImages.Concat(newImages).Distinct().ToList();
                }
            }

            return merged;
        }
    }
}