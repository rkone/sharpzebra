using System;
using System.IO;
using System.Text;

namespace SharpZebra.Commands
{
    public partial class ZPLCommands
    {
        /// <summary>
        /// Initializes printer print speed, tear off, alignment, width and darkness
        /// </summary>
        /// <param name="settings">The variable containing all required settings</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] ClearPrinter(Printing.PrinterSettings settings)
        {
            //^MMT: Tear off Mode.  ^PRp,s,b: print speed (print, slew, backfeed) (2,4,5,6,8,9,10,11,12).  
            //~TA###: Tear off position (must be 3 digits). ^LS####: Left shift.  ^LHx,y: Label home. ^SD##x: Set Darkness (00 to 30). ^PWx: Label width
            //^XA^MMT^PR4,12,12~TA000^LS-20^LH0,12~SD19^PW750
            _stringCounter = 0;
            _printerSettings = settings;
            return Encoding.GetEncoding(850).GetBytes(string.Format("^XA^MMT^PR{0},12,12~TA{1:000}^LH{2},{3}~SD{4:00}^PW{5}", settings.PrintSpeed,
                settings.AlignTearOff, settings.AlignLeft, settings.AlignTop, settings.Darkness, settings.Width + settings.AlignLeft));
        }

        /// <summary>
        /// Instruct the Zebra printer to print labels
        /// </summary>
        /// <param name="copies">The number of identical copies of the label to print</param>
        /// <returns>>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] PrintBuffer(int copies  = 1)
        {
            return Encoding.GetEncoding(850).GetBytes(copies > 1 ? $"^PQ{copies}^XZ" : "^XZ");
        }

        /// <summary>
        /// Instruct the Zebra printer to print a barcode.  Currently only 3of9, Code128, UPC_A and EAN13 are supported.
        /// </summary>
        /// <param name="left">Distance in dots from the left of the label</param>
        /// <param name="top">Distance in dots to the top of the label</param>
        /// <param name="height">Height in dots of the barcode</param>
        /// <param name="rotation">Rotate field.</param>
        /// <param name="barcode">Type and parameters of the barcode to print.</param>
        /// <param name="readable">Enable text to be printed at the bottom of the barcode.</param>
        /// <param name="barcodeData">Text to encode in the barcode</param>
        /// <returns>>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] BarCodeWrite(int left, int top, int height, ElementDrawRotation rotation, Barcode barcode, bool readable, string barcodeData)
        {
            var encodedReadable = readable ? "Y" : "N";
            switch (barcode.Type)
            {
                case BarcodeType.CODE39_STD_EXT:
                    return Encoding.GetEncoding(850).GetBytes(
                        $"^FO{left},{top}^BY{barcode.BarWidthNarrow}^B3{(char) rotation},,{height},{encodedReadable}^FD{barcodeData}^FS");
                case BarcodeType.CODE128_AUTO:
                    return Encoding.GetEncoding(850).GetBytes(
                        $"^FO{left},{top}^BY{barcode.BarWidthNarrow}^BC{(char) rotation},{height},{encodedReadable}^FD{barcodeData}^FS");
                case BarcodeType.UPC_A:
                    return Encoding.GetEncoding(850).GetBytes(
                        $"^FO{left},{top}^BY{barcode.BarWidthNarrow}^BU{(char) rotation},{height},{encodedReadable}^FD{barcodeData}^FS");
                case BarcodeType.EAN13:
                    return Encoding.GetEncoding(850).GetBytes(string.Format("^FO{0},{1}^BY{2}^BE{3},{4},{5}^FD{6}^FS", left, top,
                        barcode.BarWidthNarrow, (char)rotation, height, encodedReadable, barcodeData));
                default:
                    throw new ApplicationException("Barcode not yet supported by SharpZebra library.");
            }
        }

        /// <summary>
        /// Writes Data Matrix Bar Code for ZPL. 
        /// ZPL Command: ^BX.
        /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf#page=122"/>     
        /// </summary>
        /// <param name="left">Horizontal axis.</param>
        /// <param name="top">Vertical axis.</param>
        /// <param name="height">Height is determined by dimension and data that is encoded.</param>
        /// <param name="rotation">Rotate field.</param>
        /// <param name="text">Text to be encoded</param>                            
        /// <param name="qualityLevel">Version of Data Matrix.</param>
        /// <param name="aspectRatio">Choices the symbol, it is possible encode the same data in two forms of Data Matrix, a square form or rectangular.</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] DataMatrixWrite(int left, int top, ElementDrawRotation rotation, int height, string text, QualityLevel qualityLevel = QualityLevel.ECC_200, AspectRatio aspectRatio = AspectRatio.SQUARE)
        {
            var rotationValue = (char)rotation;
            var qualityLevelValue = (int)qualityLevel;
            var aspectRatioValue = (int)aspectRatio;

            return Encoding.GetEncoding(850).GetBytes($"^FO{left},{top}^BX{rotationValue}, {height},{qualityLevelValue},,,,,{aspectRatioValue},^FD{text}^FS");
        }

        /// <summary>
        /// Writes text using the printer's (hopefully) built-in font.
        /// <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf#page=1336"/>
        /// ZPL Command: ^A.
        /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf#page=42"/>     
        /// </summary>
        /// <param name="left">Horizontal axis.</param>
        /// <param name="top">Vertical axis.</param>
        /// <param name="rotation">Rotate field.</param>
        /// <param name="font">ZebraFont to print with. Note: these enum names do not match printer output</param>
        /// <param name="height">Height of text in dots. 10-32000, or 0 to scale based on width</param>
        /// <param name="width">Width of text in dots. 10-32000, default or 0 to scale based on height</param>
        /// <param name="text">Text to be written</param>                            
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        [Obsolete("Use ZPLFont instead of ZebraFont.")]
        public static byte[] TextWrite(int left, int top, ElementDrawRotation rotation, ZebraFont font, int height, int width = 0, string text = "", int codepage = 850)
        {
            return string.IsNullOrEmpty(text)
                ? new byte[0]
                : Encoding.GetEncoding(codepage)
                    .GetBytes($"^FO{left},{top}^A{(char) font}{(char) rotation},{height},{width}{FixTilde(text)}FH");
        }

        /// <summary>
        /// Writes text using the printer's (hopefully) built-in font.
        /// <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf#page=1336"/>
        /// ZPL Command: ^A.
        /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf#page=42"/>     
        /// </summary>
        /// <param name="left">Horizontal axis.</param>
        /// <param name="top">Vertical axis.</param>
        /// <param name="rotation">Rotate field.</param>
        /// <param name="font">ZPLFont to print with.</param>
        /// <param name="height">Height of text in dots. 10-32000, or 0 to scale based on width</param>
        /// <param name="width">Width of text in dots. 10-32000, default or 0 to scale based on height</param>
        /// <param name="text">Text to be written</param>                            
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] TextWrite(int left, int top, ElementDrawRotation rotation, ZPLFont font, int height, int width = 0, string text = "", int codepage = 850)
        {
            return string.IsNullOrEmpty(text)
                ? new byte[0]
                : Encoding.GetEncoding(codepage)
                    .GetBytes($"^FO{left},{top}^A{(char) font}{(char) rotation},{height},{width}{FixTilde(text)}");
        }

        /// <summary>
        /// Writes text using a font previously uploaded to the printer.
        /// ZPL Command: ^A@.
        /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf#page=44"/>     
        /// </summary>
        /// <param name="left">Horizontal axis.</param>
        /// <param name="top">Vertical axis.</param>
        /// <param name="rotation">Rotate field.</param>
        /// <param name="fontName">The name of the font from the printer's directory listing (ends in .FNT)</param>
        /// <param name="storageArea">The drive the font is stored on. From your printer's directory listing.</param>
        /// <param name="height">Height of text in dots for scalable fonts, nearest magnification found for bitmapped fonts (R, E, B or A)</param>
        /// <param name="text">Text to be written</param>                            
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] TextWrite(int left, int top, ElementDrawRotation rotation, string fontName, char storageArea, int height, string text, int codepage = 850)
        {
            var rotationValue = (char) rotation;
            return string.IsNullOrEmpty(text)
                ? new byte[0]
                : Encoding.GetEncoding(codepage).GetBytes(string.Format("^A@{0},{1},{1},{2}:{3}^FO{4},{5}{6}",
                    rotationValue, height, storageArea, fontName, left, top, FixTilde(text)));
        }

        /// <summary>
        /// Writes text using a font previously uploaded to the printer. Prints with the last used font.
        /// ZPL Command: ^A@.
        /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf#page=44"/>     
        /// </summary>
        /// <param name="left">Horizontal axis.</param>
        /// <param name="top">Vertical axis.</param>
        /// <param name="rotation">Rotate field.</param>
        /// <param name="height">Height of text in dots for scalable fonts, nearest magnification found for bitmapped fonts (R, E, B or A)</param>
        /// <param name="text">Text to be written</param>                            
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] TextWrite(int left, int top, ElementDrawRotation rotation, int height, string text, int codepage = 850)
        {
            //uses last specified font
            return string.IsNullOrEmpty(text)
                ? new byte[0]
                : Encoding.GetEncoding(codepage)
                    .GetBytes($"^A@{(char) rotation},{height}^FO{left},{top}{FixTilde(text)}");
        }

        /// <summary>
        /// Encases a Textwrite into a alignable box. Top left corner is determined in the TextWrite command.
        /// </summary>
        /// <param name="width">Width of the box to align the text inside</param>
        /// <param name="alignment">Left, right, centered, justified</param>
        /// <param name="textCommand">Results of a TextWrite command</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] TextAlign(int width, Alignment alignment, byte[] textCommand)
        {
            return TextAlign(width, alignment, 1, 0, 0, textCommand);
        }

        /// <summary>
        /// Encases a Textwrite into a alignable box with custom line heights and a maximum height. Top left corner is determined in the TextWrite command.
        /// </summary>
        /// <param name="width">Width of the alignment box</param>
        /// <param name="alignment">Left, right, centered, justified</param>
        /// <param name="maxLines">maximum lines to allow the text wrap before cutting it off</param>
        /// <param name="lineSpacing">dots between each line</param>
        /// <param name="indentSize">dots to indent after the first line</param>
        /// <param name="textCommand">Results of a TextWrite command</param>
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] TextAlign(int width, Alignment alignment, int maxLines, int lineSpacing, int indentSize, byte[] textCommand, int codepage = 850)
        {
            //limits from ZPL Manual:
            //width [0,9999]
            //maxLines [1,9999]
            //lineSpacing [-9999,9999]
            //indentSize [0,9999]
            var alignmentValue = (char) alignment;
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            writer.Write(textCommand, 0, textCommand.Length - 3); //strip ^FS from given command
            var s = $"^FB{width},{maxLines},{lineSpacing},{alignmentValue},{indentSize}^FS";
            writer.Write(Encoding.GetEncoding(codepage).GetBytes(s));
            return stream.ToArray();
        }

        public static byte[] LineWrite(int left, int top, int lineThickness, int right, int bottom)
        {
            var height = top - bottom;
            var width = right - left;
            var diagonal = height * width < 0 ? 'L' : 'R';
            var l = Math.Min(left, right);
            var t = Math.Min(top, bottom);
            height = Math.Abs(height);
            width = Math.Abs(width);

            //zpl requires that straight lines are drawn with GB (Graphic-Box)
            if (width < lineThickness)
                return BoxWrite(left - lineThickness / 2, top, lineThickness, width, height, 0);
            if (height < lineThickness)
                return BoxWrite(left, top - lineThickness / 2, lineThickness, width, height, 0);

            return Encoding.GetEncoding(850).GetBytes($"^FO{l},{t}^GD{width},{height},{lineThickness},,{diagonal}^FS");
        }

        public static byte[] BoxWrite(int left, int top, int lineThickness, int width, int height, int rounding)
        {
            return Encoding.GetEncoding(850).GetBytes(
                $"^FO{left},{top}^GB{Math.Max(width, lineThickness)},{Math.Max(height, lineThickness)},{lineThickness},,{rounding}^FS");
        }

        private static string FixTilde(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (!text.Contains("~"))
                return $"^FD{text}^FS";
            if (text.Contains("_"))
                throw new ApplicationException("Tilde character is not supported with underscore in same command");
            return $"^FH^FD{text.Replace("~", "_7e")}^FS";
        }
        
        /*
        public static string FormDelete(string formName)
        {
            return string.Format("FK\"{0}\"\n", formName);
        }

        public static string FormCreateBegin(string formName)
        {
            return string.Format("{0}FS\"{1}\"\n", FormDelete(formName), formName);
        }

        public static string FormCreateFinish()
        {
            return string.Format("FE\n");
        }
        */
    }
}
