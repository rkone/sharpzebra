using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SharpZebra.Commands;

public partial class EPLCommands
{
    //return the width of each character in dots 
    public static int[] CustomFontCharacterWidth(SKFont font)
    {
        var width = new int[256];
        using var paint = new SKPaint();
        paint.Typeface = font.Typeface;
        paint.TextSize = font.Size * 203 / 72; // points are 72dpi, EPL printers are 203dpi

        for (var i = 0; i < 256; i++)
        {
            var skBounds = SKRect.Empty;
            paint.MeasureText(((char)i).ToString(), ref skBounds);
            width[i] = (int)Math.Ceiling(skBounds.Width);
        }
        width[32] = (int)(paint.TextSize / 4); //set space width to be 1/4 of font size
        return width;
    }

    public static byte[] CustomFontStore(SKFont font, ZebraFont name, ElementUploadRotation rotation, FontCharsetType charset)
    {
        if (name < ZebraFont.CUSTOM_A)
        {
            throw new ArgumentOutOfRangeException(nameof(name));
        }
        var fontSize = (int)font.Size * 203 / 72; // points are 72dpi, EPL printers are 203dpi

        var sendBytes = new List<byte>();
        byte[] init = [10,
            (byte)'N',
            10,
            (byte)'E',
            (byte)'K',
            (byte)'"',
            (byte)name,
            (byte)'"',
            10,
            (byte)'E',
            (byte)'S',
            (byte)'"',
            (byte)name,
            (byte)'"'];
        //EK"a"
        //ES"a"
        sendBytes.AddRange(init);

        var characterCount = FontCharset.CharList[(int)charset].Length;
        sendBytes.Add((byte)characterCount); //P1 - Number of characters we're sending
        sendBytes.Add((byte)(int)rotation);  //P2 - 0 - portrait, 1 - Landscape, 2 - both
        sendBytes.Add((byte)fontSize);    //P3 - Font height

        //calculate character spacing - 5% of font height or 1 dot, whichever is more
        var spacing = Math.Max(1, (int)(fontSize * 0.05));

        var chr = new SKBitmap[characterCount];
        var fontStart = new int[characterCount];
        var width = new int[characterCount];
        int byteWidth;

        for (var i = 0; i < characterCount; i++)
        {
            //Use OEM encoding.  
            int currentChr = System.Text.Encoding.GetEncoding(437).GetBytes(FontCharset.CharList[(int)charset])[i];
            using var paint = new SKPaint(font);
            paint.TextSize = fontSize;
            paint.Color = SKColors.Black;

            var skBounds = SKRect.Empty;
            paint.MeasureText(FontCharset.CharList[(int)charset][i].ToString(), ref skBounds);
            chr[i] = new SKBitmap((int)Math.Ceiling(skBounds.Width + spacing), (int)Math.Ceiling(skBounds.Height), SKColorType.Gray8, SKAlphaType.Opaque);
            using var chrCanvas = new SKCanvas(chr[i]);
            chrCanvas.Clear();
            chrCanvas.DrawText(FontCharset.CharList[(int)charset][i].ToString(), 0, -skBounds.Top, paint);

            fontStart[i] = chr[i].Width;
            var fontEnd = 0;
            for (var h = 0; h < chr[i].Height; h++)
            {
                for (var w = 0; w < chr[i].Width; w++)
                {
                    var pixel = chr[i].GetPixel(w, h).Red == 255;
                    if (pixel && w > fontEnd)
                        fontEnd = w;
                    if (pixel && w < fontStart[i])
                        fontStart[i] = w;
                }
            }
            if (fontEnd < fontStart[i])
            {
                width[i] = fontSize / 4;     //set empty character width to be 1/4 height
                fontStart[i] = 0;
            }
            else
                width[i] = fontEnd - fontStart[i] + 1 + spacing;

            if (rotation != ElementUploadRotation.NO_ROTATION &&
                rotation != ElementUploadRotation.BOTH_ROTATIONS) continue;

            //pack image into bits
            byteWidth = width[i] % 8 > 0 ? width[i] / 8 + 1 : width[i] / 8;
            sendBytes.Add((byte)currentChr); //An - Character
            sendBytes.Add((byte)skBounds.Width);   //Bn - number of dots in character width (EPL manual is wrong)
            sendBytes.Add((byte)byteWidth);  //Cn - number of bytes after padding (EPL manual is wrong)

            for (var h = 0; h < chr[i].Height; h++)
            {
                var ba = new BitArray(8);
                var k = 0;

                for (var w = fontStart[i]; w < byteWidth * 8 + fontStart[i]; w++)
                {
                    if (w < chr[i].Width)
                        ba[7 - k] = chr[i].GetPixel(w, h).Red == 255;
                    else
                        ba[7 - k] = false;
                    k++;
                    if (k != 8) continue;
                    sendBytes.Add(ConvertToByte(ba));
                    k = 0;
                }
            }

        }

        if (rotation != ElementUploadRotation.ROTATE_90_DEGREES && rotation != ElementUploadRotation.BOTH_ROTATIONS)
            return [.. sendBytes];

        byteWidth = fontSize % 8 > 0 ? fontSize / 8 + 1 : fontSize / 8;
        for (var i = 0; i < characterCount; i++)
        {
            sendBytes.Add((byte)FontCharset.CharList[(int)charset][i]); //An
            sendBytes.Add((byte)(width[i] + spacing));                  //Bn
            sendBytes.Add((byte)(width[i] + spacing));                  //Cn

            for (var w = fontStart[i]; w < width[i] + fontStart[i] + spacing; w++)
            {
                var ba = new BitArray(8);
                var k = 0;

                for (var h = byteWidth * 8 - 1; h >= 0; h--)
                {
                    if (h < chr[i].Height && w < chr[i].Width)
                        ba[7 - k] = chr[i].GetPixel(w, h).Red == 255;
                    else
                        ba[7 - k] = false;
                    k++;
                    if (k != 8) continue;
                    sendBytes.Add(ConvertToByte(ba));
                    k = 0;
                }
            }
        }

        return sendBytes.ToArray();
    }
}
