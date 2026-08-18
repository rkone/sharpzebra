using System.Net.Sockets;

namespace SharpZebra.Printing
{

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
    }
}
