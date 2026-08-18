using System.Text;
using System.Collections.Generic;
using System.Collections;
using System;
using SkiaSharp;

namespace SharpZebra.Commands;

public partial class ZPLCommands
{   
    public class SKCustomString : IDisposable
    {
        private SKFont? _font;
        private ElementDrawRotation _rotation;
        private string? _text;
        private bool _inverse;
        private SKBitmap? _customImage;

        public string? Text
        {
            get => _text;
            set
            {
                if (value == _text) return;
                _text = value;
                InitGraphic();
            }
        }

        public SKFont? Font
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

        public SKBitmap? CustomImage => _customImage;

        public int TextWidth => _customImage?.Width ?? 0;

        public int TextHeight => _customImage?.Height ?? 0;

        private void InitGraphic()
        {
            if (_font == null || string.IsNullOrEmpty(_text))
            {
                _customImage = null;
                return;
            }
            using var scaledFont = new SKFont(_font.Typeface, _font.Size * 203f / 72); //should get printer dpi...
            using var paint = new SKPaint();
            paint.Color = _inverse ? SKColors.White : SKColors.Black;

            scaledFont.MeasureText(_text, out var skBounds);

            _customImage = new SKBitmap((int)Math.Ceiling(skBounds.Width), (int)Math.Ceiling(skBounds.Height), SKColorType.Gray8, SKAlphaType.Opaque);
            _customImage.Erase(_inverse ? SKColors.Black : SKColors.White);

            using var canvas = new SKCanvas(_customImage);
            canvas.Clear(_inverse ? SKColors.Black : SKColors.White);
            canvas.DrawText(_text, 0, -skBounds.Top, SKTextAlign.Left, scaledFont, paint);

            switch (_rotation)
            {
                case ElementDrawRotation.ROTATE_90_DEGREES:
                    canvas.RotateDegrees(90);
                    break;
                case ElementDrawRotation.ROTATE_180_DEGREES:
                    canvas.RotateDegrees(180);
                    break;
                case ElementDrawRotation.ROTATE_270_DEGREES:
                    canvas.RotateDegrees(270);
                    break;
            }                        
        }

        /// <summary>
        /// Releases the rendered image held by this SKCustomString.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_customImage != null)
                {
                    _customImage.Dispose();
                    _customImage = null;
                }
            }
        }
    }

    /// <summary>
    /// Write any text in any supported font style to the printer - including international characters!
    /// Note that if your printer's RAM drive letter is something other than 'R', set the ramDrive variable or call ClearPrinter first!
    /// </summary>
    /// <param name="left"></param>
    /// <param name="top"></param>
    /// <param name="rotation"></param>
    /// <param name="font"></param>
    /// <param name="text"></param>
    /// /// <param name="ramDrive">Location of your printer's ram drive</param>
    /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer</returns>
    public static byte[] CustomStringWrite(int left, int top, ElementDrawRotation rotation, SKFont font, string text, char? ramDrive = null)
    {
        using var s = new SKCustomString { Font = font, Rotation = rotation, Text = text, Inverse = false };
        return CustomStringWrite(left, top, s, ramDrive);
    }

    /// <summary>
    /// Same as CustomStringWrite, but prints the text inverted (white text on a black background).
    /// Note that if your printer's RAM drive letter is something other than 'R', set the ramDrive variable or call ClearPrinter first!
    /// </summary>
    /// <param name="left">Distance in dots from the left of the label</param>
    /// <param name="top">Distance in dots to the top of the label</param>
    /// <param name="rotation">Rotate field.</param>
    /// <param name="font">Font to render the text with</param>
    /// <param name="text">Text to be written</param>
    /// <param name="ramDrive">Location of your printer's ram drive</param>
    /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer</returns>
    public static byte[] CustomInverseStringWrite(int left, int top, ElementDrawRotation rotation, SKFont font, string text, char? ramDrive = null)
    {
        using var s = new SKCustomString { Font = font, Rotation = rotation, Text = text, Inverse = true };
        return CustomStringWrite(left, top, s, ramDrive);
    }

    /// <summary>
    /// Writes a pre-built SKCustomString to the printer: its rendered image is uploaded to the printer's
    /// RAM drive under a generated name and then placed on the label.
    /// Note that if your printer's RAM drive letter is something other than 'R', set the ramDrive variable or call ClearPrinter first!
    /// </summary>
    /// <param name="left">Distance in dots from the left of the label</param>
    /// <param name="top">Distance in dots to the top of the label</param>
    /// <param name="customString">The text, font, rotation and inversion to print</param>
    /// <param name="ramDrive">Location of your printer's ram drive</param>
    /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer</returns>
    public static byte[] CustomStringWrite(int left, int top, SKCustomString customString, char? ramDrive = null)
    {
        if (customString.CustomImage is null) throw new ArgumentException("Image has not been set for custom string.");
        _stringCounter++;
        var name = $"SZT{_stringCounter:00000}";
        var res = new List<byte>();
        var drive = ramDrive ?? _printerSettings?.RamDrive ?? 'R';
        res.AddRange(GraphicStore(customString.CustomImage, drive, name));
        res.AddRange(GraphicWrite(left, top, name, drive));
        return res.ToArray();
    }

    /// <summary>
    /// Uploads a bitmap to the printer's memory as a 1-bit .GRF graphic. Pixels are converted using
    /// the image's red channel (red below 128 = black); no dithering is done.
    /// ZPL Command: ~DG.
    /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf"/>
    /// </summary>
    /// <param name="image">Image to upload</param>
    /// <param name="storageArea">The drive to store the graphic on</param>
    /// <param name="imageName">Name to store the graphic under</param>
    /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
    public static byte[] GraphicStore(SKBitmap image, char storageArea, string imageName)
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
                        ba[k] = image.GetPixel(scanx, y).Red < 128;
                    scanx++;
                }
                res.AddRange(Encoding.GetEncoding(850).GetBytes($"{ConvertToByte(ba):X2}"));
            }
            res.AddRange(Encoding.GetEncoding(850).GetBytes("\n"));
        }
        return res.ToArray();
    }
}
