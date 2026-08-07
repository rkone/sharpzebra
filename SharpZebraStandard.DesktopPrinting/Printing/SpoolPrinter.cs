using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading.Tasks;

namespace SharpZebra.Printing;

/// <summary>
/// Creates a printer that sends raw data to a printer installed in Windows via the print spooler.
/// </summary>
/// <param name="settings">Settings identifying the printer: PrinterName is the name of the installed Windows printer</param>
public partial class SpoolPrinter(PrinterSettings settings) : IZebraPrinter
{
    public PrinterSettings Settings { get; set; } = settings;

    /// <summary>
    /// Sends the given data to the printer as a RAW spooler document.
    /// </summary>
    /// <param name="data">The EPL2/ZPLII bytes to send</param>
    /// <returns>True if the data was accepted by the spooler, false otherwise</returns>
    public bool? Print(byte[] data)
        {
            var h = GCHandle.Alloc(data, GCHandleType.Pinned);
            var res = SendBytesToPrinter(Settings.PrinterName, h.AddrOfPinnedObject(), data.Length);
            h.Free();
            return res;
        }

    /// <summary>
    /// Sends the given data to the printer. The spooler API is synchronous, so this simply wraps Print.
    /// </summary>
    /// <param name="data">The EPL2/ZPLII bytes to send</param>
    /// <returns>True if the data was accepted by the spooler, false otherwise</returns>
    public Task<bool> PrintAsync(byte[] data)
        {
            var res = Print(data);
            return Task.FromResult(res ?? false);
        }

    // Structure and API declarations:
    [StructLayout(LayoutKind.Sequential)]
    private struct DOCINFOA
    {
        public IntPtr pDocName;
        public IntPtr pOutputFile;
        public IntPtr pDataType;
    }

    [LibraryImport("winspool.drv", EntryPoint = "OpenPrinterA", SetLastError = true,
        StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [LibraryImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ClosePrinter(IntPtr hPrinter);

    [LibraryImport("winspool.drv", EntryPoint = "StartDocPrinterA", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool StartDocPrinter(IntPtr hPrinter, int level, in DOCINFOA di);

    [LibraryImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EndDocPrinter(IntPtr hPrinter);

    [LibraryImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool StartPagePrinter(IntPtr hPrinter);

    [LibraryImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EndPagePrinter(IntPtr hPrinter);

    [LibraryImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    // SendBytesToPrinter()
    // When the function is given a printer name and an unmanaged array
    // of bytes, the function sends those bytes to the print queue.
    // Returns true on success, false on failure.
    private static bool SendBytesToPrinter(string szPrinterName, IntPtr pBytes, int dwCount)
    {
        var bSuccess = false; // Assume failure unless you specifically succeed.

        // DOCINFOA carries raw ANSI string pointers, so the strings are allocated manually.
        var docName = Marshal.StringToHGlobalAnsi("Labels");
        var dataType = Marshal.StringToHGlobalAnsi("RAW");
        try
        {
            var di = new DOCINFOA
            {
                pDocName = docName,
                pOutputFile = IntPtr.Zero,
                pDataType = dataType
            };

            if (!OpenPrinter(szPrinterName.Normalize(), out var hPrinter, IntPtr.Zero)) return false;
            if (StartDocPrinter(hPrinter, 1, in di))
            {
                if (StartPagePrinter(hPrinter))
                {
                    bSuccess = WritePrinter(hPrinter, pBytes, dwCount, out _);
                    EndPagePrinter(hPrinter);
                }
                EndDocPrinter(hPrinter);
            }
            ClosePrinter(hPrinter);
        }
        finally
        {
            Marshal.FreeHGlobal(docName);
            Marshal.FreeHGlobal(dataType);
        }
        // If you did not succeed, GetLastError may give more information
        // about why not.
        // var dwError = Marshal.GetLastWin32Error();
        // throw new ApplicationException($"Print failure. Error code {dwError}");
        return bSuccess;

    }

}