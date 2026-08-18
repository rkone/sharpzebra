using System;
using System.Collections.Generic;
using System.Text;
using SharpZebra.Printing;

namespace SharpZebra.Commands
{
    public partial class EPLCommands
    {
        /// <summary>
        /// Initializes the printer: clears the image buffer and sets label length, width, print speed,
        /// darkness and the default codepage from the given settings.
        /// EPL Commands: N, O, Q, q, S, D, ZB, JF, I8.
        /// Manual: <see href="https://www.zebra.com/content/dam/zebra/manuals/printers/common/programming/epl2-pm-en.pdf"/>
        /// </summary>
        /// <param name="settings">The variable containing all required settings</param>
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
        public static byte[] ClearPrinter(PrinterSettings settings, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"\nN\nO\nQ{settings.Length + 10},{25}\nq{settings.Width + settings.AlignLeft}\nS{settings.PrintSpeed}" +
                $"\nD{settings.Darkness}\nZB\nJF\nI8,{(int)Codepage8.DOS_437:x},{(int)Codepage8KDU.USA:000}\n");
        }

        /// <summary>
        /// Instruct the Zebra printer to print labels.
        /// EPL Command: P.
        /// </summary>
        /// <param name="copies">The number of identical copies of the label to print</param>
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
        public static byte[] PrintBuffer(int copies, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"P{copies}\n");
        }

        /// <summary>
        /// Instruct the Zebra printer to print a barcode.
        /// EPL Command: B.
        /// </summary>
        /// <param name="left">Distance in dots from the left of the label</param>
        /// <param name="top">Distance in dots to the top of the label</param>
        /// <param name="height">Height in dots of the barcode</param>
        /// <param name="rotation">Rotate field.</param>
        /// <param name="barcode">Type and parameters of the barcode to print.</param>
        /// <param name="readable">Enable text to be printed at the bottom of the barcode.</param>
        /// <param name="barcodeData">Text to encode in the barcode</param>
        /// <param name="settings">Printer settings, used to apply the configured left/top alignment offsets</param>
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
        public static byte[] BarCodeWrite(int left, int top, int height, ElementDrawRotation rotation, Barcode barcode, bool readable,
            string barcodeData, PrinterSettings settings, int codepage = 437)
        {
            string encodedReadable = readable ? "B" : "N";
            return Encoding.GetEncoding(codepage).GetBytes($"B{left + settings.AlignLeft},{top + settings.AlignTop},{EPLConvert.Rotation(rotation)},{barcode.P4Value}," +
                $"{barcode.BarWidthNarrow},{barcode.BarWidthWide},{height},{encodedReadable},\"{barcodeData}\"\n");
        }

        /// <summary>
        /// Writes text using one of the printer's built-in fonts.
        /// EPL Command: A.
        /// </summary>
        /// <param name="left">Distance in dots from the left of the label</param>
        /// <param name="top">Distance in dots to the top of the label</param>
        /// <param name="rotation">Rotate field.</param>
        /// <param name="font">ZebraFont to print with. Note: these enum names do not match printer output</param>
        /// <param name="horizontalMult">Horizontal magnification of the font, 1-6 or 8</param>
        /// <param name="verticalMult">Vertical magnification of the font, 1-9</param>
        /// <param name="isReverse">Print white text on a black background</param>
        /// <param name="text">Text to be written</param>
        /// <param name="settings">Printer settings, used to apply the configured left/top alignment offsets</param>
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
        [Obsolete("Use EPLFont instead of ZebraFont.")]
        public static byte[] TextWrite(int left, int top, ElementDrawRotation rotation, ZebraFont font,
                                                int horizontalMult, int verticalMult, bool isReverse, string text, PrinterSettings settings, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"A{left + settings.AlignLeft},{top + settings.AlignTop},{EPLConvert.Rotation(rotation)},{(char)font}," +
                $"{horizontalMult},{verticalMult},{(isReverse ? 'R' : 'N')},\"{text.Replace(@"\", @"\\").Replace("\"", "\\\"")}\"\n");
        }

        /// <summary>
        /// Writes text using one of the printer's built-in fonts.
        /// EPL Command: A.
        /// </summary>
        /// <param name="left">Distance in dots from the left of the label</param>
        /// <param name="top">Distance in dots to the top of the label</param>
        /// <param name="rotation">Rotate field.</param>
        /// <param name="font">EPLFont to print with.</param>
        /// <param name="horizontalMult">Horizontal magnification of the font, 1-6 or 8</param>
        /// <param name="verticalMult">Vertical magnification of the font, 1-9</param>
        /// <param name="isReverse">Print white text on a black background</param>
        /// <param name="text">Text to be written</param>
        /// <param name="settings">Printer settings, used to apply the configured left/top alignment offsets</param>
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
        public static byte[] TextWrite(int left, int top, ElementDrawRotation rotation, EPLFont font,
            int horizontalMult, int verticalMult, bool isReverse, string text, PrinterSettings settings, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"A{left + settings.AlignLeft},{top + settings.AlignTop},{EPLConvert.Rotation(rotation)},{(char)font}," +
                $"{horizontalMult},{verticalMult},{(isReverse ? 'R' : 'N')},\"{text.Replace(@"\", @"\\").Replace("\"", "\\\"")}\"\n");
        }

        /// <summary>
        /// Draws a filled black rectangle. With a width or height of 1, draws a straight line.
        /// EPL Command: LO.
        /// </summary>
        /// <param name="left">Distance in dots from the left of the label</param>
        /// <param name="top">Distance in dots to the top of the label</param>
        /// <param name="width">Width in dots</param>
        /// <param name="height">Height in dots</param>
        /// <param name="settings">Printer settings, used to apply the configured left/top alignment offsets</param>
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
        public static byte[] LineWriteBlack(int left, int top, int width, int height, PrinterSettings settings, int codepage = 437)
        {
            return LineDraw("LO", left, top, width, height, settings, codepage);
        }

        /// <summary>
        /// Draws a filled white rectangle, erasing anything already drawn beneath it.
        /// EPL Command: LW.
        /// </summary>
        /// <param name="left">Distance in dots from the left of the label</param>
        /// <param name="top">Distance in dots to the top of the label</param>
        /// <param name="width">Width in dots</param>
        /// <param name="height">Height in dots</param>
        /// <param name="settings">Printer settings, used to apply the configured left/top alignment offsets</param>
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
        public static byte[] LineWriteWhite(int left, int top, int width, int height, PrinterSettings settings, int codepage = 437)
        {
            return LineDraw("LW", left, top, width, height, settings, codepage);
        }

        /// <summary>
        /// Draws a rectangle in exclusive-OR mode: anything black beneath it turns white and vice versa.
        /// EPL Command: LE.
        /// </summary>
        /// <param name="left">Distance in dots from the left of the label</param>
        /// <param name="top">Distance in dots to the top of the label</param>
        /// <param name="width">Width in dots</param>
        /// <param name="height">Height in dots</param>
        /// <param name="settings">Printer settings, used to apply the configured left/top alignment offsets</param>
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
        public static byte[] LineWriteOR(int left, int top, int width, int height, PrinterSettings settings, int codepage = 437)
        {
            return LineDraw("LE", left, top, width, height, settings, codepage);
        }

        /// <summary>
        /// Draws a diagonal black line between two points.
        /// EPL Command: LS.
        /// </summary>
        /// <param name="left">Distance in dots from the left of the label to the start of the line</param>
        /// <param name="top">Distance in dots from the top of the label to the start of the line</param>
        /// <param name="lineThickness">Thickness of the line in dots</param>
        /// <param name="right">Distance in dots from the left of the label to the end of the line</param>
        /// <param name="bottom">Distance in dots from the top of the label to the end of the line</param>
        /// <param name="settings">Printer settings, used to apply the configured left/top alignment offsets</param>
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
        public static byte[] DiagonalLineWrite(int left, int top, int lineThickness, int right, int bottom, PrinterSettings settings, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"LS{left + settings.AlignLeft},{top + settings.AlignTop},{lineThickness},{right + settings.AlignLeft},{bottom + settings.AlignTop}\n");
        }

        /// <summary>
        /// Draws a box outline. Note that unlike the ZPL version, the box is defined by its two corners rather than width and height.
        /// EPL Command: X.
        /// </summary>
        /// <param name="left">Distance in dots from the left of the label to the left edge of the box</param>
        /// <param name="top">Distance in dots from the top of the label to the top edge of the box</param>
        /// <param name="lineThickness">Border thickness in dots</param>
        /// <param name="right">Distance in dots from the left of the label to the right edge of the box</param>
        /// <param name="bottom">Distance in dots from the top of the label to the bottom edge of the box</param>
        /// <param name="settings">Printer settings, used to apply the configured left/top alignment offsets</param>
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
        public static byte[] BoxWrite(int left, int top, int lineThickness, int right, int bottom, PrinterSettings settings, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"X{left + settings.AlignLeft},{top + settings.AlignTop},{lineThickness},{right + settings.AlignLeft},{bottom + settings.AlignTop}\n");
        }

        private static byte[] LineDraw(string lineDrawCode, int left, int top, int width, int height, PrinterSettings settings, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"{lineDrawCode}{left + settings.AlignLeft},{top + settings.AlignTop},{width},{height}\n");
        }

        //Form functions are untested but should work
        /// <summary>
        /// Deletes a stored form from the printer.
        /// EPL Command: FK.
        /// </summary>
        /// <param name="formName">Name of the form to delete</param>
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
        public static byte[] FormDelete(string formName, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"FK\"{formName}\"\n");
        }

        /// <summary>
        /// Begins storing a form on the printer. Any existing form with the same name is deleted first.
        /// All commands sent between this and FormCreateFinish are saved as the form instead of being executed.
        /// EPL Commands: FK, FS.
        /// </summary>
        /// <param name="formName">Name to store the form under</param>
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
        public static byte[] FormCreateBegin(string formName, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"{FormDelete(formName)}FS\"{formName}\"\n");
        }

        /// <summary>
        /// Ends the form definition started with FormCreateBegin.
        /// EPL Command: FE.
        /// </summary>
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
        public static byte[] FormCreateFinish(int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes("FE\n");
        }

        /// <summary>
        /// Sets the printer's 7-bit character set.
        /// EPL Command: I7.
        /// </summary>
        /// <param name="c">The 7-bit character set to use</param>
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
        public static byte[] CodePageSet(Codepage7 c, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"I7,{(int)c}\n");
        }
        /// <summary>
        /// Sets the printer's 8-bit codepage and KDU country code.
        /// EPL Command: I8.
        /// </summary>
        /// <param name="zebraCodePage">The 8-bit codepage the printer should interpret text with</param>
        /// <param name="country">The KDU (Keyboard Display Unit) country code</param>
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
        public static byte[] CodePageSet(Codepage8 zebraCodePage, Codepage8KDU country, int codepage = 437)
        {
            return Encoding.GetEncoding(codepage).GetBytes($"I8,{(int)zebraCodePage:x},{country:000}\n");
        }

        /// <summary>
        /// Prints a test pattern of rulers along all four label edges, marked in 5-dot steps, to help
        /// calibrate the AlignLeft/AlignTop settings and the label dimensions.
        /// </summary>
        /// <param name="p">Printer settings whose alignment is being calibrated</param>
        /// <param name="codepage">The text encoding page the printer is set to use</param>
        /// <returns>Array of bytes containing EPL2 data to be sent to the Zebra printer.</returns>
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