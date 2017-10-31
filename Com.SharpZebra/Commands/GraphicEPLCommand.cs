using System.IO;
using System.Drawing;
using System.Text;
using System.Collections;
using Com.SharpZebra.Printing;
using System.Collections.Generic;

namespace Com.SharpZebra.Commands
{
    public partial class EPLCommands
    {

        public static byte[] GraphicWrite(int left, int top, string imageName, PrinterSettings settings)
        {
            return Encoding.GetEncoding(437).GetBytes(string.Format("GG{0},{1},\"{2}\"\n", left, top, imageName));
        }

        public static byte[] GraphicStore(Stream fileStream, string imageName)
        {
            BinaryReader binaryReader = new BinaryReader(fileStream);
            byte[] fileContents = binaryReader.ReadBytes((int)fileStream.Length);
            binaryReader.Close();
            List<byte> res = new List<byte>();
            res.AddRange(Encoding.GetEncoding(437).GetBytes(string.Format("GK\"{0}\"\nGM\"{0}\"{1}\n", imageName,fileContents.Length)));
            res.AddRange(fileContents);
            return res.ToArray();
        }

        public static byte[] GraphicStore(string pcxFilename, string imageName)
        {
            FileStream stream = new FileStream(pcxFilename, FileMode.Open, FileAccess.Read);
            byte[] res = GraphicStore(stream, imageName);
            stream.Close();
            return res;
        }

        public static byte[] GraphicDelete(string imageName)
        {
            return Encoding.GetEncoding(437).GetBytes(string.Format("GK\"{0}\"\n", imageName));
        }

        public static byte[] GraphicDirectWrite(int left, int top, string bitmapName, PrinterSettings settings)
        {
            Bitmap bmp = new Bitmap(bitmapName);
            List<byte> res = new List<byte>();            
            int byteWidth = bmp.Width % 8 == 0 ? bmp.Width / 8 : bmp.Width / 8 + 1;
            res.AddRange(Encoding.GetEncoding(437).GetBytes(string.Format("GW{0},{1},{2},{3},", left + settings.AlignLeft, top + settings.AlignTop, byteWidth, bmp.Height)));
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < byteWidth; x++)
                {
                    BitArray ba = new BitArray(8);
                    int scanx = x * 8;
                    for (int k = 7; k >= 0; k--)
                    {
                        if (scanx >= bmp.Width)
                            ba[k] = true;
                        else
                            ba[k] = bmp.GetPixel(scanx, y).R > 128;
                        scanx++;
                    }
                    res.Add(ConvertToByte(ba));
                }
            }
            return res.ToArray();
        }        

    }
}
