using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpZebra.Commands;

public partial class ZPLCommands
{
    private static readonly Regex FontNameValidator = new("^[A-Za-z0-9]{1,16}$", RegexOptions.Compiled);

    /// <summary>
    /// Uploads a TrueType (.TTF) font file to the printer's memory. The file is validated as a TrueType font
    /// before it is encoded. Once stored, print with it using TextWrite and a fontName of "{fontName}.TTF".
    /// ZPL Command: ~DY.
    /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf"/>
    /// </summary>
    /// <param name="fontPath">Path to the .TTF file to upload</param>
    /// <param name="storageArea">The drive to store the font on (R, E, B or A)</param>
    /// <param name="fontName">Name to store the font under: 1 to 16 letters or digits (8 on older firmware), with or without a .TTF extension</param>
    /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
    /// <exception cref="ArgumentException">The file is not a TrueType font, or the name or drive is not valid</exception>
    /// <exception cref="FileNotFoundException">The font file does not exist</exception>
    public static byte[] FontStore(string fontPath, char storageArea, string fontName)
    {
        if (string.IsNullOrEmpty(fontPath)) throw new ArgumentException("Font file path must be specified.", nameof(fontPath));
        if (!File.Exists(fontPath)) throw new FileNotFoundException("Font file not found.", fontPath);
        return FontStore(File.ReadAllBytes(fontPath), storageArea, fontName);
    }

    /// <summary>
    /// Uploads the contents of a TrueType (.TTF) font file to the printer's memory. The data is validated as a
    /// TrueType font before it is encoded. Once stored, print with it using TextWrite and a fontName of "{fontName}.TTF".
    /// ZPL Command: ~DY.
    /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf"/>
    /// </summary>
    /// <param name="fontData">The raw bytes of the .TTF file</param>
    /// <param name="storageArea">The drive to store the font on (R, E, B or A)</param>
    /// <param name="fontName">Name to store the font under: 1 to 16 letters or digits (8 on older firmware), with or without a .TTF extension</param>
    /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
    /// <exception cref="ArgumentException">The data is not a TrueType font, or the name or drive is not valid</exception>
    public static byte[] FontStore(byte[] fontData, char storageArea, string fontName)
    {
        if (fontData is null) throw new ArgumentNullException(nameof(fontData));
        if (!char.IsLetter(storageArea)) throw new ArgumentException("Storage area must be a printer drive letter such as R, E, B or A.", nameof(storageArea));
        var name = NormalizeFontName(fontName);
        ValidateTrueTypeFont(fontData);

        // ~DYd:f,b,x,t,w,data - b=B: binary data, x=T: TrueType font, t: byte count, w: unused for fonts
        var res = new List<byte>(fontData.Length + 64);
        res.AddRange(Encoding.GetEncoding(850).GetBytes($"~DY{char.ToUpperInvariant(storageArea)}:{name},B,T,{fontData.Length},0,"));
        res.AddRange(fontData);
        return res.ToArray();
    }

    /// <summary>
    /// Deletes a font previously stored on the printer with FontStore.
    /// ZPL Command: ^ID.
    /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf"/>
    /// </summary>
    /// <param name="storageArea">The drive the font is stored on</param>
    /// <param name="fontName">Name the font was stored under, with or without a .TTF extension</param>
    /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
    public static byte[] FontDelete(char storageArea, string fontName)
    {
        return Encoding.GetEncoding(850).GetBytes($"^ID{char.ToUpperInvariant(storageArea)}:{NormalizeFontName(fontName)}.TTF^FS");
    }

    private static string NormalizeFontName(string fontName)
    {
        if (string.IsNullOrEmpty(fontName)) throw new ArgumentException("Font name must be specified.", nameof(fontName));
        var name = fontName.Trim();
        if (name.EndsWith(".TTF", StringComparison.OrdinalIgnoreCase)) name = name.Substring(0, name.Length - 4);
        if (!FontNameValidator.IsMatch(name))
            throw new ArgumentException("Font name must be 1 to 16 letters or digits, optionally followed by .TTF.", nameof(fontName));
        return name;
    }

    private static void ValidateTrueTypeFont(byte[] data)
    {
        const int headerLength = 12;
        const int tableRecordLength = 16;
        if (data.Length < headerLength) throw new ArgumentException("File is too small to be a TrueType font.", nameof(data));

        var version = ReadUInt32BigEndian(data, 0);
        switch (version)
        {
            case 0x00010000: // TrueType outlines
            case 0x74727565: // 'true' - Apple TrueType outlines
                break;
            case 0x4F54544F: // 'OTTO'
                throw new ArgumentException("File is an OpenType font with CFF outlines (.OTF), not a TrueType (.TTF) font.", nameof(data));
            case 0x74746366: // 'ttcf'
                throw new ArgumentException("File is a TrueType collection (.TTC); only a single .TTF font can be uploaded.", nameof(data));
            default:
                throw new ArgumentException("File is not a TrueType (.TTF) font.", nameof(data));
        }

        int numTables = ReadUInt16BigEndian(data, 4);
        if (numTables == 0 || headerLength + numTables * tableRecordLength > data.Length)
            throw new ArgumentException("TrueType font table directory is invalid.", nameof(data));

        var hasHead = false;
        var hasGlyf = false;
        for (var i = 0; i < numTables; i++)
        {
            var record = headerLength + i * tableRecordLength;
            var tag = Encoding.ASCII.GetString(data, record, 4);
            var offset = ReadUInt32BigEndian(data, record + 8);
            var length = ReadUInt32BigEndian(data, record + 12);
            if (offset > (uint)data.Length || length > (uint)data.Length - offset)
                throw new ArgumentException($"TrueType font table '{tag}' extends past the end of the file.", nameof(data));
            if (tag == "head") hasHead = true;
            if (tag == "glyf") hasGlyf = true;
        }
        if (!hasHead || !hasGlyf)
            throw new ArgumentException("TrueType font is missing its 'head' or 'glyf' table.", nameof(data));
    }

    private static uint ReadUInt32BigEndian(byte[] data, int offset)
    {
        return ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];
    }

    private static ushort ReadUInt16BigEndian(byte[] data, int offset)
    {
        return (ushort)((data[offset] << 8) | data[offset + 1]);
    }
}
