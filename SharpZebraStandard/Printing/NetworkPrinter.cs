using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SharpZebra.Printing;


public class NetworkPrinter : IZebraPrinter
{
    /// <summary>Once the printer has started replying, stop reading after it has been quiet for this long</summary>
    private const int IdleTimeoutMilliseconds = 500;
    private const int ReadBufferSize = 4096;

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

    /// <summary>
    /// Sends data to the printer and returns whatever the printer sends back over the same connection, for host
    /// commands such as ^HW (directory listing) or ~HS (status). Reading stops once the printer has replied and
    /// then been quiet for half a second.
    /// </summary>
    /// <param name="data">The EPL2/ZPLII bytes to send</param>
    /// <param name="timeoutMilliseconds">How long to wait for the printer to connect and start replying</param>
    /// <returns>The raw response bytes, or null if the printer could not be reached or did not reply in time</returns>
    public byte[]? Query(byte[] data, int timeoutMilliseconds = 5000)
    {
        try
        {
            using var printer = new TcpClient();
            var connect = printer.BeginConnect(Settings.PrinterName, Settings.PrinterPort, null, null);
            if (!connect.AsyncWaitHandle.WaitOne(timeoutMilliseconds))
                return null; // disposing the client abandons the pending connect
            printer.EndConnect(connect);

            using var stream = printer.GetStream();
            stream.Write(data, 0, data.Length);

            var response = new MemoryStream();
            var buffer = new byte[ReadBufferSize];
            stream.ReadTimeout = timeoutMilliseconds;
            try
            {
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    response.Write(buffer, 0, read);
                    stream.ReadTimeout = IdleTimeoutMilliseconds;
                }
            }
            catch (IOException) { } // read timed out: the printer has finished (or never started) replying
            return response.Length == 0 ? null : response.ToArray();
        }
        catch (SocketException)
        {
            return null;
        }
    }

    /// <summary>
    /// Sends data to the printer asynchronously and returns whatever the printer sends back. See Query.
    /// </summary>
    /// <param name="data">The EPL2/ZPLII bytes to send</param>
    /// <param name="timeoutMilliseconds">How long to wait for the printer to connect and start replying</param>
    /// <returns>The raw response bytes, or null if the printer could not be reached or did not reply in time</returns>
    public async Task<byte[]?> QueryAsync(byte[] data, int timeoutMilliseconds = 5000)
    {
        using var printer = new TcpClient();
        try
        {
            var connectTask = printer.ConnectAsync(Settings.PrinterName, Settings.PrinterPort);
            if (await Task.WhenAny(connectTask, Task.Delay(timeoutMilliseconds)).ConfigureAwait(false) != connectTask)
            {
                _ = connectTask.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                return null; // disposing the client abandons the pending connect
            }
            await connectTask.ConfigureAwait(false);

            using var stream = printer.GetStream();
            await stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);

            var response = new MemoryStream();
            var buffer = new byte[ReadBufferSize];
            var timeout = timeoutMilliseconds;
            while (true)
            {
                var readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                if (await Task.WhenAny(readTask, Task.Delay(timeout)).ConfigureAwait(false) != readTask)
                {
                    // Timed out: the printer has finished (or never started) replying. Closing the socket below
                    // faults the pending read, so observe it to avoid an unobserved task exception.
                    _ = readTask.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                    break;
                }
                var read = await readTask.ConfigureAwait(false);
                if (read == 0) break;
                response.Write(buffer, 0, read);
                timeout = IdleTimeoutMilliseconds;
            }
            return response.Length == 0 ? null : response.ToArray();
        }
        catch (SocketException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        finally
        {
            printer.Close();
        }
    }
}
