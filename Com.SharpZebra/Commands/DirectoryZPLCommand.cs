using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SharpZebra.Printing;

namespace SharpZebra.Commands
{
    public partial class ZPLCommands
    {
        private static readonly Regex DirectoryPatternValidator = new Regex(@"^[A-Za-z0-9_*?]{1,16}(\.[A-Za-z0-9*?]{1,3})?$", RegexOptions.Compiled);
        private static readonly Regex DirectoryHeaderLine = new Regex(@"^-\s+DIR\s+(?<drive>[A-Za-z]):(?<pattern>\S+)", RegexOptions.Compiled);
        private static readonly Regex DirectoryFileLine = new Regex(@"^\*\s+(?<drive>[A-Za-z]):(?<name>\S+)\s+(?<size>\d+)", RegexOptions.Compiled);
        private static readonly Regex DirectoryFooterLine = new Regex(@"^-\s+(?<free>\d+)\s+bytes free\s+(?<drive>[A-Za-z]):\s*(?<description>.*?)\s*$", RegexOptions.Compiled);

        /// <summary>
        /// Asks the printer for a directory listing of one of its drives. The printer answers on the same connection,
        /// so send this with IZebraPrinter.Query and pass the response to ParseDirectoryList, or use the
        /// DirectoryList overload that takes a printer to do both.
        /// ZPL Command: ^HW.
        /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf"/>
        /// </summary>
        /// <param name="storageArea">The drive to list (R, E, B or A)</param>
        /// <param name="pattern">File pattern to filter by, using * and ? wildcards, e.g. "*.*", "*.TTF" or "LOGO?.GRF"</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] DirectoryList(char storageArea, string pattern = "*.*")
        {
            if (!char.IsLetter(storageArea)) throw new ArgumentException("Storage area must be a printer drive letter such as R, E, B or A.", nameof(storageArea));
            if (string.IsNullOrEmpty(pattern) || !DirectoryPatternValidator.IsMatch(pattern))
                throw new ArgumentException("Pattern must be a file name or wildcard pattern such as *.*, *.TTF or LOGO?.GRF.", nameof(pattern));
            return Encoding.GetEncoding(850).GetBytes($"^XA^HW{char.ToUpperInvariant(storageArea)}:{pattern}^XZ");
        }

        /// <summary>
        /// Lists the files on one of the printer's drives. Sends the ^HW directory command to the printer and parses
        /// its reply. Only printers that can return data support this: NetworkPrinter and USBPrinter can, SpoolPrinter cannot.
        /// </summary>
        /// <param name="printer">The printer to query</param>
        /// <param name="storageArea">The drive to list (R, E, B or A)</param>
        /// <param name="pattern">File pattern to filter by, using * and ? wildcards, e.g. "*.*", "*.TTF" or "LOGO?.GRF"</param>
        /// <param name="timeoutMilliseconds">How long to wait for the printer to connect and start replying</param>
        /// <returns>The directory listing, or null if the printer could not be reached or did not reply</returns>
        /// <exception cref="NotSupportedException">The printer type cannot return data (SpoolPrinter)</exception>
        public static PrinterDirectory DirectoryList(IZebraPrinter printer, char storageArea, string pattern = "*.*", int timeoutMilliseconds = 5000)
        {
            if (printer == null) throw new ArgumentNullException(nameof(printer));
            var response = printer.Query(DirectoryList(storageArea, pattern), timeoutMilliseconds);
            return response == null ? null : ParseDirectoryList(response);
        }

        /// <summary>
        /// Parses the printer's reply to a ^HW directory command into a PrinterDirectory. The reply looks like:
        /// <code>
        /// - DIR E:*.TTF
        /// * E:MYFONT.TTF     55908
        /// -  63102976 bytes free E: ONBOARD FLASH
        /// </code>
        /// </summary>
        /// <param name="response">The raw bytes returned by the printer</param>
        /// <returns>The parsed directory listing. Lines that are not recognised are ignored.</returns>
        public static PrinterDirectory ParseDirectoryList(byte[] response)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));
            var text = Encoding.ASCII.GetString(response).Replace("\x02", "").Replace("\x03", "");
            var directory = new PrinterDirectory();

            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.Trim('\r', ' ', '\t');
                if (line.Length == 0) continue;

                var file = DirectoryFileLine.Match(line);
                if (file.Success)
                {
                    directory.Files.Add(new PrinterFile
                    {
                        Drive = char.ToUpperInvariant(file.Groups["drive"].Value[0]),
                        Name = file.Groups["name"].Value,
                        Size = long.Parse(file.Groups["size"].Value, CultureInfo.InvariantCulture)
                    });
                    continue;
                }

                var header = DirectoryHeaderLine.Match(line);
                if (header.Success)
                {
                    directory.Drive = char.ToUpperInvariant(header.Groups["drive"].Value[0]);
                    directory.Pattern = header.Groups["pattern"].Value;
                    continue;
                }

                var footer = DirectoryFooterLine.Match(line);
                if (footer.Success)
                {
                    directory.BytesFree = long.Parse(footer.Groups["free"].Value, CultureInfo.InvariantCulture);
                    if (directory.Drive == '\0') directory.Drive = char.ToUpperInvariant(footer.Groups["drive"].Value[0]);
                    var description = footer.Groups["description"].Value;
                    directory.DriveDescription = description.Length == 0 ? null : description;
                }
            }
            return directory;
        }
    }
}
