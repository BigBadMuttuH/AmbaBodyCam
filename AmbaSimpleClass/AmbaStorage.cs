using System;
using System.IO;
using System.Linq;

namespace AmbaSimpleClass
{
    public class AmbaStorage : IDisposable
    {
        private readonly string? _storagePath;
        private readonly string _deviceId;
        private readonly Action<string> _logger;

        public AmbaStorage(string deviceId, Action<string>? logger = null)
        {
            _logger = logger ?? Console.WriteLine;
            _deviceId = deviceId;
            _storagePath = FindAmbaStorage(deviceId);

            if (_storagePath == null)
            {
                _logger($"❌ Камера с ID {deviceId} в режиме накопителя не найдена.");
            }
            else
            {
                _logger($"✅ Камера найдена: {_storagePath}");
            }
        }

        private static string? FindAmbaStorage(string deviceId)
        {
            return DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Removable || d.DriveType == DriveType.Fixed)
                .FirstOrDefault(d => d.VolumeLabel.Equals(deviceId, StringComparison.OrdinalIgnoreCase))
                ?.RootDirectory.FullName;
        }

        public bool CopyFiles(string baseDestinationPath, string fileFormat)
        {
            if (_storagePath == null)
            {
                _logger("❌ Накопитель не найден!");
                return false;
            }

            var files = Directory.GetFiles(_storagePath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!files.Any())
            {
                _logger("⚠️ Нет фото или видео для копирования.");
                return false;
            }

            _logger($"📂 Найдено {files.Count} файлов. Начинаем копирование...");

            foreach (var file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                DateTime creationTime = fileInfo.CreationTime;

                string yearFolder = Path.Combine(baseDestinationPath, creationTime.Year.ToString());
                string dateFolder = Path.Combine(yearFolder, creationTime.ToString("yyyy-MM-dd"));

                string formattedFileName = fileFormat
                    .Replace("{TIME}", creationTime.ToString("HH-mm-ss"))
                    .Replace("{ID}", _deviceId)
                    .Replace("{EXT}", fileInfo.Extension);

                string destFile = Path.Combine(dateFolder, formattedFileName);

                Directory.CreateDirectory(dateFolder);
                File.Copy(file, destFile, overwrite: true);
                _logger($"✅ Скопировано: {formattedFileName}");
            }

            return true;
        }

        public void ClearStorage()
        {
            if (_storagePath == null)
            {
                _logger("❌ Накопитель не найден!");
                return;
            }

            var files = Directory.GetFiles(_storagePath, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!files.Any())
            {
                _logger("⚠️ Нет файлов для удаления.");
                return;
            }

            _logger($"🗑 Удаляем {files.Count} файлов...");

            foreach (var file in files)
            {
                File.Delete(file);
                _logger($"🗑 Удалено: {Path.GetFileName(file)}");
            }
        }

        public void Dispose() { }
    }
}
