using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SharpZebra.Printing;

namespace SharpZebra.Commands
{
    public partial class ZPLCommands
    {
        private static readonly Regex HostStatusBlock = new Regex("\x02([^\x03]*)\x03", RegexOptions.Compiled);

        /// <summary>
        /// Asks the printer for its status: media, ribbon, head, pause and buffer state and more. The printer answers
        /// on the same connection, so send this with IZebraPrinter.Query and pass the response to ParseHostStatus,
        /// or use the HostStatus overload that takes a printer to do both.
        /// ZPL Command: ~HS.
        /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf"/>
        /// </summary>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] HostStatus()
        {
            return Encoding.GetEncoding(850).GetBytes("~HS");
        }

        /// <summary>
        /// Gets the printer's status. Sends the ~HS host status command and parses the reply.
        /// Only printers that can return data support this: NetworkPrinter and USBPrinter can, SpoolPrinter cannot.
        /// </summary>
        /// <param name="printer">The printer to query</param>
        /// <param name="timeoutMilliseconds">How long to wait for the printer to connect and start replying</param>
        /// <returns>The printer's status, or null if the printer could not be reached or did not reply</returns>
        /// <exception cref="NotSupportedException">The printer type cannot return data (SpoolPrinter)</exception>
        public static PrinterStatus HostStatus(IZebraPrinter printer, int timeoutMilliseconds = 5000)
        {
            if (printer == null) throw new ArgumentNullException(nameof(printer));
            var response = printer.Query(HostStatus(), timeoutMilliseconds);
            return response == null ? null : ParseHostStatus(response);
        }

        /// <summary>
        /// Parses the printer's reply to a ~HS host status command. The reply is three STX/ETX delimited strings:
        /// <code>
        /// aaa,b,c,dddd,eee,f,g,h,iii,j,k,l
        /// mmm,n,o,p,q,r,s,t,uuuuuuuu,v,www
        /// xxxx,y
        /// </code>
        /// </summary>
        /// <param name="response">The raw bytes returned by the printer</param>
        /// <returns>The parsed status</returns>
        /// <exception cref="ArgumentException">The response does not contain at least the first two status strings</exception>
        public static PrinterStatus ParseHostStatus(byte[] response)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));
            var blocks = SplitHostStatusBlocks(Encoding.ASCII.GetString(response));
            if (blocks.Count < 2)
                throw new ArgumentException("Response is not a ~HS host status reply: expected three comma separated strings.", nameof(response));

            var s1 = blocks[0].Split(',');
            var s2 = blocks[1].Split(',');
            if (s1.Length < 12 || s2.Length < 11)
                throw new ArgumentException("Response is not a ~HS host status reply: status strings have too few fields.", nameof(response));

            var status = new PrinterStatus
            {
                CommunicationSettings = s1[0].Trim(),
                PaperOut = Flag(s1[1]),
                Paused = Flag(s1[2]),
                LabelLength = Number(s1[3]),
                FormatsInBuffer = Number(s1[4]),
                BufferFull = Flag(s1[5]),
                DiagnosticMode = Flag(s1[6]),
                PartialFormat = Flag(s1[7]),
                CorruptRam = Flag(s1[9]),
                UnderTemperature = Flag(s1[10]),
                OverTemperature = Flag(s1[11]),

                FunctionSettings = s2[0].Trim(),
                HeadUp = Flag(s2[2]),
                RibbonOut = Flag(s2[3]),
                ThermalTransferMode = Flag(s2[4]),
                PrintMode = ParsePrintMode(s2[5]),
                PrintWidthMode = Number(s2[6]),
                LabelWaiting = Flag(s2[7]),
                LabelsRemainingInBatch = Number(s2[8]),
                GraphicsStored = Number(s2[10])
            };

            if (blocks.Count >= 3)
            {
                var s3 = blocks[2].Split(',');
                status.Password = s3[0].Trim();
                if (s3.Length > 1) status.StaticRamInstalled = Flag(s3[1]);
            }
            return status;
        }

        private static List<string> SplitHostStatusBlocks(string text)
        {
            var blocks = new List<string>();
            foreach (Match m in HostStatusBlock.Matches(text))
                blocks.Add(m.Groups[1].Value.Trim());
            if (blocks.Count > 0) return blocks;

            // No STX/ETX framing (some transports strip control characters): fall back to one string per line
            foreach (var line in text.Split('\n'))
            {
                var trimmed = line.Trim('\r', ' ', '\t');
                if (trimmed.Length > 0) blocks.Add(trimmed);
            }
            return blocks;
        }

        private static bool Flag(string field)
        {
            return field.Trim() == "1";
        }

        private static int Number(string field)
        {
            int value;
            return int.TryParse(field.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : 0;
        }

        private static PrintMode ParsePrintMode(string field)
        {
            switch (field.Trim().ToUpperInvariant())
            {
                case "0": return PrintMode.Rewind;
                case "1": return PrintMode.PeelOff;
                case "2": return PrintMode.TearOff;
                case "3": return PrintMode.Cutter;
                case "4": return PrintMode.Applicator;
                case "5": return PrintMode.DelayedCut;
                case "6": return PrintMode.LinerlessPeel;
                case "7": return PrintMode.LinerlessRewind;
                case "8": return PrintMode.PartialCutter;
                case "9": return PrintMode.Rfid;
                case "K": return PrintMode.Kiosk;
                case "S": return PrintMode.Stream;
                default: return PrintMode.Unknown;
            }
        }
    }
}
