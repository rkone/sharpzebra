using System.Drawing;
using System.Text;
using System.Collections.Generic;
using System.Collections;

namespace SharpZebra.Commands
{
    public partial class ZPLCommands
    {
        private static int _stringCounter;
        private static Printing.PrinterSettings _printerSettings;

        public class CustomString
        {
            private Font _font;
            private ElementDrawRotation _rotation;
            private string _text;
            private bool _inverse;
            private Bitmap _customImage;

            public string Text
            {
                get => _text;
                set
                {
                    if (value == _text) return;
                    _text = value;
                    InitGraphic();
                }
            }

            public Font Font
            {
                get => _font;
                set
                {
                    if (Equals(value, _font)) return;
                    _font = value;
                    InitGraphic();
                }
            }

            public bool Inverse
            {
                get => _inverse;
                set
                {
                    if (value == _inverse) return;
                    _inverse = value;
                    InitGraphic();
                }
            }

            public ElementDrawRotation Rotation
            {
                get => _rotation;
                set
                {
                    if (value == _rotation) return;
                    _rotation = value;
                    InitGraphic();
                }
            }

            public Bitmap CustomImage => _customImage;

            public int TextWidth => _customImage?.Width ?? 0;

            public int TextHeight => _customImage?.Height ?? 0;

            private void InitGraphic()
            {
                if (_font == null || string.IsNullOrEmpty(_text))
                {
                    _customImage = null;
                    return;
                }

                _customImage = new Bitmap(1, 1);
                var graphics = Graphics.FromImage(_customImage);
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                var sWidth = (int)graphics.MeasureString(_text, _font).Width;
                var sHeight = (int)graphics.MeasureString(_text, _font).Height;
                _customImage = new Bitmap(_customImage, sWidth, sHeight);

                using (var g = Graphics.FromImage(_customImage))
                {
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                    var stringFormat = new StringFormat()
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Near,
                        Trimming = StringTrimming.None
                    };

                    if (!_inverse)
                    {
                        g.Clear(Color.White);
                        g.DrawString(_text, _font, new SolidBrush(Color.Black), 0, 0, stringFormat);
                        g.Flush();
                    }
                    else
                    {
                        g.Clear(Color.Black);
                        g.DrawString(_text, _font, new SolidBrush(Color.White), 0, 0, stringFormat);
                        g.Flush();
                    }
                }
                switch (_rotation)
                {
                    case ElementDrawRotation.ROTATE_90_DEGREES:
                        _customImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        break;
                    case ElementDrawRotation.ROTATE_180_DEGREES:
                        _customImage.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        break;
                    case ElementDrawRotation.ROTATE_270_DEGREES:
                        _customImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        break;
                }
            }
        }

        /// <summary>
        /// Write any windows-supported text in any windows-supported font style to the printer - including international characters!
        /// Note that if your printer's RAM drive letter is something other than 'R', set the ramDrive variable or call ClearPrinter first!
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="rotation"></param>
        /// <param name="font"></param>
        /// <param name="text"></param>
        /// /// <param name="ramDrive">Location of your printer's ram drive</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer</returns>
        public static byte[] CustomStringWrite(int left, int top, ElementDrawRotation rotation, Font font, string text, char? ramDrive = null)
        {
            var s = new CustomString { Font = font, Rotation = rotation, Text = text, Inverse = false };
            return CustomStringWrite(left, top, s, ramDrive);
        }

        public static byte[] CustomInverseStringWrite(int left, int top, ElementDrawRotation rotation, Font font, string text, char? ramDrive = null)
        {
            var s = new CustomString { Font = font, Rotation = rotation, Text = text, Inverse = true };
            return CustomStringWrite(left, top, s, ramDrive);
        }

        public static byte[] CustomStringWrite(int left, int top, CustomString customString, char? ramDrive = null)
        {
            _stringCounter++;
            var name = $"SZT{_stringCounter:00000}";
            var res = new List<byte>();
            var drive = ramDrive ?? _printerSettings?.RamDrive ?? 'R';
            res.AddRange(GraphicStore(customString.CustomImage, drive, name));
            res.AddRange(GraphicWrite(left, top, name, drive));
            return res.ToArray();
        }

        public static byte[] GraphicWrite(int left, int top, string imageName, char storageArea)
        {
            return Encoding.GetEncoding(850).GetBytes($"^FO{left},{top}^XG{storageArea}:{imageName}.GRF^FS");
        }

        public static byte[] GraphicStore(Bitmap image, char storageArea, string imageName)
        {
            //Note that we're using the RED channel to determine if each pixel of an image is enabled.  
            //No dithering is done: values of red higher than 128 are on.
            var res = new List<byte>();
            var byteWidth = image.Width % 8 == 0 ? image.Width / 8 : image.Width / 8 + 1;
            res.AddRange(Encoding.GetEncoding(850).GetBytes($"~DG{storageArea}:{imageName},{image.Height * byteWidth},{byteWidth},"));

            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < byteWidth; x++)
                {
                    var ba = new BitArray(8);
                    var scanx = x * 8;
                    for (var k = 7; k >= 0; k--)
                    {
                        if (scanx >= image.Width)
                            ba[k] = false;
                        else
                            ba[k] = image.GetPixel(scanx, y).R < 128;
                        scanx++;
                    }
                    res.AddRange(Encoding.GetEncoding(850).GetBytes($"{ConvertToByte(ba):X2}"));
                }
                res.AddRange(Encoding.GetEncoding(850).GetBytes("\n"));
            }
            return res.ToArray();
        }

        public static byte[] GraphicDelete(char storageArea, string imageName)
        {
            return Encoding.GetEncoding(850).GetBytes($"^ID{storageArea}:{imageName}.GRF^FS");
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
    }
}
