using System;
using System.IO;
using System.Text;

namespace Com.SharpZebra.Commands
{
    public partial class ZPLCommands
    {
        public static byte[] ClearPrinter(Printing.PrinterSettings settings)
        {
            //^MMT: Tear off Mode.  ^PRp,s,b: print speed (print, slew, backfeed) (2,4,5,6,8,9,10,11,12).  
            //~TA###: Tear off position (must be 3 digits). ^LS####: Left shift.  ^LHx,y: Label home. ^SD##x: Set Darkness (00 to 30). ^PWx: Label width
            //^XA^MMT^PR4,12,12~TA000^LS-20^LH0,12~SD19^PW750
            _stringCounter = 0;
            _printerSettings = settings;
            return Encoding.GetEncoding(850).GetBytes(string.Format("^XA^MMT^PR{0},12,12~TA{1:000}^LH{2},{3}~SD{4:00}^PW{5}", settings.PrintSpeed, 
                settings.AlignTearOff, settings.AlignLeft, settings.AlignTop, settings.Darkness, settings.Width+settings.AlignLeft));
        }        

        public static byte[] PrintBuffer(int copies)
        {
            return Encoding.GetEncoding(850).GetBytes(copies > 1 ? $"^PQ{copies}^XZ" : "^XZ");
        }

        public static byte[] BarCodeWrite(int left, int top, int height, ElementDrawRotation rotation, Barcode barcode, bool readable, string barcodeData)
        {
            var encodedReadable = readable ? "Y" : "N";
            switch (barcode.Type)
	        {
                case BarcodeType.CODE39_STD_EXT:
                    return Encoding.GetEncoding(850).GetBytes(string.Format("^FO{0},{1}^BY{2}^B3{3},,{4},{5}^FD{6}^FS", left, top, 
                        barcode.BarWidthNarrow, (char)rotation, height, encodedReadable, barcodeData));
                case BarcodeType.CODE128_AUTO:
                    return Encoding.GetEncoding(850).GetBytes(string.Format("^FO{0},{1}^BY{2}^BC{3},{4},{5}^FD{6}^FS", left, top,
                        barcode.BarWidthNarrow, (char)rotation, height, encodedReadable, barcodeData));
                case BarcodeType.UPC_A:
                    return Encoding.GetEncoding(850).GetBytes(string.Format("^FO{0},{1}^BY{2}^BU{3},{4},{5}^FD{6}^FS", left, top,
                        barcode.BarWidthNarrow, (char)rotation, height, encodedReadable, barcodeData));
                case BarcodeType.EAN13:
                    return Encoding.GetEncoding(850).GetBytes(string.Format("^FO{0},{1}^BY{2}^BE{3},{4},{5}^FD{6}^FS", left, top,
                        barcode.BarWidthNarrow, (char)rotation, height, encodedReadable, barcodeData));
		        default:
                    throw new ApplicationException("Barcode not yet supported by SharpZebra library.");                
	        }
        }

        public static byte[] TextWrite(int left, int top, ElementDrawRotation rotation, ZebraFont font, int height, int width, string text, int codepage = 850)
        {
            return Encoding.GetEncoding(codepage).GetBytes(string.Format("^FO{0},{1}^A{2}{3},{4},{5}^FD{6}^FS", left, top, (char)font, (char)rotation, height, width, text));
        }

        public static byte[] TextWrite(int left, int top, ElementDrawRotation rotation, string fontName, char storageArea, int height, string text, int codepage = 850)
        {
            return Encoding.GetEncoding(codepage).GetBytes(string.Format("^A@{0},{1},{1},{2}:{3}^FO{4},{5}^FD{6}^FS",(char)rotation, height, storageArea, fontName, left, top, text));
        }

        public static byte[] TextWrite(int left, int top, ElementDrawRotation rotation, int height, string text, int codepage = 850)
        {
            //uses last specified font
            return Encoding.GetEncoding(codepage).GetBytes(string.Format("^A@{0},{1}^FO{2},{3}^FD{4}^FS", (char)rotation, height, left, top, text));
        }

        public static byte[] TextAlign(int width, Alignment alignment, byte[] textCommand)
        {
            return TextAlign(width, alignment, 1, 0, 0, textCommand);
        }

        public static byte[] TextAlign (int width, Alignment alignment, int maxLines, int lineSpacing, int indentSize, byte[] textCommand, int codepage = 850)
        {
            //limits from ZPL Manual:
            //width [0,9999]
            //maxLines [1,9999]
            //lineSpacing [-9999,9999]
            //indentSize [0,9999]

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            byte[] cmd = new byte[textCommand.Length - 3];
            Array.Copy(textCommand, cmd, textCommand.Length-3);
            writer.Write(cmd); //strip ^FS from given command
            var s = string.Format("^FB{0},{1},{2},{3},{4}^FS", width, maxLines, lineSpacing, (char)alignment, indentSize);
            writer.Write(Encoding.GetEncoding(codepage).GetBytes(s));
            return stream.ToArray();
        }

        public static byte[] LineWrite(int left, int top, int lineThickness, int right, int bottom)
        {
            var height = top-bottom;
            var width = right - left;            
            var diagonal = height * width < 0 ? 'L' : 'R';
            var l = Math.Min(left, right);
            var t = Math.Min(top, bottom);
            height = Math.Abs(height);
            width = Math.Abs(width);

            //zpl requires that straight lines are drawn with GB (Graphic-Box)
            if (width < lineThickness)
                return BoxWrite(left-((int)(lineThickness/2)), top, lineThickness, width, height, 0);
            if (height < lineThickness)
                return BoxWrite(left, top-((int)(lineThickness/2)), lineThickness, width, height, 0);
            
            return Encoding.GetEncoding(850).GetBytes(string.Format("^FO{0},{1}^GD{2},{3},{4},{5},{6}^FS", l, t, width, height, 
                    lineThickness, "", diagonal));
        }

        public static byte[] BoxWrite(int left, int top, int lineThickness, int width, int height, int rounding)
        {
            return Encoding.GetEncoding(850).GetBytes(string.Format("^FO{0},{1}^GB{2},{3},{4},{5},{6}^FS", left, top, 
                Math.Max(width, lineThickness), Math.Max(height, lineThickness), lineThickness, "", rounding));
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
