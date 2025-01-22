using LibUsbDotNet;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AmbaSimpleClass
{
    public class AmbaService
    {
        private readonly AmbaSettings _settings;
        private readonly Action<string> _logger;
        private readonly LogLevel _logLevel;
        private CancellationTokenSource? _cts;

        public AmbaService(Action<string>? logger = null, LogLevel logLevel = LogLevel.None)
        {
            _logger = logger ?? Console.WriteLine;
            _logLevel = logLevel;
            _settings = AmbaSettings.Load();
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => Run(_cts.Token));
        }

        public void Stop()
        {
            if (_cts != null)
            {
                _logger("🛑 Остановка сервиса...");
                _cts.Cancel();
            }
        }

        private void Run(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    _logger($"⏳ Запуск синхронизации ({DateTime.Now:HH:mm:ss})");

                    using AmbaDevice device = new AmbaDevice(_logLevel, _logger);
                    if (!device.Connect())
                    {
                        _logger("⚠️ Устройство не найдено. Повторная попытка через 5 минут...");
                        Task.Delay(TimeSpan.FromMinutes(5), token).Wait(token);
                        continue;
                    }

                    string? deviceId = device.GetDeviceId();
                    if (deviceId == null)
                    {
                        _logger("❌ Не удалось получить ID устройства.");
                        Task.Delay(TimeSpan.FromMinutes(5), token).Wait(token);
                        continue;
                    }

                    device.EnterStorageMode();
                    Task.Delay(5000, token).Wait(token);

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
                    Task.Delay(TimeSpan.FromMinutes(_settings.SyncIntervalMinutes), token).Wait(token);
                }
            }
            catch (TaskCanceledException)
            {
                _logger("✅ Сервис остановлен.");
            }
            catch (Exception ex)
            {
                _logger($"❌ Ошибка в сервисе: {ex.Message}");
            }
        }
    }
}
