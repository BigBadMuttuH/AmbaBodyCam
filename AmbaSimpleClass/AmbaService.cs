using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AmbaSimpleClass
{
    public class AmbaService
    {
        private readonly AmbaSettings _settings;
        private readonly Action<string> _logger;
        private CancellationTokenSource? _cts;

        public AmbaService(Action<string>? logger = null)
        {
            _logger = logger ?? Console.WriteLine;
            _settings = AmbaSettings.Load();
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => Run(_cts.Token)); // Запускаем сервис в фоновом потоке
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

                    using AmbaDevice device = new AmbaDevice(logger: _logger);
                    if (!device.Connect())
                    {
                        _logger("⚠️ Устройство не найдено. Повторная попытка через 5 минут...");
                        WaitWithCancellation(TimeSpan.FromMinutes(5), token);
                        continue;
                    }

                    string? deviceId = device.GetDeviceId();
                    if (deviceId == null)
                    {
                        _logger("❌ Не удалось получить ID устройства.");
                        WaitWithCancellation(TimeSpan.FromMinutes(5), token);
                        continue;
                    }

                    device.EnterStorageMode();
                    WaitWithCancellation(TimeSpan.FromSeconds(5), token); // Ждём подключения в режиме накопителя

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
                    WaitWithCancellation(TimeSpan.FromMinutes(_settings.SyncIntervalMinutes), token);
                }
            }
            catch (TaskCanceledException)
            {
                _logger("✅ Сервис остановлен (TaskCanceledException).");
            }
            catch (Exception ex)
            {
                _logger($"❌ Ошибка в сервисе: {ex.Message}");
            }
        }

        private void WaitWithCancellation(TimeSpan delay, CancellationToken token)
        {
            try
            {
                Task.Delay(delay, token).Wait(token);
            }
            catch (TaskCanceledException) { }
        }
    }
}
