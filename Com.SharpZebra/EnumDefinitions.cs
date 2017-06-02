using System;
namespace Com.SharpZebra
{

    public enum Alignment
    {
        LEFT = 'L',
        CENTER = 'C',
        RIGHT = 'R',
        JUSTIFIED = 'J'
    }

    public enum ElementDrawRotation
    {
        NO_ROTATION = 'N',
        ROTATE_90_DEGREES = 'R',
        ROTATE_180_DEGREES = 'I',
        ROTATE_270_DEGREES = 'B'
    }

    public enum ElementUploadRotation
    {
        NO_ROTATION = 0,
        ROTATE_90_DEGREES,
        BOTH_ROTATIONS
    }

    public enum FontCharsetType
    {
        NUMERIC = 0,
        STANDARD,
        LOWERCASE,
        UPPERCASE,
        LOWERCASE_NUMERIC,
        UPPERCASE_NUMERIC,
        EXTENDED,
        EMSIZE  //emsize is width of the an upper-case M in points.  1 point is 1.3333 pixels.  WPF assumes 96 pixels/inch. Confused yet?
    }

    public class FontCharset
    {
        public static string[] CharList = {"0123456789.- ",
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~",
            "abcdefghijklmnopqrstuvwxyz ",
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ ",
            "abcdefghijklmnopqrstuvwxyz 0123456789.-",
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789.-",
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" + 
            "ÇüéâäàåçêëèïîìÄÅÉæÆôöòûùÿÖÜ¢£¥₧ƒáíóúñÑªº¿⌐¬½¼¡«»αßΓπΣσµτΦΘΩδ∞φε∩≡±≥≤⌠⌡÷≈°∙·√ⁿ² ",
            "M"};
    }

    public enum ZebraFont
    {
        STANDARD_SMALLEST = 48,
        STANDARD_SMALL,
        STANDARD_NORMAL,
        STANDARD_LARGE,
        STANDARD_LARGEST,
        CUSTOM_A = 97,
        CUSTOM_B,
        CUSTOM_C,
        CUSTOM_D,
        CUSTOM_E,
        CUSTOM_F,
        CUSTOM_G,
        CUSTOM_H,
        CUSTOM_I,
        CUSTOM_J,
        CUSTOM_K,
        CUSTOM_L,
        CUSTOM_M,
        CUSTOM_N,
        CUSTOM_O,
        CUSTOM_P,
        CUSTOM_Q,
        CUSTOM_R,
        CUSTOM_S,
        CUSTOM_T,
        CUSTOM_U,
        CUSTOM_V,
        CUSTOM_W,
        CUSTOM_X,
        CUSTOM_Y,
        CUSTOM_Z,
        CUSTOM_ALL = 42
    }

    public enum BarcodeType
    {
        CODE39_STD_EXT = 0,
        CODE39_CHECK,
        CODE93,
        CODE128_UCC,
        CODE128_AUTO,
        CODE128_A,
        CODE128_B,
        CODE128_C,
        CODABAR,
        EAN8,
        EAN8_2DIGIT_ADDON,
        EAN8_5DIGIT_ADDON,
        EAN13,
        EAN13_2DIGIT_ADDON,
        EAN13_5DIGIT_ADDON,
        GERMAN_POST_CODE,
        INTERLEAVED_2OF5,
        INTERLEAVED_2OF5_CHECK_MOD10,
        INTERLEAVED_2OF5_CHECK_READABLE,
        POSTNET,
        POSTNET_JAPANESE,
        UCC_EAN128,
        UPC_A,
        UPC_A_2DIGIT_ADDON,
        UPC_A_5DIGIT_ADDON,
        UPC_E,
        UPC_E_2DIGIT_ADDON,
        UPC_E_5DIGIT_ADDON,
        UPC_INTERLEAVED_2OF5,
        PLESSEY_CHECK,
        MSI_3_CHECK
    }

    public enum Codepage8
    {
        DOS_437 = 0,
        DOS_850,
        DOS_852,
        DOS_860,
        DOS_863,
        DOS_865,
        DOS_857,
        DOS_861,
        DOS_862,
        DOS_855,
        DOS_866,
        DOS_737,
        DOS_851,
        DOS_869,
        Windows_1252,
        Windows_1250,
        Windows_1251,
        Windows_1253,
        Windows_1254,
        Windows_1255
    }

    public enum Codepage7
    {
        USA = 0,
        British,
        German,
        French,
        Danish,
        Italian,
        Spanish,
        Swedish,
        Swiss
    }

    public enum Codepage8KDU
    {
        USA = 1,
        Canada = 2,
        Belguim = 32,
        Denmark = 45,
        Finland = 358,
        France = 33,
        Germany = 49,
        Netherlands = 31,
        Italy = 39,
        Latin_America = 3,
        Noray = 47,
        Portugal = 351,
        South_Africa = 27,
        Spain = 34,
        Sweden = 46,
        Swizerland = 41,
        UK = 44
    }

    public class Barcode
    {
        private BarcodeType type;
        public BarcodeType Type 
        {
            get {return type;}
            set
            {
                type = value; 
                P4Value = P4ValueList[(int)type];
                BarWidthNarrowMin = P5MinList[(int)type];
                BarWidthNarrowMax = P5MaxList[(int)type];
                if (!P5MinList[(int)type].Equals(null))
                {
                    barWidthNarrow = P5MinList[(int)type] == 1 ? 2 : P5MinList[(int)type];
                    barWidthWide = 4;
                }
                else
                {
                    barWidthNarrow = null;
                    barWidthWide = 4;
                }
            }
        }
        public string P4Value { get; private set; }
        public int? BarWidthNarrowMin { get; private set; }
        public int? BarWidthNarrowMax { get; private set; }
        private int? barWidthNarrow;
        public int? BarWidthNarrow
        {
            get { return barWidthNarrow; }
            set
            {
                if (P5MinList[(int)type].Equals(null))
                    barWidthNarrow = null;
                else
                {
                    if (value < P5MinList[(int)type] || value > P5MaxList[(int)type])
                        throw new ApplicationException("Invalid barcode narrow bar width setting");
                    else
                        barWidthNarrow = value;
                }
            }
        }
        private int barWidthWide;
        public int BarWidthWide
        {
            get { return barWidthWide; }
            set
            {
                if (value < 2 || value > 30)
                    throw new ApplicationException("Invalid barcode wide bar width setting");
                else
                    barWidthWide = value;
            }
        }

        private readonly string[] P4ValueList = {"3", "3C", "9", "0", "1", "1A", "1B", "1C", "K", "E80", "E82",
                                           "E85", "E30", "E32", "E35", "2G", "2", "2C", "2D", "P", "J",
                                           "1E", "UA0", "UA2", "UA5", "UE0", "UE2", "UE5", "2U", "L",
                                           "M"};
        private readonly int?[] P5MinList = {1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 3, 1, 1, 1, null, null,
                                                1, 2, 2, 2, 2, 2, 2, 1, null, null};
        private readonly int?[] P5MaxList = {10, 10, 10, 10, 10, 10, 10, 10, 10, 4, 4, 4, 4, 4, 4, 4, 10, 10, 10,
                                                null, null, 10, 4, 4, 4, 4, 4, 4, 10, null, null};

    }
}
