using System.Text;
using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

namespace AmbaSimpleClass;

public class AmbaDevice : IDisposable
{
    private const int VendorId = 0x4255;
    private const int ProductId = 0x0001;
    private readonly UsbContext _context;
    private UsbDevice? _device;
    private UsbEndpointReader? _reader;
    private UsbEndpointWriter? _writer;

    public AmbaDevice()
    {
        _context = new UsbContext();
        // _context.SetDebugLevel(LogLevel.Debug);
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
            Console.WriteLine("❌ Устройство не найдено!");
            return false;
        }

        if (!_device.TryOpen())
        {
            Console.WriteLine("⚠️ Не удалось открыть устройство.");
            return false;
        }

        Console.WriteLine("✅ Устройство подключено!");
        Console.WriteLine($"🔍 Info: {_device.Info}");
        Console.WriteLine($"🔌 LocationId: {_device.BusNumber}");

        // Проверяем наличие конфигурации и интерфейсов
        if (_device.Configs.Count > 0 && _device.Configs[0].Interfaces.Count > 0)
        {
            _device.ClaimInterface(_device.Configs[0].Interfaces[0].Number);
        }
        else
        {
            Console.WriteLine("⚠️ Устройство не поддерживает конфигурацию интерфейса.");
            return false;
        }

        // Инициализируем Endpoints (0x01 - OUT, 0x81 - IN)
        _writer = _device.OpenEndpointWriter((WriteEndpointID)0x01);
        _reader = _device.OpenEndpointReader((ReadEndpointID)0x81);

        Console.WriteLine("🔑 Входим на устройство...");
        var loginResponse = SendCommand("@Ver;8;00000000;#");

        if (!loginResponse.Contains("Ver;OK"))
        {
            Console.WriteLine("❌ Ошибка авторизации! Устройство не отвечает.");
            return false;
        }

        Console.WriteLine("✅ Авторизация успешна!");
        return true;
    }

    /// <summary>
    ///     Отправляет команду и получает ответ
    /// </summary>
    public string SendCommand(string command)
    {
        if (_writer == null || _reader == null)
        {
            Console.WriteLine("⚠️ Нет соединения с устройством.");
            return string.Empty;
        }

        Console.WriteLine($"📡 Отправка: {command}");

        var data = Encoding.ASCII.GetBytes(command);
        var writeStatus = _writer.Write(data, 3000, out var bytesWritten);

        if (writeStatus != Error.Success)
        {
            Console.WriteLine($"❌ Ошибка записи: {writeStatus}");
            return string.Empty;
        }

        Console.WriteLine($"✅ Записано байт: {bytesWritten}");

        var readBuffer = new byte[64];
        var readStatus = _reader.Read(readBuffer, 3000, out var bytesRead);

        if (readStatus != Error.Success)
        {
            Console.WriteLine($"❌ Ошибка чтения: {readStatus}");
            return string.Empty;
        }

        var response = Encoding.ASCII.GetString(readBuffer, 0, bytesRead).Trim();
        Console.WriteLine($"📩 Ответ: {response}");

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

        Console.WriteLine($"📅 Устанавливаем текущую дату: {date}");
        SendCommand($"@Sdt;8;{date};#");

        Console.WriteLine($"⏰ Устанавливаем текущее время: {time}");
        SendCommand($"@Stm;6;{time};#");
    }

    /// <summary>
    ///     Переводит устройство в режим медиа-устройства
    /// </summary>
    public void EnterStorageMode()
    {
        Console.WriteLine("🔄 Переводим в режим накопителя...");
        SendCommand("@ATH;8;12345678;#");
    }
    
    /// <summary>
    /// Получает ID устройства
    /// </summary>
    public string GetDeviceId()
    {
        Console.WriteLine("🔎 Запрашиваем ID устройства...");
        string response = SendCommand("@Gdv;8;12345678;#");

        if (response.StartsWith("@Gdv"))
        {
            string deviceId = response.Split(';')[2].Trim('#');
            Console.WriteLine($"🆔 ID устройства: {deviceId}");
            return deviceId;
        }

        Console.WriteLine("⚠️ Ошибка получения ID.");
        return string.Empty;
    }
    
    /// <summary>
    /// Переключает устройство в рабочий режим, если оно уже в режиме накопителя
    /// </summary>
    public void EnsureWorkingMode()
    {
        string response = SendCommand("@Gdv;8;12345678;#");

        if (string.IsNullOrEmpty(response) || response.Contains("ERROR"))
        {
            Console.WriteLine("🔄 Устройство в режиме накопителя. Переключаем обратно...");
            SendCommand("@ATH;8;12345678;#");
            Thread.Sleep(2000);
        }
        else
        {
            Console.WriteLine("✅ Устройство уже в рабочем режиме.");
        }
    }
}