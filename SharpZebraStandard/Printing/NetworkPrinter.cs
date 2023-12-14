using System.Net.Sockets;

namespace SharpZebra.Printing;


public class NetworkPrinter : IZebraPrinter
{
    public PrinterSettings Settings { get; set; }

    public NetworkPrinter(PrinterSettings settings)
    {
        Settings = settings;
    }

    public bool? Print(byte[] data)
    {
        using var printer = new TcpClient(Settings.PrinterName, Settings.PrinterPort);
        using (var stream = printer.GetStream())
        {
            stream.Write(data, 0, data.Length);
            stream.Close();
        }
        printer.Close();
        return null;
    }
}
