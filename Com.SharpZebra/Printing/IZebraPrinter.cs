namespace SharpZebra.Printing
{
    public interface IZebraPrinter
    {
        bool? Print(byte[] data);

        /// <summary>
        /// Sends data to the printer and returns whatever the printer sends back, for host commands such as
        /// ^HW (directory listing) or ~HS (status). Reading stops once the printer has replied and gone quiet.
        /// </summary>
        /// <param name="data">The EPL2/ZPLII bytes to send</param>
        /// <param name="timeoutMilliseconds">How long to wait for the printer to connect and start replying</param>
        /// <returns>The raw response bytes, or null if the printer could not be reached or did not reply in time</returns>
        /// <exception cref="System.NotSupportedException">This printer type cannot return data (SpoolPrinter)</exception>
        byte[] Query(byte[] data, int timeoutMilliseconds = 5000);

        PrinterSettings Settings { get; set; }
    }

    public class PrinterSettings
    {
        public int Id { get; set; }
        public char PrinterType { get; set; }
        public string PrinterName { get; set; }
        public int PrinterPort { get; set; }
        public int AlignLeft { get; set; }
        public int AlignTop { get; set; }
        public int AlignTearOff { get; set; }
        public int Darkness { get; set; }
        public int PrintSpeed { get; set; }
        public string SlewSpeed { get; set; }
        public string BackfeedSpeed { get; set; }
        public int Width { get; set; }
        public int Length { get; set; }
        public char RamDrive { get; set; }

        /// <summary>
        /// Creates printer settings with the defaults: RAM drive 'R', slew and backfeed speed 12.
        /// </summary>
        public PrinterSettings()
        {
            RamDrive = 'R';
            SlewSpeed = "12";
            BackfeedSpeed = "12";
        }
    }
}