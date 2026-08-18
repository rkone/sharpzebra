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
        /// Instruct the Zebra printer to print a barcode.  Currently only 3of9, Code93, Code128, UPC_A, UPC_E,
        /// EAN8, EAN13, SSCC, Interleaved 2 of 5 and GTIN-14 are supported.
        /// When the barcode's BearerBars is not NONE, bearer bars are drawn around the symbology and the
        /// interpretation line is printed manually outside the bars.
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

            //without bearer bars, print the bare symbology command with ZPL's own interpretation line
            //(GTIN-14 always uses the assembly below so its quiet zones and manual text are kept)
            if (barcode.BearerBars == BearerBarType.NONE && barcode.Type != BarcodeType.GTIN14)
            {
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
                    case BarcodeType.INTERLEAVED_2OF5:
                        return Encoding.GetEncoding(850).GetBytes($"^FO{left},{top}^BY{barcode.BarWidthNarrow}^B2{(char)rotation},{height},{encodedReadable},,^FD{barcodeData}^FS");
                    default:
                        throw new ArgumentException("Barcode not yet supported by SharpZebra library.");
                }
            }

            int narrow = barcode.BarWidthNarrow ?? 2;
            //BearerBarWidth of 0 or less selects the conventional default of 3x the narrow bar
            int bearer = barcode.BearerBars == BearerBarType.NONE ? 0
                : barcode.BearerBarWidth > 0 ? barcode.BearerBarWidth : 3 * narrow;
            //^B2 adds a leading zero to odd-length data; each digit pair is 18 modules (at 3:1), start/stop add 9
            int i2of5Digits = barcodeData.Length + barcodeData.Length % 2;
            int ssccDigits = 0;
            foreach (var c in barcodeData) if (c >= '0' && c <= '9') ssccDigits++;
            //drawing bearer bars requires the symbol's rendered width: modules (multiples of the narrow bar)
            //per symbology, paired with its command with ZPL's interpretation line disabled
            int modules;
            string command;
            switch (barcode.Type)
            {
                case BarcodeType.CODE39_STD_EXT:
                    //16 modules per character (at 3:1, incl. intercharacter gap) plus start/stop, less the trailing gap
                    modules = 16 * (barcodeData.Length + 2) - 1;
                    command = $"^B3{(char)rotation},,{height},N";
                    break;
                case BarcodeType.CODE128_AUTO:
                    //subset B: 11 modules per character plus start, check and stop
                    modules = 11 * barcodeData.Length + 35;
                    command = $"^BC{(char)rotation},{height},N";
                    break;
                case BarcodeType.UPC_A:
                    modules = 95;
                    command = $"^BU{(char)rotation},{height},N";
                    break;
                case BarcodeType.EAN13:
                    modules = 95;
                    command = $"^BE{(char)rotation},{height},N";
                    break;
                case BarcodeType.EAN8:
                    modules = 67;
                    command = $"^B8{(char)rotation},{height},N";
                    break;
                case BarcodeType.UPC_E:
                    modules = 51;
                    command = $"^B9{(char)rotation},{height},N";
                    break;
                case BarcodeType.CODE93:
                    //9 modules per character plus start, two check characters, stop and termination bar
                    modules = 9 * (barcodeData.Length + 4) + 1;
                    command = $"^BA{(char)rotation},{height},N,N,N";
                    break;
                case BarcodeType.SSCC:
                    //subset C digit pairs plus start, FNC1, check and stop; non-digits are not encoded
                    modules = 11 * ((ssccDigits + 1) / 2 + 3) + 13;
                    command = $"^BC{(char)rotation},{height},N,N,,D";
                    break;
                case BarcodeType.INTERLEAVED_2OF5:
                case BarcodeType.GTIN14:
                    modules = 9 * i2of5Digits + 9;
                    command = $"^B2{(char)rotation},{height},N,N,N";
                    break;
                default:
                    throw new ArgumentException("Barcode not yet supported by SharpZebra library.");
            }
            int symbolWidth = modules * narrow;
            int boxWidth = symbolWidth + 2 * (50 + bearer);
            int textHeight = height / 6;
            //the interpretation line sits inside the frame for ENCLOSING, below the bearer bars otherwise
            int frameHeight = barcode.BearerBars == BearerBarType.ENCLOSING
                ? height + textHeight + 10 + 2 * bearer
                : height + 2 * bearer;
            int textTop = barcode.BearerBars == BearerBarType.ENCLOSING
                ? bearer + height + 5
                : height + 2 * bearer + 5;
            bool vertical = rotation == ElementDrawRotation.ROTATE_90_DEGREES || rotation == ElementDrawRotation.ROTATE_270_DEGREES;
            //layout is computed unrotated ((0,0) = frame top left, x running along the bars), then each
            //element is placed so the rotated assembly keeps its top left corner at (left, top)
            string Place(int x, int y, int w, int h)
            {
                switch (rotation)
                {
                    case ElementDrawRotation.ROTATE_90_DEGREES:
                        return $"^FO{left + frameHeight - y - h},{top + x}";
                    case ElementDrawRotation.ROTATE_180_DEGREES:
                        return $"^FO{left + boxWidth - x - w},{top + frameHeight - y - h}";
                    case ElementDrawRotation.ROTATE_270_DEGREES:
                        return $"^FO{left + y},{top + boxWidth - x - w}";
                    default:
                        return $"^FO{left + x},{top + y}";
                }
            }
            string Box(int x, int y, int w, int h, int border)
            {
                return Place(x, y, w, h) + (vertical ? $"^GB{h},{w},{border}^FS" : $"^GB{w},{h},{border}^FS");
            }
            string bearerZpl;
            switch (barcode.BearerBars)
            {
                //frame around the barcode and quiet zones (plus the interpretation line for ENCLOSING)
                case BearerBarType.ABUTTING:
                case BearerBarType.ENCLOSING:
                    bearerZpl = Box(0, 0, boxWidth, frameHeight, bearer);
                    break;
                //bearer bars along the top and bottom of the barcode, spanning the quiet zones
                case BearerBarType.HORIZONTAL:
                    bearerZpl = Box(0, 0, boxWidth, bearer, bearer) +
                                Box(0, bearer + height, boxWidth, bearer, bearer);
                    break;
                default:
                    bearerZpl = string.Empty;
                    break;
            }
            //interpretation line is always printed manually; GS1 spacing when the data is a full GTIN-14
            var text = barcode.Type == BarcodeType.GTIN14 && barcodeData.Length == 14
                ? $"{barcodeData[0]} {barcodeData.Substring(1, 2)} {barcodeData.Substring(3, 5)} {barcodeData.Substring(8, 5)} {barcodeData[13]}"
                : barcodeData;
            var textZpl = readable
                ? Place(0, textTop, boxWidth, textHeight) + $"^A0{(char)rotation},{textHeight},{textHeight}^FD{text}^FB{boxWidth},1,0,C,^FS"
                : string.Empty;
            return Encoding.GetEncoding(850).GetBytes(
                bearerZpl +
                Place(50 + bearer, bearer, symbolWidth, height) + $"^BY{narrow},3,{command}^FD{barcodeData}^FS" +
                textZpl);
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
