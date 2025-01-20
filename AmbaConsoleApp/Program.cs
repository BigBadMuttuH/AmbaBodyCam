using AmbaSimpleClass;
using LibUsbDotNet;
using System.Text;

namespace AmbaConsoleApp;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        AmbaService service = new AmbaService();
        service.Run();
    }
}

