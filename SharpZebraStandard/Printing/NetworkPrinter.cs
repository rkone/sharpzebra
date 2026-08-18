using System.Net.Sockets;
using System.Threading.Tasks;

namespace SharpZebra.Printing;


public class NetworkPrinter : IZebraPrinter
{
    public PrinterSettings Settings { get; set; }

    /// <summary>
    /// Creates a printer that sends data to a network-attached Zebra printer over a raw TCP socket.
    /// </summary>
    /// <param name="settings">Settings identifying the printer: PrinterName is the hostname or IP address,
    /// PrinterPort is the raw printing port (usually 9100)</param>
    public NetworkPrinter(PrinterSettings settings)
    {
        Settings = settings;
    }

    /// <summary>
    /// Sends the given data to the printer.
    /// </summary>
    /// <param name="data">The EPL2/ZPLII bytes to send</param>
    /// <returns>True if the data was sent successfully, false if the connection failed</returns>
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

    /// <summary>
    /// Sends the given data to the printer asynchronously.
    /// </summary>
    /// <param name="data">The EPL2/ZPLII bytes to send</param>
    /// <returns>True if the data was sent successfully, false if the connection failed</returns>
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
