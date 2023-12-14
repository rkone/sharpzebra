#region License
/* ---------------------------------------------------------------------------
 * Creative Commons License
 * http://creativecommons.org/licenses/by/2.5/au/
 *
 * Attribution 2.5 Australia
 *
 * You are free:
 *
 * - to copy, distribute, display, and perform the work 
 * - to make derivative works 
 * - to make commercial use of the work 
 *
 * Under the following conditions:
 *
 * Attribution: You must attribute the work in the manner specified by the
 *              author or licensor. 
 *
 * For any reuse or distribution, you must make clear to others the license
 * terms of this work.  Any of these conditions can be waived if you get
 * permission from the copyright holder.  Your fair use and other rights
 * are in no way affected by the above.
 *
 * This is a human-readable summary of the Legal Code (the full license). 
 * http://creativecommons.org/licenses/by/2.5/au/legalcode
 * ------------------------------------------------------------------------ */
#endregion License

using System;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Threading;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace SharpZebra.Printing
{
    public class USBPrinter : IZebraPrinter
    {
        public PrinterSettings Settings { get; set; }

        public USBPrinter(PrinterSettings settings)
        {
            Settings = settings;
        }

        public bool? Print(byte[] data)
        {
            var connector = new UsbPrinterConnector(Settings.PrinterName);
            if (connector.BeginSend())
            {
                return connector.Send(data, 0, data.Length) == data.Length;
            }
            return false;
        }
    }

    public class UsbPrinterConnector
    {
        private const int DefaultReadTimeout = 200;
        private const int DefaultWriteTimeout = 200;
        public int ReadTimeout { get; set; } = DefaultReadTimeout;
        public int WriteTimeout { get; set; } = DefaultWriteTimeout;

        #region EnumDevices

        private static readonly Guid GuidDeviceInterfaceUsbPrint = new Guid(
                0x28d78fad, 0x5a12, 0x11D1,
                0xae, 0x5b, 0x00, 0x00, 0xf8, 0x03, 0xa8, 0xc2);

        private static Dictionary<string, string> EnumDevices()
        {
            RegistryKey subKey;
            int portNumber;
            var printers = new Dictionary<string, string>();
            var printerNames = new Dictionary<int, string>();
            var nameKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Print\\Printers");
            if (nameKey == null) throw new ApplicationException("Windows Compatibility issue: Missing Printer registry keys");
            foreach (var printer in nameKey.GetSubKeyNames())
            {
                subKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Print\\Printers\\" + printer);
                if (subKey == null) continue;
                var portValue = subKey.GetValue("Port", string.Empty).ToString();
                if (portValue.Length >= 3 && portValue.Substring(0, 3) == "USB")
                {
                    if (int.TryParse(portValue.Substring(3, portValue.Length - 3), out portNumber))
                    {
                        if (!printerNames.ContainsKey(portNumber))
                            printerNames.Add(portNumber, printer);
                    }
                }
                subKey.Close();
            }
            nameKey.Close();

            var regKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\DeviceClasses\\" +
                                                          GuidDeviceInterfaceUsbPrint.ToString("B"));
            if (regKey == null) return printers;
            foreach (var sub in regKey.GetSubKeyNames())
            {
                if (sub.Substring(0, 16).ToUpper() != "##?#USB#VID_0A5F") continue;
                //build NT object manager name for the device. Issues? Check with sysinternals WinObj program.
                var path = sub.Replace("##?#", @"\\?\GLOBALROOT\GLOBAL??\");

                subKey = Registry.LocalMachine.OpenSubKey(
                    $"SYSTEM\\CurrentControlSet\\Control\\DeviceClasses\\{GuidDeviceInterfaceUsbPrint:B}\\{sub}\\#\\Device Parameters");
                if (subKey == null) continue;
                if (int.TryParse(subKey.GetValue("Port Number").ToString(), out portNumber))
                {
                    if (printerNames.ContainsKey(portNumber))
                        printers.Add(printerNames[portNumber], path);
                }

                subKey.Close();
            }
            regKey.Close();

            return printers;
        }

        #endregion EnumDevices

        private readonly string _interfaceName;

        private IntPtr _usbHandle = IntPtr.Zero;

        private const uint ReadBufferSize = 512;

        private byte[] _readBuffer;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="printerName">The name of the printer to print to</param>
        public UsbPrinterConnector(string printerName)
        {

            var plist = EnumDevices();
            if (plist.ContainsKey(printerName))
                _interfaceName = plist[printerName];
            else
                throw new Exception($"No printer named {printerName} was found connected via USB.");
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~UsbPrinterConnector()
        {
            SetConnected(false);
        }

        private void SetConnected(bool value)
        {
            if (value)
            {
                if ((int)_usbHandle > 0)
                    SetConnected(false);

                /* C++ Decl.
                usbHandle = CreateFile(
                    interfaceName, 
                    GENERIC_WRITE, 
                    FILE_SHARE_READ,
                    NULL, 
                    OPEN_ALWAYS, 
                    FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, 
                    NULL);
                */

                _usbHandle = FileIO.CreateFile(
                    _interfaceName,
                    FileIO.FileAccess.GENERIC_WRITE | FileIO.FileAccess.GENERIC_READ,
                    FileIO.FileShareMode.FILE_SHARE_READ,
                    IntPtr.Zero,
                    FileIO.FileCreationDisposition.OPEN_ALWAYS,
                    FileIO.FileAttributes.FILE_ATTRIBUTE_NORMAL |
                        FileIO.FileAttributes.FILE_FLAG_SEQUENTIAL_SCAN |
                        FileIO.FileAttributes.FILE_FLAG_OVERLAPPED,
                    IntPtr.Zero);
                if ((int)_usbHandle <= 0)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            else
                if ((int)_usbHandle > 0)
            {
                FileIO.CloseHandle(_usbHandle);
                _usbHandle = IntPtr.Zero;
            }
        }

        private bool GetConnected()
        {
            try
            {
                SetConnected(true);
                return ((int)_usbHandle > 0);
            }
            catch (Win32Exception)  //printer is not online
            {
                return false;
            }
        }

        public bool BeginSend()
        {
            return GetConnected();
        }

        public int Send(byte[] buffer, int offset, int count)
        {
            // USB 1.1 WriteFile maximum block size is 4096

            if (!GetConnected())
                throw new ApplicationException("Not connected");

            if (count > 4096)
            {
                var current = 0;
                var total = 0;
                while (current < count)
                {
                    var newCount = current + 4096 > count ? count - current : 4096;
                    total += Send(buffer, current, newCount);
                    current += 4096;
                }
                return total;
            }

            var bytes = new byte[count];
            Array.Copy(buffer, offset, bytes, 0, count);
            var wo = new ManualResetEvent(false);
            var ov = new NativeOverlapped { EventHandle = wo.SafeWaitHandle.DangerousGetHandle() };
            // ov.OffsetLow = 0; ov.OffsetHigh = 0;
            if (!FileIO.WriteFile(_usbHandle, bytes, (uint)count, out var size, ref ov))
            {
                if (Marshal.GetLastWin32Error() == FileIO.ERROR_IO_PENDING)
                    wo.WaitOne(WriteTimeout, false);
                else
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            FileIO.GetOverlappedResult(_usbHandle, ref ov, out size, true);
            return (int)size;
        }

        public int Read(out byte[] buffer)
        {
            // USB 1.1 ReadFile in block chunks of 64 bytes
            // USB 2.0 ReadFile in block chunks of 512 bytes

            if (_readBuffer == null)
                _readBuffer = new byte[ReadBufferSize];

            var sg = new AutoResetEvent(false);
            var ov = new NativeOverlapped
            {
                OffsetLow = 0,
                OffsetHigh = 0,
                EventHandle = sg.SafeWaitHandle.DangerousGetHandle()
            };

            if (!FileIO.ReadFile(_usbHandle, _readBuffer, ReadBufferSize, out var read, ref ov))
            {
                if (Marshal.GetLastWin32Error() == FileIO.ERROR_IO_PENDING)
                    sg.WaitOne(ReadTimeout, false);
                else
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            FileIO.GetOverlappedResult(_usbHandle, ref ov, out read, true);
            buffer = new byte[read];
            Array.Copy(_readBuffer, buffer, read);
            return (int)read;
        }
    }


    internal class FileIO
    {

        internal const int INVALID_HANDLE_VALUE = -1;

        internal const int ERROR_FILE_NOT_FOUND = 2;
        internal const int ERROR_INVALID_NAME = 123;
        internal const int ERROR_ACCESS_DENIED = 5;
        internal const int ERROR_IO_PENDING = 997;
        internal const int ERROR_IO_INCOMPLETE = 996;

        internal class NullClass
        {
            public NullClass()
            {
                throw new Exception("Cannot create instance of NullClass");
            }
        }

        #region CreateFile

        [Flags]
        internal enum FileAccess : uint  // from winnt.h
        {
            GENERIC_READ = 0x80000000,
            GENERIC_WRITE = 0x40000000,
            GENERIC_EXECUTE = 0x20000000,
            GENERIC_ALL = 0x10000000
        }

        [Flags]
        internal enum FileShareMode : uint  // from winnt.h
        {
            FILE_SHARE_READ = 0x00000001,
            FILE_SHARE_WRITE = 0x00000002,
            FILE_SHARE_DELETE = 0x00000004
        }

        internal enum FileCreationDisposition : uint  // from winbase.h
        {
            CREATE_NEW = 1,
            CREATE_ALWAYS = 2,
            OPEN_EXISTING = 3,
            OPEN_ALWAYS = 4,
            TRUNCATE_EXISTING = 5
        }

        [Flags]
        internal enum FileAttributes : uint  // from winnt.h
        {
            FILE_ATTRIBUTE_READONLY = 0x00000001,
            FILE_ATTRIBUTE_HIDDEN = 0x00000002,
            FILE_ATTRIBUTE_SYSTEM = 0x00000004,
            FILE_ATTRIBUTE_DIRECTORY = 0x00000010,
            FILE_ATTRIBUTE_ARCHIVE = 0x00000020,
            FILE_ATTRIBUTE_DEVICE = 0x00000040,
            FILE_ATTRIBUTE_NORMAL = 0x00000080,
            FILE_ATTRIBUTE_TEMPORARY = 0x00000100,
            FILE_ATTRIBUTE_SPARSE_FILE = 0x00000200,
            FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400,
            FILE_ATTRIBUTE_COMPRESSED = 0x00000800,
            FILE_ATTRIBUTE_OFFLINE = 0x00001000,
            FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000,
            FILE_ATTRIBUTE_ENCRYPTED = 0x00004000,

            // from winbase.h
            FILE_FLAG_WRITE_THROUGH = 0x80000000,
            FILE_FLAG_OVERLAPPED = 0x40000000,
            FILE_FLAG_NO_BUFFERING = 0x20000000,
            FILE_FLAG_RANDOM_ACCESS = 0x10000000,
            FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000,
            FILE_FLAG_DELETE_ON_CLOSE = 0x04000000,
            FILE_FLAG_BACKUP_SEMANTICS = 0x02000000,
            FILE_FLAG_POSIX_SEMANTICS = 0x01000000,
            FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000,
            FILE_FLAG_OPEN_NO_RECALL = 0x00100000,
            FILE_FLAG_FIRST_PIPE_INSTANCE = 0x00080000
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr CreateFile(
            string lpFileName,
            FileAccess dwDesiredAccess,
            FileShareMode dwShareMode,
            IntPtr lpSecurityAttributes,
            FileCreationDisposition dwCreationDisposition,
            FileAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        #endregion CreateFile

        #region CloseHandle

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hObject);

        #endregion CloseHandle

        #region GetOverlappedResult

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetOverlappedResult(
            IntPtr hFile,
            /* IntPtr */ ref System.Threading.NativeOverlapped lpOverlapped,
            out uint nNumberOfBytesTransferred,
            bool bWait);

        #endregion GetOverlappedResult

        #region WriteFile

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "WriteFile")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WriteFile0(
            IntPtr hFile,
            [MarshalAs(UnmanagedType.LPArray)]
            byte[] lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            NullClass lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WriteFile(
            IntPtr hFile,
            [MarshalAs(UnmanagedType.LPArray)] byte[] lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            [In] ref System.Threading.NativeOverlapped lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int WriteFileEx(
            IntPtr hFile,
            [MarshalAs(UnmanagedType.LPArray)] byte[] lpBuffer,
            int nNumberOfBytesToWrite,
            [In] ref System.Threading.NativeOverlapped lpOverlapped,
            [MarshalAs(UnmanagedType.FunctionPtr)] IOCompletionCallback callback
        );

        #endregion WriteFile

        #region ReadFile

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReadFile(
            IntPtr hFile,
            [MarshalAs(UnmanagedType.LPArray)] [Out] byte[] lpBuffer,
            uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead,
            [In] ref System.Threading.NativeOverlapped lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "ReadFile")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReadFile0(
            IntPtr hFile,
            [MarshalAs(UnmanagedType.LPArray)] [Out] byte[] lpBuffer,
            uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead,
            NullClass lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int ReadFileEx(
            IntPtr hFile,
            [MarshalAs(UnmanagedType.LPArray)] byte[] lpBuffer,
            int nNumberOfBytesToRead,
            [In] ref System.Threading.NativeOverlapped lpOverlapped,
            [MarshalAs(UnmanagedType.FunctionPtr)] IOCompletionCallback callback);

        #endregion ReadFile

    }
}
