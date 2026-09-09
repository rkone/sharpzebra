using System.Collections.Generic;
using System.IO;

namespace SharpZebra.Printing;

/// <summary>
/// A file stored on one of a Zebra printer's drives, as reported by the printer's directory listing.
/// </summary>
public class PrinterFile
{
    /// <summary>The drive the file is stored on (R, E, B or A)</summary>
    public char Drive { get; set; }

    /// <summary>The file name including its extension, e.g. "LOGO.GRF" or "MYFONT.TTF"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Size of the file in bytes</summary>
    public long Size { get; set; }

    /// <summary>The file name without its extension, e.g. "LOGO"</summary>
    public string BaseName => Path.GetFileNameWithoutExtension(Name);

    /// <summary>The upper-case extension without the leading dot, e.g. "GRF", "TTF", "FNT"</summary>
    public string Extension => Path.GetExtension(Name).TrimStart('.').ToUpperInvariant();

    public override string ToString() => $"{Drive}:{Name} ({Size} bytes)";
}

/// <summary>
/// The result of listing a Zebra printer drive.
/// </summary>
public class PrinterDirectory
{
    /// <summary>The drive that was listed</summary>
    public char Drive { get; set; }

    /// <summary>The file pattern the listing was filtered by, e.g. "*.*" or "*.TTF"</summary>
    public string Pattern { get; set; } = "*.*";

    /// <summary>The files found, in the order the printer reported them</summary>
    public List<PrinterFile> Files { get; } = new();

    /// <summary>Free space remaining on the drive in bytes, if the printer reported it</summary>
    public long? BytesFree { get; set; }

    /// <summary>The printer's description of the drive, e.g. "ONBOARD FLASH" or "RAM", if reported</summary>
    public string? DriveDescription { get; set; }
}
