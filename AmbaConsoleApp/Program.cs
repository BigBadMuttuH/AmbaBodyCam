using System;
using System.Threading;
using AmbaSimpleClass;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        AmbaService service = new AmbaService(Console.WriteLine);

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
            Thread.Sleep(1000); // Держим приложение запущенным
        }
    }
}
