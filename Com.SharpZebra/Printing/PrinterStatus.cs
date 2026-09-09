using System.Collections.Generic;

namespace SharpZebra.Printing
{
    /// <summary>
    /// The printer's media handling mode, as reported by the ~HS host status command.
    /// </summary>
    public enum PrintMode
    {
        Unknown = -1,
        Rewind = 0,
        PeelOff = 1,
        TearOff = 2,
        Cutter = 3,
        Applicator = 4,
        DelayedCut = 5,
        LinerlessPeel = 6,
        LinerlessRewind = 7,
        PartialCutter = 8,
        Rfid = 9,
        Kiosk = 10,
        Stream = 11
    }

    /// <summary>
    /// The printer's current state, as reported by the ZPL ~HS host status command.
    /// Flags are true when the condition is present.
    /// </summary>
    public class PrinterStatus
    {
        // ---- string 1 ----

        /// <summary>The raw 3-digit communication settings code (baud, handshake, parity, data and stop bits)</summary>
        public string CommunicationSettings { get; set; } = string.Empty;

        /// <summary>The printer is out of media</summary>
        public bool PaperOut { get; set; }

        /// <summary>The printer is paused</summary>
        public bool Paused { get; set; }

        /// <summary>Label length in dots</summary>
        public int LabelLength { get; set; }

        /// <summary>Number of formats waiting in the receive buffer</summary>
        public int FormatsInBuffer { get; set; }

        /// <summary>The receive buffer is full</summary>
        public bool BufferFull { get; set; }

        /// <summary>Communications diagnostic mode is active</summary>
        public bool DiagnosticMode { get; set; }

        /// <summary>A partial format is in progress</summary>
        public bool PartialFormat { get; set; }

        /// <summary>Configuration data has been lost (corrupt RAM)</summary>
        public bool CorruptRam { get; set; }

        /// <summary>The print head is below its operating temperature</summary>
        public bool UnderTemperature { get; set; }

        /// <summary>The print head is above its operating temperature</summary>
        public bool OverTemperature { get; set; }

        // ---- string 2 ----

        /// <summary>The raw 3-digit function settings code (media type, sensor, print method, print mode)</summary>
        public string FunctionSettings { get; set; } = string.Empty;

        /// <summary>The print head is open</summary>
        public bool HeadUp { get; set; }

        /// <summary>The printer is out of ribbon</summary>
        public bool RibbonOut { get; set; }

        /// <summary>Thermal transfer mode is selected (false means direct thermal)</summary>
        public bool ThermalTransferMode { get; set; }

        /// <summary>How labels are presented after printing</summary>
        public PrintMode PrintMode { get; set; } = PrintMode.Unknown;

        /// <summary>The raw print width mode code</summary>
        public int PrintWidthMode { get; set; }

        /// <summary>A printed label is waiting to be taken in peel-off mode</summary>
        public bool LabelWaiting { get; set; }

        /// <summary>Labels remaining in the current batch</summary>
        public int LabelsRemainingInBatch { get; set; }

        /// <summary>Number of graphic images stored in the printer's memory</summary>
        public int GraphicsStored { get; set; }

        // ---- string 3 ----

        /// <summary>The printer's 4-digit password</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>Static RAM is installed</summary>
        public bool StaticRamInstalled { get; set; }

        /// <summary>
        /// True when nothing is stopping the printer from printing: media and ribbon present, head closed,
        /// not paused, buffer not full, head temperature in range and configuration intact.
        /// </summary>
        public bool IsReady => !PaperOut && !Paused && !HeadUp && !RibbonOut && !BufferFull && !CorruptRam && !UnderTemperature && !OverTemperature;

        public override string ToString()
        {
            var problems = new List<string>();
            if (PaperOut) problems.Add("paper out");
            if (RibbonOut) problems.Add("ribbon out");
            if (HeadUp) problems.Add("head up");
            if (Paused) problems.Add("paused");
            if (BufferFull) problems.Add("buffer full");
            if (CorruptRam) problems.Add("corrupt RAM");
            if (UnderTemperature) problems.Add("under temperature");
            if (OverTemperature) problems.Add("over temperature");
            return problems.Count == 0 ? "Ready" : string.Join(", ", problems.ToArray());
        }
    }
}
