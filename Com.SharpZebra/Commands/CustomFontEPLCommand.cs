using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace Com.SharpZebra.Commands
{
    public partial class EPLCommands
    {
        public static float CustomFontEmSize(Font font)
        {
            return 0;
        }

        public static byte[] CustomFontDelete(ZebraFont name)
        {
            if (name != ZebraFont.CUSTOM_ALL && name < ZebraFont.CUSTOM_A)
            {
                throw new ApplicationException("Unable to delete standard fonts");
            }
            return System.Text.Encoding.GetEncoding(437).GetBytes("EK\"" + (char)name + "\"\n");
        }

        public static string CustomFontCut(string text, int maxWidth, int[] charWidths)
        {
            int i = 0;
            int cutLen = -1;
            int curLen = 0;
            List<string> result = new List<string>();
            string remainder = text;
            while (i < text.Length)
            {
                curLen += charWidths[(int)text[i]];
                if (curLen > maxWidth)
                {
                    cutLen = i - 1;
                    break;
                }
                i++;
            }
            if (cutLen < 0)
                return text;
            else
                return text.Substring(0, cutLen);            
        }

        public static string[] CustomFontCutToFit(string text, int maxWidth, int[] charWidths)
        {
            int i = 0;
            int lastCut = -1;
            int curLen = 0;
            List<string> result = new List<string>();
            string remainder = text;
            while (i < remainder.Length)
            {
                if (remainder[i] == ' ' || remainder[i] == '-')
                    lastCut = i + 1;
                curLen += charWidths[(int)remainder[i]];
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
            int i = 0;
            foreach (char c in text)
            {
                i += charWidths[(int)c];
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
            Bitmap[] chr = new Bitmap[256];
            int[] width = new int[256];
            int fontstart;
            int spacing = Math.Max(1, (int)(font.Height * 0.05));

            for (int i = 0; i < 256; i++)
            {
                chr[i] = RenderChar(font, (char)i);

                fontstart = chr[i].Width;
                int fontend = 0;
                for (int h = 0; h < chr[i].Height; h++)
                {
                    for (int w = 0; w < chr[i].Width; w++)
                    {
                        bool pixel = chr[i].GetPixel(w, h).A == 255;
                        if (pixel && w > fontend)
                            fontend = w;
                        if (pixel && w < fontstart)
                            fontstart = w;
                    }
                }
                if (fontend < fontstart)
                {
                    width[i] = font.Height / 4;     //set empty character width to be 1/4 height
                }
                else
                    width[i] = (fontend - fontstart + 1 + spacing)*203/84;
            }
            return width;
        }

        public static byte[] CustomFontStore(Font font, ZebraFont name, ElementUploadRotation rotation, FontCharsetType charset)
        {
            if (name < ZebraFont.CUSTOM_A)
            {
                throw new ApplicationException("Invalid font name selected for storing");
            }

            List<byte> sendBytes = new List<byte>();
            byte[] init = {(byte)10, (byte)'N', (byte)10, (byte)'E', (byte)'K', (byte)'"', (byte)name, (byte)'"', (byte)10,
                              (byte)'E', (byte)'S', (byte)'"', (byte)name, (byte)'"'};
            //EK"a"
            //ES"a"
            sendBytes.AddRange(init);

            int characterCount = FontCharset.CharList[(int)charset].Length;
            sendBytes.Add((byte)characterCount); //P1 - Number of characters we're sending
            sendBytes.Add((byte)(int)rotation);  //P2 - 0 - portrait, 1 - Landscape, 2 - both
            sendBytes.Add((byte)font.Height);    //P3 - Font height

            //calculate character spacing - 5% of font height or 1 dot, whichever is more
            int spacing = Math.Max(1, (int)(font.Height * 0.05));

            Bitmap[] chr = new Bitmap[characterCount];
            int currentChr;
            int[] fontstart = new int[characterCount];
            int[] width = new int[characterCount];
            int bytewidth;

            for (int i = 0; i < characterCount; i++)
            {
                //Use OEM encoding.  
                currentChr = System.Text.Encoding.GetEncoding(437).GetBytes(FontCharset.CharList[(int)charset])[i];
                chr[i] = RenderChar(font, FontCharset.CharList[(int)charset][i]);

                fontstart[i] = chr[i].Width;
                int fontend = 0;
                for (int h = 0; h < chr[i].Height; h++)
                {
                    for (int w = 0; w < chr[i].Width; w++)
                    {
                        bool pixel = chr[i].GetPixel(w, h).A == 255;
                        if (pixel && w > fontend)
                            fontend = w;
                        if (pixel && w < fontstart[i])
                            fontstart[i] = w;
                    }
                }
                if (fontend < fontstart[i])
                {
                    width[i] = font.Height / 4;     //set empty character width to be 1/4 height
                    fontstart[i] = 0;
                }
                else
                    width[i] = fontend - fontstart[i] + 1 + spacing;

                if (rotation == ElementUploadRotation.NO_ROTATION || rotation == ElementUploadRotation.BOTH_ROTATIONS)
                {
                    bytewidth = width[i] % 8 > 0 ? (int)width[i] / 8 + 1 : width[i] / 8;
                    sendBytes.Add((byte)currentChr); //An - Character
                    sendBytes.Add((byte)width[i]);   //Bn - number of dots in character width (EPL manual is wrong)
                    sendBytes.Add((byte)bytewidth);  //Cn - number of bytes after padding (EPL manual is wrong)
                    
                    for (int h = 0; h < chr[i].Height; h++)
                    {
                        BitArray ba = new BitArray(8);
                        int k = 0;

                        for (int w = fontstart[i]; w < bytewidth * 8 + fontstart[i]; w++)
                        {
                            if (w < chr[i].Width)
                                ba[7 - k] = chr[i].GetPixel(w, h).A == 255;
                            else
                                ba[7 - k] = false;
                            k++;
                            if (k == 8)
                            {
                                byte b = ConvertToByte(ba);
                                sendBytes.Add(b);
                                k = 0;
/*                                for (int x = 7; x > 0; x--)
                                {
                                    if (ba[x])
                                        Console.Write("X");
                                    else
                                        Console.Write("_");
                                }
  */                          }
                        }
    //                    Console.WriteLine();
                    }
                }
            }

            if (rotation == ElementUploadRotation.ROTATE_90_DEGREES || rotation == ElementUploadRotation.BOTH_ROTATIONS)
            {
                bytewidth = font.Height % 8 > 0 ? (int)font.Height / 8 + 1 : font.Height / 8;
                for (int i = 0; i < characterCount; i++)
                {
                    sendBytes.Add((byte)FontCharset.CharList[(int)charset][i]); //An
                    sendBytes.Add((byte)(width[i] + spacing));                  //Bn
                    sendBytes.Add((byte)(width[i] + spacing));                  //Cn

                    for (int w = fontstart[i]; w < width[i] + fontstart[i] + spacing; w++)
                    {
                        BitArray ba = new BitArray(8);
                        int k = 0;

                        for (int h = bytewidth * 8 - 1; h >= 0; h--)
                        {
                            if (h < chr[i].Height && w < chr[i].Width)
                                ba[7 - k] = chr[i].GetPixel(w, h).A == 255;
                            else
                                ba[7 - k] = false;
                            k++;
                            if (k == 8)
                            {
                                byte b = ConvertToByte(ba);
                                sendBytes.Add(b);
                                k = 0;
/*                                for (int x = 7; x > 0; x--)
                                {
                                    if (ba[x])
                                        Console.Write("X");
                                    else
                                        Console.Write("_");
                                }
*/                            }
                        }
//                        Console.WriteLine();
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
                value |= (byte)((bits[x] == true) ? (0x01 << x) : 0x00);
            }
            return value;
        }

        private static Bitmap RenderChar(Font font, char letter)
        {
            SizeF temps = TextRenderer.MeasureText(letter.ToString(), font);
            float fontScale = font.Height / temps.Height;
            Bitmap bm = new Bitmap((int)(temps.Width * fontScale), font.Height);

            using (Graphics g = Graphics.FromImage(bm))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                StringFormat stringFormat = new StringFormat()
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Near
                };
                Rectangle rect = new Rectangle(0, 0, bm.Width, bm.Height);
                // measure how large the text is on the Graphics object with the current font size
                using (Font fontForDrawing = new Font(font.FontFamily, (float)(font.SizeInPoints * fontScale), font.Style, GraphicsUnit.Point))
                {
                    g.DrawString(letter.ToString(), fontForDrawing, Brushes.Black, rect, stringFormat);
                }
            }
            return bm;
        }
    }
}
