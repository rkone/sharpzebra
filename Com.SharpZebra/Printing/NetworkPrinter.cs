using System.IO;
using System.Net.Sockets;

namespace SharpZebra.Printing
{

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
            using (var printer = new TcpClient(Settings.PrinterName, Settings.PrinterPort))
            {
                using (var stream = printer.GetStream())
                {
                    stream.Write(data, 0, data.Length);
                    stream.Close();
                }
                printer.Close();
            }
            return null;
        }

        /// <summary>
        /// Sends data to the printer and returns whatever the printer sends back over the same connection, for host
        /// commands such as ^HW (directory listing) or ~HS (status). Reading stops once the printer has replied and
        /// then been quiet for half a second.
        /// </summary>
        /// <param name="data">The EPL2/ZPLII bytes to send</param>
        /// <param name="timeoutMilliseconds">How long to wait for the printer to connect and start replying</param>
        /// <returns>The raw response bytes, or null if the printer could not be reached or did not reply in time</returns>
        public byte[] Query(byte[] data, int timeoutMilliseconds = 5000)
        {
            try
            {
                using (var printer = new TcpClient())
                {
                    var connect = printer.BeginConnect(Settings.PrinterName, Settings.PrinterPort, null, null);
                    if (!connect.AsyncWaitHandle.WaitOne(timeoutMilliseconds, false))
                        return null; // disposing the client abandons the pending connect
                    printer.EndConnect(connect);

                    using (var stream = printer.GetStream())
                    {
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
                }
            }
            catch (SocketException)
            {
                return null;
            }
        }
    }
}
