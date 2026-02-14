using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Emby.AITranslationScraper.Core.Backup
{
    public class DatabaseBackup
    {
        private readonly PluginConfiguration _config;

        public DatabaseBackup(PluginConfiguration config)
        {
            _config = config;
            // 确保备份目录存在
            Directory.CreateDirectory(_config.BackupPath);
        }

        // 执行备份
        public async Task<string> BackupAsync()
        {
            try
            {
                // 生成带时间戳的备份文件名
                var backupFileName = $"EmbyScraperBackup_{DateTime.Now:yyyyMMddHHmmss}.zip";
                var backupPath = Path.Combine(_config.BackupPath, backupFileName);

                // 模拟备份逻辑（实际需替换为Emby元数据数据库备份）
                // 此处为示例：复制元数据目录到备份文件
                var embyDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Emby", "data");
                if (Directory.Exists(embyDataPath))
                {
                    await Task.Run(() =>
                    {
                        CopyDirectory(embyDataPath, Path.Combine(_config.BackupPath, $"temp_{DateTime.Now.Ticks}"));
                        // 压缩逻辑（可使用System.IO.Compression）
                    });
                }

                // 清理旧备份
                CleanOldBackups();

                return backupPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"备份失败：{ex.Message}");
            }
        }

        // 清理过期备份
        private void CleanOldBackups()
        {
            var cutoffDate = DateTime.Now.AddDays(-_config.BackupRetentionDays);
            var backupFiles = Directory.GetFiles(_config.BackupPath, "EmbyScraperBackup_*.zip")
                                       .Where(f => File.GetCreationTime(f) < cutoffDate);

            foreach (var file in backupFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // 忽略删除失败的文件
                }
            }
        }

        // 目录复制（辅助方法）
        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }

        // 自动备份定时器
        public void StartAutoBackup()
        {
            if (!_config.AutoBackup) return;

            var timer = new System.Timers.Timer(_config.AutoBackupIntervalHours * 3600 * 1000);
            timer.Elapsed += async (sender, e) => await BackupAsync();
            timer.Start();
        }
    }
}