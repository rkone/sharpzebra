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
    public static byte[] GraphicWrite(int left, int top, string imageName, PrinterSettings settings)
    {
        return Encoding.GetEncoding(437).GetBytes($"GG{left},{top},\"{imageName}\"\n");
    }
#pragma warning restore IDE0060
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

    public static byte[] GraphicStore(string pcxFilename, string imageName)
    {
        FileStream stream = new(pcxFilename, FileMode.Open);
        byte[] res = GraphicStore(stream, imageName);
        stream.Close();
        return res;
    }

    public static byte[] GraphicDelete(string imageName)
    {
        return Encoding.GetEncoding(437).GetBytes($"GK\"{imageName}\"\n");
    }

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
