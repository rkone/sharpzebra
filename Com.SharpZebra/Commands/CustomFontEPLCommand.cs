using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace SharpZebra.Commands
{
    public partial class EPLCommands
    {
        public static byte[] CustomFontDelete(ZebraFont name)
        {
            if (name != ZebraFont.CUSTOM_ALL && name < ZebraFont.CUSTOM_A)
            {
                throw new ArgumentException("Unable to delete standard fonts");
            }
            return System.Text.Encoding.GetEncoding(437).GetBytes("EK\"" + (char)name + "\"\n");
        }

        public static string CustomFontCut(string text, int maxWidth, int[] charWidths)
        {
            var i = 0;
            var cutLen = -1;
            var curLen = 0;
            while (i < text.Length)
            {
                curLen += charWidths[text[i]];
                if (curLen > maxWidth)
                {
                    cutLen = i - 1;
                    break;
                }
                i++;
            }
            return cutLen < 0 ? text : text.Substring(0, cutLen);
        }

        public static string[] CustomFontCutToFit(string text, int maxWidth, int[] charWidths)
        {
            var i = 0;
            var lastCut = -1;
            var curLen = 0;
            var result = new List<string>();
            var remainder = text;
            while (i < remainder.Length)
            {
                if (remainder[i] == ' ' || remainder[i] == '-')
                    lastCut = i + 1;
                curLen += charWidths[remainder[i]];
                if (curLen > maxWidth)
                {
                    if (lastCut < 0)
                    {
                        result.Add(remainder.Substring(0, i));
                        remainder = remainder.Substring(i);
                    }
                    else
                    {
                        result.Add(remainder.Substring(0, lastCut));
                        remainder = remainder.Substring(lastCut);
                    }
                    lastCut = -1;
                    curLen = 0;
                    i = 0;
                }
                i++;
            }
            result.Add(remainder);
            return result.ToArray();
        }

        public static int CustomFontTextWidth(string text, int[] charWidths)
        {
            var i = 0;
            foreach (var c in text)
            {
                i += charWidths[c];
            }
            return i;
        }

        public static int CustomFontTextWidth(string text, Font font)
        {
            return CustomFontTextWidth(text, CustomFontCharacterWidth(font));
        }

        //Doesn't do a good job at calculating the width, but better than nothing...
        public static int[] CustomFontCharacterWidth(Font font)
        {
            var chr = new Bitmap[256];
            var width = new int[256];
            var spacing = Math.Max(1, (int)(font.Height * 0.05));

            for (var i = 0; i < 256; i++)
            {
                chr[i] = RenderChar(font, (char)i);

                var fontStart = chr[i].Width;
                var fontEnd = 0;
                for (var h = 0; h < chr[i].Height; h++)
                {
                    for (var w = 0; w < chr[i].Width; w++)
                    {
                        var pixel = chr[i].GetPixel(w, h).A == 255;
                        if (pixel && w > fontEnd)
                            fontEnd = w;
                        if (pixel && w < fontStart)
                            fontStart = w;
                    }
                }
                if (fontEnd < fontStart)
                {
                    width[i] = font.Height / 4;     //set empty character width to be 1/4 height
                }
                else
                    width[i] = (fontEnd - fontStart + 1 + spacing) * 203 / 84;
            }
            return width;
        }

        public static byte[] CustomFontStore(Font font, ZebraFont name, ElementUploadRotation rotation, FontCharsetType charset)
        {
            if (name < ZebraFont.CUSTOM_A)
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }

            var sendBytes = new List<byte>();
            byte[] init = {10, (byte)'N', 10, (byte)'E', (byte)'K', (byte)'"', (byte)name, (byte)'"', 10,
                              (byte)'E', (byte)'S', (byte)'"', (byte)name, (byte)'"'};
            //EK"a"
            //ES"a"
            sendBytes.AddRange(init);

            var characterCount = FontCharset.CharList[(int)charset].Length;
            sendBytes.Add((byte)characterCount); //P1 - Number of characters we're sending
            sendBytes.Add((byte)(int)rotation);  //P2 - 0 - portrait, 1 - Landscape, 2 - both
            sendBytes.Add((byte)font.Height);    //P3 - Font height

            //calculate character spacing - 5% of font height or 1 dot, whichever is more
            var spacing = Math.Max(1, (int)(font.Height * 0.05));

            var chr = new Bitmap[characterCount];
            var fontStart = new int[characterCount];
            var width = new int[characterCount];
            int byteWidth;

            for (var i = 0; i < characterCount; i++)
            {
                //Use OEM encoding.  
                int currentChr = System.Text.Encoding.GetEncoding(437).GetBytes(FontCharset.CharList[(int)charset])[i];
                chr[i] = RenderChar(font, FontCharset.CharList[(int)charset][i]);

                fontStart[i] = chr[i].Width;
                var fontEnd = 0;
                for (var h = 0; h < chr[i].Height; h++)
                {
                    for (var w = 0; w < chr[i].Width; w++)
                    {
                        var pixel = chr[i].GetPixel(w, h).A == 255;
                        if (pixel && w > fontEnd)
                            fontEnd = w;
                        if (pixel && w < fontStart[i])
                            fontStart[i] = w;
                    }
                }
                if (fontEnd < fontStart[i])
                {
                    width[i] = font.Height / 4;     //set empty character width to be 1/4 height
                    fontStart[i] = 0;
                }
                else
                    width[i] = fontEnd - fontStart[i] + 1 + spacing;

                if (rotation != ElementUploadRotation.NO_ROTATION &&
                    rotation != ElementUploadRotation.BOTH_ROTATIONS) continue;

                byteWidth = width[i] % 8 > 0 ? width[i] / 8 + 1 : width[i] / 8;
                sendBytes.Add((byte)currentChr); //An - Character
                sendBytes.Add((byte)width[i]);   //Bn - number of dots in character width (EPL manual is wrong)
                sendBytes.Add((byte)byteWidth);  //Cn - number of bytes after padding (EPL manual is wrong)

                for (var h = 0; h < chr[i].Height; h++)
                {
                    var ba = new BitArray(8);
                    var k = 0;

                    for (var w = fontStart[i]; w < byteWidth * 8 + fontStart[i]; w++)
                    {
                        if (w < chr[i].Width)
                            ba[7 - k] = chr[i].GetPixel(w, h).A == 255;
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
                return sendBytes.ToArray();

            byteWidth = font.Height % 8 > 0 ? font.Height / 8 + 1 : font.Height / 8;
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
                            ba[7 - k] = chr[i].GetPixel(w, h).A == 255;
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

        private static byte ConvertToByte(BitArray bits)
        {
            byte value = 0x00;

            for (byte x = 0; x < 8; x++)
            {
                value |= (byte)(bits[x] ? 0x01 << x : 0x00);
            }
            return value;
        }

        private static Bitmap RenderChar(Font font, char letter)
        {
            SizeF temps;
            using (var bitmap = new Bitmap(font.Height * 2, font.Height))
            {
                temps = Graphics.FromImage(bitmap).MeasureString(letter.ToString(), font);
            }
            var fontScale = font.Height / temps.Height;
            var bm = new Bitmap((int)(temps.Width * fontScale), font.Height);

            using (var g = Graphics.FromImage(bm))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                var stringFormat = new StringFormat()
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Near
                };
                var rect = new Rectangle(0, 0, bm.Width, bm.Height);
                // measure how large the text is on the Graphics object with the current font size
                using (var fontForDrawing = new Font(font.FontFamily, font.SizeInPoints * fontScale, font.Style, GraphicsUnit.Point))
                {
                    g.DrawString(letter.ToString(), fontForDrawing, Brushes.Black, rect, stringFormat);
                }
            }
            return bm;
        }
    }
}
