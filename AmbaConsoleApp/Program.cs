using System;
using System.Threading;
using AmbaSimpleClass;
using LibUsbDotNet;
using LibUsbDotNet.LibUsb;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Только консольное приложение использует LogLevel.Debug
        AmbaService service = new AmbaService(Console.WriteLine, LogLevel.Debug);

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n🛑 Остановка сервиса...");
            service.Stop();
        };

        service.Start();
        Console.WriteLine("✅ Сервис запущен! Нажмите Ctrl+C для выхода.");

        while (true)
        {
            Thread.Sleep(1000);
        }
    }
}
