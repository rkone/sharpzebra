using System.Net.Sockets;
using System.Threading.Tasks;

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
        bool success = false;
        try
        {
            using var printer = new TcpClient(Settings.PrinterName, Settings.PrinterPort);
            if (!printer.Connected)
                return false;
            using (var stream = printer.GetStream())
            {
                stream.Write(data, 0, data.Length);
                stream.Close();
            }
            printer.Close();
            success = true;
        }
        catch (SocketException) { }
        return success;        
    }

    public async Task<bool> PrintAsync(byte[] data)
    {
        using var printer = new TcpClient();
        bool success = false;
        try
        {
            await printer.ConnectAsync(Settings.PrinterName, Settings.PrinterPort);
            if (printer.Connected)
            {
                using var stream = printer.GetStream();
                await stream.WriteAsync(data, 0, data.Length);
                stream.Close();
            }
            success = true;
        }
        catch (SocketException) { }
        finally
        {
            printer.Close();
        }
        return success;
    }
}
