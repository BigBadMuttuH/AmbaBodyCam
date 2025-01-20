using System;
using System.IO;
using System.Threading;

namespace AmbaSimpleClass
{
    public class AmbaService
    {
        private readonly AmbaSettings _settings;
        private readonly Action<string> _logger;
        private bool _isRunning = true;

        public AmbaService(Action<string>? logger = null)
        {
            _logger = logger ?? Console.WriteLine;
            _settings = AmbaSettings.Load();
        }

        public void Run()
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _isRunning = false;
                _logger("🛑 Остановка сервиса...");
            };

            while (_isRunning)
            {
                _logger($"⏳ Запуск синхронизации ({DateTime.Now:HH:mm:ss})");

                using AmbaDevice device = new AmbaDevice(logger: _logger);
                if (!device.Connect())
                {
                    _logger("⚠️ Устройство не найдено. Повторная попытка через 5 минут...");
                    Thread.Sleep(TimeSpan.FromMinutes(5));
                    continue; // Пропускаем итерацию, но сервис продолжает работать
                }

                string? deviceId = device.GetDeviceId();
                if (deviceId == null)
                {
                    _logger("❌ Не удалось получить ID устройства.");
                    Thread.Sleep(TimeSpan.FromMinutes(5));
                    continue;
                }

                device.EnterStorageMode();
                Thread.Sleep(5000); // Ждём подключения в режиме накопителя

                using AmbaStorage storage = new AmbaStorage(deviceId, _logger);
                if (storage.CopyFiles(_settings.StoragePath, _settings.FileFormat))
                {
                    _logger($"✅ Файлы сохранены в: {_settings.StoragePath}");
                    if (_settings.DeleteAfterSync)
                    {
                        storage.ClearStorage();
                    }
                }

                _logger($"⏸ Ожидание {_settings.SyncIntervalMinutes} минут перед следующим запуском...");
                Thread.Sleep(TimeSpan.FromMinutes(_settings.SyncIntervalMinutes));
            }
        }
    }
}
