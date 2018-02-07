using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace Com.SharpZebra.Printing
{

    public class NetworkPrinter: IZebraPrinter
    {
        public PrinterSettings Settings { get; set; }

        public NetworkPrinter(PrinterSettings settings)
        {
            Settings = settings;
        }

        public bool? Print(byte[] data)
        {
            using (TcpClient printer = new TcpClient(Settings.PrinterName, Settings.PrinterPort))
            {
                using (NetworkStream strm = printer.GetStream())
                {
                    strm.Write(data, 0, data.Length);
                    strm.Close();
                }
                printer.Close();
            }
            return null;
        }
    }
}
