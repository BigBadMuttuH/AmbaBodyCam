using AmbaSimpleClass;

namespace AmbaConsoleApp;

public static class Program
{
    public static void Main(string[] args)
    {
        AmbaDevice device = new AmbaDevice();
        if(!device.Connect()) return;
        device.GetDeviceId();
        device.SetCurrentDateTime();
        device.EnterStorageMode();
    }
}

