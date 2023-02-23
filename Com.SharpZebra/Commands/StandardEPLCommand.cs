using System;
using System.Collections.Generic;
using System.Text;
using SharpZebra.Printing;

namespace SharpZebra.Commands
{
    public partial class EPLCommands
    {
        public static byte[] ClearPrinter(PrinterSettings settings, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"\nN\nO\nQ{settings.Length + 10},{25}\nq{settings.Width + settings.AlignLeft}\nS{settings.PrintSpeed}" +
                $"\nD{settings.Darkness}\nZB\nJF\nI8,{(int)Codepage8.DOS_437:x},{(int)Codepage8KDU.USA:000}\n");
        }

        public static byte[] PrintBuffer(int copies, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"P{copies}\n");
        }

        public static byte[] BarCodeWrite(int left, int top, int height, ElementDrawRotation rotation, Barcode barcode, bool readable,
            string barcodeData, PrinterSettings settings, int codepage = 437)
        {
            string encodedReadable = readable ? "B" : "N";
            return Encoding.GetEncoding(codepage).GetBytes($"B{left + settings.AlignLeft},{top + settings.AlignTop},{EPLConvert.Rotation(rotation)},{barcode.P4Value}," +
                $"{barcode.BarWidthNarrow},{barcode.BarWidthWide},{height},{encodedReadable},\"{barcodeData}\"\n");
        }

        [Obsolete("Use EPLFont instead of ZebraFont.")]
        public static byte[] TextWrite(int left, int top, ElementDrawRotation rotation, ZebraFont font,
                                                int horizontalMult, int verticalMult, bool isReverse, string text, PrinterSettings settings, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"A{left + settings.AlignLeft},{top + settings.AlignTop},{EPLConvert.Rotation(rotation)},{(char)font}," +
                $"{horizontalMult},{verticalMult},{(isReverse ? 'R' : 'N')},\"{text.Replace(@"\", @"\\").Replace("\"", "\\\"")}\"\n");
        }

        public static byte[] TextWrite(int left, int top, ElementDrawRotation rotation, EPLFont font,
            int horizontalMult, int verticalMult, bool isReverse, string text, PrinterSettings settings, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"A{left + settings.AlignLeft},{top + settings.AlignTop},{EPLConvert.Rotation(rotation)},{(char)font}," +
                $"{horizontalMult},{verticalMult},{(isReverse ? 'R' : 'N')},\"{text.Replace(@"\", @"\\").Replace("\"", "\\\"")}\"\n");
        }

        public static byte[] LineWriteBlack(int left, int top, int width, int height, PrinterSettings settings, int codepage = 437)
        {
            return LineDraw("LO", left, top, width, height, settings, codepage);
        }

        public static byte[] LineWriteWhite(int left, int top, int width, int height, PrinterSettings settings, int codepage = 437)
        {
            return LineDraw("LW", left, top, width, height, settings, codepage);
        }

        public static byte[] LineWriteOR(int left, int top, int width, int height, PrinterSettings settings, int codepage = 437)
        {
            return LineDraw("LE", left, top, width, height, settings, codepage);
        }

        public static byte[] DiagonalLineWrite(int left, int top, int lineThickness, int right, int bottom, PrinterSettings settings, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"LS{left + settings.AlignLeft},{top + settings.AlignTop},{lineThickness},{right + settings.AlignLeft},{bottom + settings.AlignTop}\n");
        }

        public static byte[] BoxWrite(int left, int top, int lineThickness, int right, int bottom, PrinterSettings settings, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"X{left + settings.AlignLeft},{top + settings.AlignTop},{lineThickness},{right + settings.AlignLeft},{bottom + settings.AlignTop}\n");
        }

        private static byte[] LineDraw(string lineDrawCode, int left, int top, int width, int height, PrinterSettings settings, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"{lineDrawCode}{left + settings.AlignLeft},{top + settings.AlignTop},{width},{height}\n");
        }

        //Form functions are untested but should work
        public static byte[] FormDelete(string formName, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"FK\"{formName}\"\n");
        }

        public static byte[] FormCreateBegin(string formName, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"{FormDelete(formName)}FS\"{formName}\"\n");
        }

        public static byte[] FormCreateFinish(int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes("FE\n");
        }

        public static byte[] CodePageSet(Codepage7 c, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"I7,{(int)c}\n");
        }
        public static byte[] CodePageSet(Codepage8 zebraCodePage, Codepage8KDU country, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"I8,{(int)zebraCodePage:x},{country:000}\n");
        }

        public static byte[] EPLAlign(PrinterSettings p, int codepage = 437)
        {
            var res = new List<byte>();
            res.AddRange(LineWriteBlack(0, 20, 1, 20, p, codepage));
            res.AddRange(TextWrite(5, 20, ElementDrawRotation.NO_ROTATION, EPLFont.STANDARD_NORMAL, 1, 1, false, "0", p, codepage));
            res.AddRange(LineWriteBlack(5, 40, 1, 20, p, codepage));
            res.AddRange(TextWrite(10, 40, ElementDrawRotation.NO_ROTATION, EPLFont.STANDARD_NORMAL, 1, 1, false, "5", p, codepage));
            res.AddRange(LineWriteBlack(10, 60, 1, 20, p, codepage));
            res.AddRange(TextWrite(15, 60, ElementDrawRotation.NO_ROTATION, EPLFont.STANDARD_NORMAL, 1, 1, false, "10", p, codepage));
            res.AddRange(LineWriteBlack(15, 80, 1, 20, p, codepage));
            res.AddRange(TextWrite(20, 80, ElementDrawRotation.NO_ROTATION, EPLFont.STANDARD_NORMAL, 1, 1, false, "15", p, codepage));
            res.AddRange(LineWriteBlack(20, 100, 1, 20, p, codepage));
            res.AddRange(TextWrite(25, 100, ElementDrawRotation.NO_ROTATION, EPLFont.STANDARD_NORMAL, 1, 1, false, "20", p, codepage));

            res.AddRange(LineWriteBlack(40, 0, 20, 1, p, codepage));
            res.AddRange(TextWrite(40, 5, ElementDrawRotation.NO_ROTATION, EPLFont.STANDARD_NORMAL, 1, 1, false, "0", p, codepage));
            res.AddRange(LineWriteBlack(60, 5, 20, 1, p, codepage));
            res.AddRange(TextWrite(60, 10, ElementDrawRotation.NO_ROTATION, EPLFont.STANDARD_NORMAL, 1, 1, false, "5", p, codepage));
            res.AddRange(LineWriteBlack(80, 10, 20, 1, p, codepage));
            res.AddRange(TextWrite(80, 15, ElementDrawRotation.NO_ROTATION, EPLFont.STANDARD_NORMAL, 1, 1, false, "10", p, codepage));
            res.AddRange(LineWriteBlack(100, 15, 20, 1, p, codepage));
            res.AddRange(TextWrite(100, 20, ElementDrawRotation.NO_ROTATION, EPLFont.STANDARD_NORMAL, 1, 1, false, "15", p, codepage));
            res.AddRange(LineWriteBlack(120, 20, 20, 1, p, codepage));
            res.AddRange(TextWrite(120, 25, ElementDrawRotation.NO_ROTATION, EPLFont.STANDARD_NORMAL, 1, 1, false, "20", p, codepage));

            res.AddRange(LineWriteBlack(p.Width, 20, 1, 20, p, codepage));
            res.AddRange(LineWriteBlack(p.Width - 5, 40, 1, 20, p, codepage));
            res.AddRange(LineWriteBlack(p.Width - 10, 60, 1, 20, p, codepage));
            res.AddRange(LineWriteBlack(p.Width - 15, 80, 1, 20, p, codepage));
            res.AddRange(LineWriteBlack(p.Width - 20, 100, 1, 20, p, codepage));

            res.AddRange(LineWriteBlack(40, p.Length, 20, 1, p, codepage));
            res.AddRange(LineWriteBlack(60, p.Length - 5, 20, 1, p, codepage));
            res.AddRange(LineWriteBlack(80, p.Length - 10, 20, 1, p, codepage));
            res.AddRange(LineWriteBlack(100, p.Length - 15, 20, 1, p, codepage));
            res.AddRange(LineWriteBlack(120, p.Length - 20, 20, 1, p, codepage));

            return res.ToArray();
        }
    }
}