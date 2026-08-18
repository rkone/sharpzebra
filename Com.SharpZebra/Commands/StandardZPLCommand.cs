using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

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
            return Encoding.GetEncoding(850).GetBytes(
                $"^XA^MMT^PR{settings.PrintSpeed},{settings.SlewSpeed},{settings.BackfeedSpeed}~TA{settings.AlignTearOff:000}^LH{settings.AlignLeft},{settings.AlignTop}~SD{settings.Darkness:00}^PW{settings.Width + settings.AlignLeft}" +
                (settings.Length > 0 ? $"^LL{settings.Length}" : ""));
        }

        /// <summary>
        /// Instruct the Zebra printer to print labels
        /// </summary>
        /// <param name="copies">The number of identical copies of the label to print</param>
        /// <returns>>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] PrintBuffer(int copies = 1)
        {
            return Encoding.GetEncoding(850).GetBytes(copies > 1 ? $"^PQ{copies}^XZ" : "^XZ");
        }

        /// <summary>
        /// Instruct the Zebra printer to print a barcode.  Currently only 3of9, Code93, Code128, UPC_A, UPC_E, EAN8, EAN13 and SSCC are supported.
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
            if (barcodeData is null) return new byte[0];
            var encodedReadable = readable ? "Y" : "N";
            switch (barcode.Type)
            {
                case BarcodeType.CODE39_STD_EXT:
                    return Encoding.GetEncoding(850).GetBytes($"^FO{left},{top}^BY{barcode.BarWidthNarrow}^B3{(char)rotation},,{height},{encodedReadable}^FD{barcodeData}^FS");                    
                case BarcodeType.CODE128_AUTO:
                    return Encoding.GetEncoding(850).GetBytes($"^FO{left},{top}^BY{barcode.BarWidthNarrow}^BC{(char)rotation},{height},{encodedReadable}^FD{barcodeData}^FS");
                case BarcodeType.EAN13:
                    return Encoding.GetEncoding(850).GetBytes($"^FO{left},{top}^BY{barcode.BarWidthNarrow}^BE{(char)rotation},{height},{encodedReadable}^FD{barcodeData}^FS");
                case BarcodeType.UPC_A:
                    return Encoding.GetEncoding(850).GetBytes($"^FO{left},{top}^BY{barcode.BarWidthNarrow}^BU{(char)rotation},{height},{encodedReadable}^FD{barcodeData}^FS");
                case BarcodeType.EAN8:
                    return Encoding.GetEncoding(850).GetBytes($"^FO{left},{top}^BY{barcode.BarWidthNarrow}^B8{(char)rotation},{height},{encodedReadable}^FD{barcodeData}^FS");
                case BarcodeType.UPC_E:
                    return Encoding.GetEncoding(850).GetBytes($"^FO{left},{top}^BY{barcode.BarWidthNarrow}^B9{(char)rotation},{height},{encodedReadable}^FD{barcodeData}^FS");
                case BarcodeType.CODE93:
                    return Encoding.GetEncoding(850).GetBytes($"^FO{left},{top}^BY{barcode.BarWidthNarrow}^BA{(char)rotation},{height},{encodedReadable},N,N^FD{barcodeData}^FS");
                case BarcodeType.SSCC:
                    return Encoding.GetEncoding(850).GetBytes($"^FO{left},{top}^BY{barcode.BarWidthNarrow}^BC{(char)rotation},{height},{encodedReadable},N,,D^FD{barcodeData}^FS");

                default:
                    throw new ArgumentException("Barcode not yet supported by SharpZebra library.");
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
            if (string.IsNullOrEmpty(text)) return new byte[0];
            var rotationValue = (char)rotation;
            var qualityLevelValue = (int)qualityLevel;
            var aspectRatioValue = (int)aspectRatio;

            return Encoding.GetEncoding(850).GetBytes($"^FO{left},{top}^BX{rotationValue}, {height},{qualityLevelValue},,,,,{aspectRatioValue},^FD{text}^FS");
        }

        /// <summary>
        /// Writes QR Code for ZPL. 
        /// ZPL Command: ^BQ.
        /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf#page=113"/>     
        /// </summary>
        /// <param name="left">Horizontal axis.</param>
        /// <param name="top">Vertical axis.</param>
        /// <param name="height">Height is determined by dimension and data that is encoded.</param>
        /// <param name="magnificationfactor">Scale the QR Code, from 1 to 10  (1 = small, 10 = really big).</param>
        /// <param name="text">Text to be encoded</param>
        /// <param name="qualityLevel">Error correction (L, M, Q or H).</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] QRCodeWrite(int left, int top, int magnificationFactor, string text, string qualityLevel = "M")
        {
            return string.IsNullOrEmpty(text) ? new byte[0] : Encoding.GetEncoding(850).GetBytes($"^FO{left},{top}^BQN,2,{magnificationFactor},{qualityLevel},^FD{qualityLevel}A,{text}^FS");
        }

        /// <summary>
        /// Writes a PDF417 barcode for ZPL.
        /// ZPL Command: ^B7.
        /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf"/>
        /// </summary>
        /// <param name="left">Horizontal axis.</param>
        /// <param name="top">Vertical axis.</param>
        /// <param name="rotation">Rotate field.</param>
        /// <param name="rowHeight">Height of an individual row in dots. Total symbol height is rowHeight x rows.</param>
        /// <param name="text">Text to be encoded</param>
        /// <param name="securityLevel">Error correction level, 1-8. Each level doubles the error correction codewords. 0 uses the printer default of error detection only.</param>
        /// <param name="columns">Number of data columns, 1-30, or 0 to let the printer pick a 1:2 row-to-column aspect ratio</param>
        /// <param name="rows">Number of rows, 3-90, or 0 to let the printer pick</param>
        /// <param name="truncate">Print in truncated mode (right row indicators and stop pattern omitted)</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] PDF417Write(int left, int top, ElementDrawRotation rotation, int rowHeight, string text, int securityLevel = 0, int columns = 0, int rows = 0, bool truncate = false)
        {
            if (string.IsNullOrEmpty(text)) return new byte[0];
            var columnsValue = columns > 0 ? columns.ToString() : "";
            var rowsValue = rows > 0 ? rows.ToString() : "";
            return Encoding.GetEncoding(850).GetBytes(
                $"^FO{left},{top}^B7{(char)rotation},{rowHeight},{securityLevel},{columnsValue},{rowsValue},{(truncate ? "Y" : "N")}^FD{text}^FS");
        }

        /// <summary>
        /// Writes an Aztec barcode for ZPL.
        /// ZPL Command: ^B0.
        /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf"/>
        /// </summary>
        /// <param name="left">Horizontal axis.</param>
        /// <param name="top">Vertical axis.</param>
        /// <param name="rotation">Rotate field.</param>
        /// <param name="magnification">Scale of the symbol, 1 to 10</param>
        /// <param name="text">Text to be encoded</param>
        /// <param name="errorControl">0 for default error correction, 1-99 for a minimum error correction percentage,
        /// 101-104 to force a compact symbol of that many layers, 201-232 to force a full-range symbol of that many layers, 300 for a simple rune</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] AztecWrite(int left, int top, ElementDrawRotation rotation, int magnification, string text, int errorControl = 0)
        {
            if (string.IsNullOrEmpty(text)) return new byte[0];
            return Encoding.GetEncoding(850).GetBytes(
                $"^FO{left},{top}^B0{(char)rotation},{magnification},N,{errorControl},N,1^FD{text}^FS");
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
        public static byte[] TextWrite(int left, int top, ElementDrawRotation rotation, ZebraFont font, int height, int width = 0, string text = null, int codepage = 850)
        {
            return string.IsNullOrEmpty(text)
                ? new byte[0]
                : Encoding.GetEncoding(codepage)
                    .GetBytes($"^FO{left},{top}^A{(char)font}{(char)rotation},{height},{width}{FixTilde(text)}FH");
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
                    .GetBytes($"^FO{left},{top}^A{(char)font}{(char)rotation},{height},{width}{FixTilde(text)}");
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
            var rotationValue = (char)rotation;
            return string.IsNullOrEmpty(text)
                ? new byte[0]
                : Encoding.GetEncoding(codepage).GetBytes($"^A@{rotationValue},{height},{height},{storageArea}:{fontName}^FO{left},{top}{FixTilde(text)}");
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
                    .GetBytes($"^A@{(char)rotation},{height}^FO{left},{top}{FixTilde(text)}");
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
            if (textCommand.Length < 3) return new byte[0];
            var alignmentValue = (char)alignment;
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            writer.Write(textCommand, 0, textCommand.Length - 3); //strip ^FS from given command
            var s = $"^FB{width},{maxLines},{lineSpacing},{alignmentValue},{indentSize}^FS";
            writer.Write(Encoding.GetEncoding(codepage).GetBytes(s));
            return stream.ToArray();
        }

        /// <summary>
        /// Draws a line between two points. Vertical and horizontal lines are drawn with the ^GB (Graphic Box)
        /// command as ZPL requires; anything else is drawn with ^GD (Graphic Diagonal Line).
        /// ZPL Commands: ^GB, ^GD.
        /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf"/>
        /// </summary>
        /// <param name="left">Distance in dots from the left of the label to the start of the line</param>
        /// <param name="top">Distance in dots from the top of the label to the start of the line</param>
        /// <param name="lineThickness">Thickness of the line in dots</param>
        /// <param name="right">Distance in dots from the left of the label to the end of the line</param>
        /// <param name="bottom">Distance in dots from the top of the label to the end of the line</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
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

        /// <summary>
        /// Draws a box, optionally with rounded corners. A line thickness matching the width or height draws a solid box.
        /// ZPL Command: ^GB.
        /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf"/>
        /// </summary>
        /// <param name="left">Distance in dots from the left of the label</param>
        /// <param name="top">Distance in dots to the top of the label</param>
        /// <param name="lineThickness">Border thickness in dots</param>
        /// <param name="width">Width of the box in dots</param>
        /// <param name="height">Height of the box in dots</param>
        /// <param name="rounding">Degree of corner rounding, 0 (none) to 8 (heaviest)</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] BoxWrite(int left, int top, int lineThickness, int width, int height, int rounding)
        {
            return Encoding.GetEncoding(850).GetBytes(
                $"^FO{left},{top}^GB{Math.Max(width, lineThickness)},{Math.Max(height, lineThickness)},{lineThickness},,{rounding}^FS");
        }

        /// <summary>
        /// Draws a circle.
        /// ZPL Command: ^GC.
        /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf#page=139"/>
        /// </summary>
        /// <param name="left">Distance in dots from the left of the label to the left edge of the circle</param>
        /// <param name="top">Distance in dots from the top of the label to the top edge of the circle</param>
        /// <param name="lineThickness">Border thickness in dots. A thickness of at least half the diameter draws a solid circle</param>
        /// <param name="diameter">Diameter of the circle in dots (3-4095)</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] CircleWrite(int left, int top, int lineThickness, int diameter)
        {
            return Encoding.GetEncoding(850).GetBytes(
                $"^FO{left},{top}^GC{diameter},{Math.Min(lineThickness, (diameter + 1) / 2)},B^FS");
        }

        /// <summary>
        /// Draws an ellipse.
        /// ZPL Command: ^GE.
        /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf"/>
        /// </summary>
        /// <param name="left">Distance in dots from the left of the label to the left edge of the ellipse</param>
        /// <param name="top">Distance in dots from the top of the label to the top edge of the ellipse</param>
        /// <param name="lineThickness">Border thickness in dots. A thickness of at least half the smaller dimension draws a solid ellipse</param>
        /// <param name="width">Width of the ellipse in dots (3-4095)</param>
        /// <param name="height">Height of the ellipse in dots (3-4095)</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] EllipseWrite(int left, int top, int lineThickness, int width, int height)
        {
            return Encoding.GetEncoding(850).GetBytes(
                $"^FO{left},{top}^GE{width},{height},{Math.Min(lineThickness, (Math.Min(width, height) + 1) / 2)},B^FS");
        }

        /// <summary>
        /// Prints a graphic symbol (®, ©, ™ or the UL/CSA certification marks), which cannot be produced through normal text fields.
        /// ZPL Command: ^GS.
        /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf"/>
        /// </summary>
        /// <param name="left">Distance in dots from the left of the label</param>
        /// <param name="top">Distance in dots to the top of the label</param>
        /// <param name="rotation">Rotate field.</param>
        /// <param name="symbol">The symbol to print</param>
        /// <param name="height">Height of the symbol in dots</param>
        /// <param name="width">Width of the symbol in dots, default or 0 to match the height</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] SymbolWrite(int left, int top, ElementDrawRotation rotation, ZPLSymbol symbol, int height, int width = 0)
        {
            return Encoding.GetEncoding(850).GetBytes(
                $"^FO{left},{top}^GS{(char)rotation},{height},{(width > 0 ? width : height)}^FD{(char)symbol}^FS");
        }

        /// <summary>
        /// Marks every field of a previously generated command as reversed: where the field overlaps
        /// something already drawn in black it prints white, and vice versa. Combine with BoxWrite to
        /// print white-on-black text, e.g. FieldReverse(TextWrite(...)) over a solid box.
        /// ZPL Command: ^FR.
        /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/zpl-zbi2-pm-en.pdf"/>
        /// </summary>
        /// <param name="fieldCommand">Results of another command, e.g. TextWrite, BoxWrite or BarCodeWrite</param>
        /// <returns>Array of bytes containing ZPLII data to be sent to the Zebra printer.</returns>
        public static byte[] FieldReverse(byte[] fieldCommand)
        {
            if (fieldCommand.Length == 0) return fieldCommand;
            //codepage 850 maps all 256 byte values, so the round trip preserves text in any single-byte codepage
            var zpl = Encoding.GetEncoding(850).GetString(fieldCommand);
            return Encoding.GetEncoding(850).GetBytes(Regex.Replace(zpl, @"\^FO-?\d+,-?\d+", "$0^FR"));
        }

        private static string FixTilde(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (!text.Contains("~"))
                return $"^FD{text}^FS";
            if (text.Contains("_"))
                throw new ArgumentException("Tilde character is not supported with underscore in same command");
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
