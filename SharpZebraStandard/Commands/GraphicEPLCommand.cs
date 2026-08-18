using System.IO;
using System.Text;
using System.Collections;
using SharpZebra.Printing;
using System.Collections.Generic;
using SkiaSharp;

namespace SharpZebra.Commands;

public partial class EPLCommands
{
#pragma warning disable IDE0060
    /// <summary>
    /// Prints a graphic previously stored on the printer with GraphicStore.
    /// EPL Command: GG.
    /// </summary>
    /// <param name="left">Distance in dots from the left of the label</param>
    /// <param name="top">Distance in dots to the top of the label</param>
    /// <param name="imageName">Name the graphic was stored under</param>
    /// <param name="settings">Unused; kept for signature compatibility</param>
    /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
    public static byte[] GraphicWrite(int left, int top, string imageName, PrinterSettings settings)
    {
        return Encoding.GetEncoding(437).GetBytes($"GG{left},{top},\"{imageName}\"\n");
    }
#pragma warning restore IDE0060
    /// <summary>
    /// Uploads a PCX image to the printer's memory, replacing any graphic already stored under the same name.
    /// The image must be monochrome PCX format.
    /// EPL Commands: GK, GM.
    /// </summary>
    /// <param name="fileStream">Stream containing the PCX file contents</param>
    /// <param name="imageName">Name to store the graphic under</param>
    /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
    public static byte[] GraphicStore(Stream fileStream, string imageName)
    {
        BinaryReader binaryReader = new(fileStream);
        byte[] fileContents = binaryReader.ReadBytes((int)fileStream.Length);
        binaryReader.Close();
        List<byte> res = new();
        res.AddRange(Encoding.GetEncoding(437).GetBytes($"GK\"{imageName}\"\nGM\"{imageName}\"{fileContents.Length}\n"));
        res.AddRange(fileContents);
        return res.ToArray();
    }

    /// <summary>
    /// Uploads a PCX image file to the printer's memory, replacing any graphic already stored under the same name.
    /// The image must be monochrome PCX format.
    /// EPL Commands: GK, GM.
    /// </summary>
    /// <param name="pcxFilename">Path to the PCX file</param>
    /// <param name="imageName">Name to store the graphic under</param>
    /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
    public static byte[] GraphicStore(string pcxFilename, string imageName)
    {
        FileStream stream = new(pcxFilename, FileMode.Open);
        byte[] res = GraphicStore(stream, imageName);
        stream.Close();
        return res;
    }

    /// <summary>
    /// Deletes a graphic stored on the printer.
    /// EPL Command: GK.
    /// </summary>
    /// <param name="imageName">Name the graphic was stored under</param>
    /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
    public static byte[] GraphicDelete(string imageName)
    {
        return Encoding.GetEncoding(437).GetBytes($"GK\"{imageName}\"\n");
    }

    /// <summary>
    /// Draws an image file directly onto the label without storing it on the printer.
    /// The image is converted to 1-bit using its red channel (red above 128 = white); no dithering is done.
    /// EPL Command: GW.
    /// </summary>
    /// <param name="left">Distance in dots from the left of the label</param>
    /// <param name="top">Distance in dots to the top of the label</param>
    /// <param name="bitmapName">Path to the image file (any format SkiaSharp can decode)</param>
    /// <param name="settings">Printer settings, used to apply the configured left/top alignment offsets</param>
    /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
    public static byte[] GraphicDirectWrite(int left, int top, string bitmapName, PrinterSettings settings)
    {
        SKBitmap bmp = SKBitmap.Decode(bitmapName);
        List<byte> res = [];
        int byteWidth = bmp.Width % 8 == 0 ? bmp.Width / 8 : bmp.Width / 8 + 1;
        res.AddRange(Encoding.GetEncoding(437).GetBytes($"GW{left + settings.AlignLeft},{top + settings.AlignTop},{byteWidth},{bmp.Height},"));
        for (int y = 0; y < bmp.Height; y++)
        {
            for (int x = 0; x < byteWidth; x++)
            {
                BitArray ba = new(8);
                int scanx = x * 8;
                for (int k = 7; k >= 0; k--)
                {
                    if (scanx >= bmp.Width)
                        ba[k] = true;
                    else
                        ba[k] = bmp.GetPixel(scanx, y).Red > 128;
                    scanx++;
                }
                res.Add(ConvertToByte(ba));
            }
        }
        return res.ToArray();
    }

}
