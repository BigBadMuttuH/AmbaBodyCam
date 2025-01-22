using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using System.Text;

namespace AmbaSimpleClass;

public class AmbaDevice : IDisposable
{
    private const int VendorId = 0x4255;
    private const int ProductId = 0x0001;
    private readonly UsbContext _context;
    private UsbDevice? _device;
    private UsbEndpointWriter? _writer;
    private UsbEndpointReader? _reader;
    private readonly Action<string> _logger;

    /// <summary>
    /// Конструктор с возможностью передать уровень логирования и кастомный логгер.
    /// </summary>

    public AmbaDevice(LogLevel logLevel = LogLevel.None, Action<string>? logger = null)
    {
        _logger = logger ?? Console.WriteLine;

        _context = new UsbContext();
        _context.SetDebugLevel(logLevel); // Используем переданный уровень логирования
    }

    public void Dispose()
    {
        _device?.Close();
        _context?.Dispose();
    }

    /// <summary>
    ///     Подключается к устройству Amba Simple Class
    /// </summary>
    public bool Connect()
    {
        _device = _context.Find(dev => dev.VendorId == VendorId && dev.ProductId == ProductId) as UsbDevice;

        if (_device == null)
        {
            _logger("❌ Устройство не найдено!");
            return false;
        }

        if (!_device.TryOpen())
        {
            _logger("⚠️ Не удалось открыть устройство.");
            return false;
        }

        _logger("✅ Устройство подключено!");
        _logger($"🔍 Info: {_device.Info}");
        _logger($"🔌 LocationId: {_device.BusNumber}");

        // Проверяем наличие конфигурации и интерфейсов
        if (_device.Configs.Count > 0 && _device.Configs[0].Interfaces.Count > 0)
        {
            _device.ClaimInterface(_device.Configs[0].Interfaces[0].Number);
        }
        else
        {
            _logger("⚠️ Устройство не поддерживает конфигурацию интерфейса.");
            return false;
        }

        // Инициализируем Endpoints (0x01 - OUT, 0x81 - IN)
        _writer = _device.OpenEndpointWriter((WriteEndpointID)0x01);
        _reader = _device.OpenEndpointReader((ReadEndpointID)0x81);

        _logger("🔑 Входим на устройство...");
        var loginResponse = SendCommand("@Ver;8;00000000;#");

        if (!loginResponse.Contains("Ver;OK"))
        {
            _logger("❌ Ошибка авторизации! Устройство не отвечает.");
            return false;
        }

        _logger("✅ Авторизация успешна!");
        return true;
    }

    /// <summary>
    ///     Отправляет команду и получает ответ
    /// </summary>
    public string SendCommand(string command)
    {
        if (_writer == null || _reader == null)
        {
            _logger("⚠️ Нет соединения с устройством.");
            return string.Empty;
        }

        _logger($"📡 Отправка: {command}");

        var data = Encoding.ASCII.GetBytes(command);
        var writeStatus = _writer.Write(data, 3000, out var bytesWritten);

        if (writeStatus != Error.Success)
        {
            _logger($"❌ Ошибка записи: {writeStatus}");
            return string.Empty;
        }

        _logger($"✅ Записано байт: {bytesWritten}");

        var readBuffer = new byte[64];
        var readStatus = _reader.Read(readBuffer, 3000, out var bytesRead);

        if (readStatus != Error.Success)
        {
            _logger($"❌ Ошибка чтения: {readStatus}");
            return string.Empty;
        }

        var response = Encoding.ASCII.GetString(readBuffer, 0, bytesRead).Trim();
        _logger($"📩 Ответ: {response}");

        return response;
    }

    /// <summary>
    ///     Устанавливает текущую дату и время на устройстве
    /// </summary>
    public void SetCurrentDateTime()
    {
        var now = DateTime.Now;
        var date = now.ToString("yyyyMMdd"); // Год, месяц, день
        var time = now.ToString("HHmmss00"); // Часы, минуты, секунды + "00"

        _logger($"📅 Устанавливаем текущую дату: {date}");
        SendCommand($"@Sdt;8;{date};#");

        _logger($"⏰ Устанавливаем текущее время: {time}");
        SendCommand($"@Stm;6;{time};#");
    }

    /// <summary>
    ///     Переводит устройство в режим медиа-устройства
    /// </summary>
    public void EnterStorageMode()
    {
        _logger("🔄 Переводим в режим накопителя...");
        SendCommand("@ATH;8;12345678;#");
    }

    /// <summary>
    /// Получает ID устройства
    /// </summary>
    public string? GetDeviceId()
    {
        _logger("🔎 Запрашиваем ID устройства...");
        string response = SendCommand("@Gdv;8;12345678;#");

        if (response.StartsWith("@Gdv"))
        {
            string deviceId = response.Split(';')[2].Trim('#');
            _logger($"🆔 ID устройства: {deviceId}");
            return deviceId;
        }

        _logger("⚠️ Ошибка получения ID.");
        return null;
    }

    /// <summary>
    /// Переключает устройство в рабочий режим, если оно уже в режиме накопителя
    /// </summary>
    public void EnsureWorkingMode()
    {
        string response = SendCommand("@Gdv;8;12345678;#");

        if (string.IsNullOrEmpty(response) || response.Contains("ERROR"))
        {
            _logger("🔄 Устройство в режиме накопителя. Переключаем обратно...");
            SendCommand("@ATH;8;12345678;#");
            Thread.Sleep(2000);
        }
        else
        {
            _logger("✅ Устройство уже в рабочем режиме.");
        }
    }
}