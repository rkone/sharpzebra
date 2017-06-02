using System.Collections.Generic;
using System.Text;
using Com.SharpZebra.Commands;
using System.Drawing;
using Com.SharpZebra.Printing;

namespace Com.SharpZebra.Commands
{
    public partial class EPLCommands
    {
        public static byte[] ClearPrinter(SharpZebra.Printing.PrinterSettings settings)
        {       
            return Encoding.GetEncoding(437).GetBytes(string.Format("\nN\nO\nQ{0},{1}\nq{2}\nS{3}\nD{4}\nZB\nJF\nI8,{5:x},{6:000}\n", settings.Length + 10, 25,
                settings.Width + settings.AlignLeft, settings.PrintSpeed, settings.Darkness, (int)Codepage8.DOS_437, (int)Codepage8KDU.USA));  
        }

        public static byte[] PrintBuffer(int copies)
        {
            return Encoding.GetEncoding(437).GetBytes(string.Format("P{0}\n", copies));
        }

        public static byte[] BarCodeWrite(int left, int top, int height, ElementDrawRotation rotation, Barcode barcode, bool readable, 
            string barcodeData, PrinterSettings settings)
        {
            string encodedReadable = readable ? "B" : "N";
            return Encoding.GetEncoding(437).GetBytes(string.Format("B{0},{1},{2},{3},{4},{5},{6},{7},\"{8}\"\n", left + settings.AlignLeft, 
                top + settings.AlignTop, (int)rotation, barcode.P4Value, barcode.BarWidthNarrow, barcode.BarWidthWide, height, encodedReadable,
                barcodeData));
        }

        public static byte[] TextWrite(int left, int top, ElementDrawRotation rotation, ZebraFont font,
                                                int horizontalMult, int verticalMult, bool isReverse, string text, PrinterSettings settings)
        {
            return Encoding.GetEncoding(437).GetBytes(string.Format("A{0},{1},{2},{3},{4},{5},{6},\"{7}\"\n", left + settings.AlignLeft, 
                top + settings.AlignTop, (int)rotation, (char)font, horizontalMult, verticalMult, isReverse ? "R" : "N", 
                text.Replace(@"\", @"\\").Replace("\"", "\\\"")));            
        }

        public static byte[] LineWriteBlack(int left, int top, int width, int height, PrinterSettings settings)
        {
            return LineDraw("LO", left, top, width,height, settings);
        }

        public static byte[] LineWriteWhite(int left, int top, int width, int height, PrinterSettings settings)
        {
            return LineDraw("LW", left, top, width, height, settings);
        }

        public static byte[] LineWriteOR(int left, int top, int width, int height, PrinterSettings settings)
        {
            return LineDraw("LE", left, top, width, height, settings);
        }

        public static byte[] DiagonalLineWrite(int left, int top, int lineThickness, int right, int bottom, PrinterSettings settings)
        {
            return Encoding.GetEncoding(437).GetBytes(string.Format("LS{0},{1},{2},{3},{4}\n", left + settings.AlignLeft, top + settings.AlignTop, 
                lineThickness, right + settings.AlignLeft, bottom + settings.AlignTop));
        }

        public static byte[] BoxWrite(int left, int top, int lineThickness, int right, int bottom, PrinterSettings settings)
        {
            return Encoding.GetEncoding(437).GetBytes(string.Format("X{0},{1},{2},{3},{4}\n", left + settings.AlignLeft, top + settings.AlignTop, 
                lineThickness, right + settings.AlignLeft, bottom + settings.AlignTop));
        }

        private static byte[] LineDraw(string lineDrawCode, int left, int top, int width, int height, PrinterSettings settings)
        {
            return Encoding.GetEncoding(437).GetBytes(string.Format("{0}{1},{2},{3},{4}\n", lineDrawCode, left + settings.AlignLeft, 
                top + settings.AlignTop, width, height));
        }

        //Form functions are untested but should work
        public static byte[] FormDelete(string formName)
        {
            return Encoding.GetEncoding(437).GetBytes(string.Format("FK\"{0}\"\n", formName));
        }

        public static byte[] FormCreateBegin(string formName)
        {
            return Encoding.GetEncoding(437).GetBytes(string.Format("{0}FS\"{1}\"\n", FormDelete(formName), formName));
        }

        public static byte[] FormCreateFinish()
        {
            return Encoding.GetEncoding(437).GetBytes(string.Format("FE\n"));
        }

        public static byte[] CodePageSet(Codepage7 c)
        {
            return Encoding.GetEncoding(437).GetBytes(string.Format("I7,{0}\n", (int)c));
        }
        public static byte[] CodePageSet(Codepage8 codePage, Codepage8KDU country)
        {
            return Encoding.GetEncoding(437).GetBytes(string.Format("I8,{0:x},{1:000}\n", (int)codePage, country));
        }

        public static byte[] EPL_Align(PrinterSettings p)
        {
            List<byte> res = new List<byte>();
            res.AddRange(EPLCommands.LineWriteBlack(0, 20, 1, 20, p));
            res.AddRange(EPLCommands.TextWrite(5, 20, Com.SharpZebra.ElementDrawRotation.NO_ROTATION, Com.SharpZebra.ZebraFont.STANDARD_NORMAL,
                1, 1, false, "0", p));
            res.AddRange(EPLCommands.LineWriteBlack(5, 40, 1, 20, p));
            res.AddRange(EPLCommands.TextWrite(10, 40, Com.SharpZebra.ElementDrawRotation.NO_ROTATION, Com.SharpZebra.ZebraFont.STANDARD_NORMAL,
                1, 1, false, "5", p));
            res.AddRange(EPLCommands.LineWriteBlack(10, 60, 1, 20, p));
            res.AddRange(EPLCommands.TextWrite(15, 60, Com.SharpZebra.ElementDrawRotation.NO_ROTATION, Com.SharpZebra.ZebraFont.STANDARD_NORMAL,
                1, 1, false, "10", p));
            res.AddRange(EPLCommands.LineWriteBlack(15, 80, 1, 20, p));
            res.AddRange(EPLCommands.TextWrite(20, 80, Com.SharpZebra.ElementDrawRotation.NO_ROTATION, Com.SharpZebra.ZebraFont.STANDARD_NORMAL,
                1, 1, false, "15", p));
            res.AddRange(EPLCommands.LineWriteBlack(20, 100, 1, 20, p));
            res.AddRange(EPLCommands.TextWrite(25, 100, Com.SharpZebra.ElementDrawRotation.NO_ROTATION, Com.SharpZebra.ZebraFont.STANDARD_NORMAL,
                1, 1, false, "20", p));

            res.AddRange(EPLCommands.LineWriteBlack(40, 0, 20, 1, p));
            res.AddRange(EPLCommands.TextWrite(40, 5, Com.SharpZebra.ElementDrawRotation.NO_ROTATION, Com.SharpZebra.ZebraFont.STANDARD_NORMAL,
                1, 1, false, "0", p));
            res.AddRange(EPLCommands.LineWriteBlack(60, 5, 20, 1, p));
            res.AddRange(EPLCommands.TextWrite(60, 10, Com.SharpZebra.ElementDrawRotation.NO_ROTATION, Com.SharpZebra.ZebraFont.STANDARD_NORMAL,
                1, 1, false, "5", p));
            res.AddRange(EPLCommands.LineWriteBlack(80, 10, 20, 1, p));
            res.AddRange(EPLCommands.TextWrite(80, 15, Com.SharpZebra.ElementDrawRotation.NO_ROTATION, Com.SharpZebra.ZebraFont.STANDARD_NORMAL,
                1, 1, false, "10", p));
            res.AddRange(EPLCommands.LineWriteBlack(100, 15, 20, 1, p));
            res.AddRange(EPLCommands.TextWrite(100, 20, Com.SharpZebra.ElementDrawRotation.NO_ROTATION, Com.SharpZebra.ZebraFont.STANDARD_NORMAL,
                1, 1, false, "15", p));
            res.AddRange(EPLCommands.LineWriteBlack(120, 20, 20, 1, p));
            res.AddRange(EPLCommands.TextWrite(120, 25, Com.SharpZebra.ElementDrawRotation.NO_ROTATION, Com.SharpZebra.ZebraFont.STANDARD_NORMAL,
                1, 1, false, "20", p));

            res.AddRange(EPLCommands.LineWriteBlack(p.Width, 20, 1, 20, p));
            res.AddRange(EPLCommands.LineWriteBlack(p.Width - 5, 40, 1, 20, p));
            res.AddRange(EPLCommands.LineWriteBlack(p.Width - 10, 60, 1, 20, p));
            res.AddRange(EPLCommands.LineWriteBlack(p.Width - 15, 80, 1, 20, p));
            res.AddRange(EPLCommands.LineWriteBlack(p.Width - 20, 100, 1, 20, p));

            res.AddRange(EPLCommands.LineWriteBlack(40, p.Length, 20, 1, p));
            res.AddRange(EPLCommands.LineWriteBlack(60, p.Length - 5, 20, 1, p));
            res.AddRange(EPLCommands.LineWriteBlack(80, p.Length - 10, 20, 1, p));
            res.AddRange(EPLCommands.LineWriteBlack(100, p.Length - 15, 20, 1, p));
            res.AddRange(EPLCommands.LineWriteBlack(120, p.Length - 20, 20, 1, p));

            return res.ToArray();
        }
    }
}