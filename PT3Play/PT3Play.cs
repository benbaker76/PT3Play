// PT3Play by Sergey Bulba (svbulba@gmail.com)
// Modified code from ayfly (https://github.com/l29ah/ayfly)
// Port to C# by benbaker76 (https://github.com/benbaker76)

using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;

namespace PT3Play
{
    public static class PT3Play
    {
        private const int SPEC_BANDS = 42;
        public const int SPEC_HEIGHT = 48;
        private const int SPEC_RANGE = 1000;
        private const int SPEC_DECAY = 3;

        private const UInt32 SPEC_CHA_COL = 0xFFFF0000;
        private const UInt32 SPEC_CHB_COL = 0xFF00FF00;
        private const UInt32 SPEC_CHC_COL = 0xFF0000FF;
        private const UInt32 SPEC_ENV_COL = 0xFFFFFFFF;

        public const int AY_CLOCK = 1773400;    // Pitch
        public const int SAMPLE_RATE = 44100;   // Sample Rate
        public const int FRAME_RATE = 50;       // Speed

        // Table #0 of Pro Tracker 3.3x - 3.4r
        private static readonly ushort[] PT3NoteTable_PT_33_34r = { 0x0C21, 0x0B73, 0x0ACE, 0x0A33, 0x09A0, 0x0916, 0x0893, 0x0818, 0x07A4, 0x0736, 0x06CE, 0x066D, 0x0610, 0x05B9, 0x0567, 0x0519, 0x04D0, 0x048B, 0x0449, 0x040C, 0x03D2, 0x039B, 0x0367, 0x0336, 0x0308, 0x02DC, 0x02B3, 0x028C, 0x0268, 0x0245, 0x0224, 0x0206, 0x01E9, 0x01CD, 0x01B3, 0x019B, 0x0184, 0x016E, 0x0159, 0x0146, 0x0134, 0x0122, 0x0112, 0x0103, 0x00F4, 0x00E6, 0x00D9, 0x00CD, 0x00C2, 0x00B7, 0x00AC, 0x00A3, 0x009A, 0x0091, 0x0089, 0x0081, 0x007A, 0x0073, 0x006C, 0x0066, 0x0061, 0x005B, 0x0056, 0x0051, 0x004D, 0x0048, 0x0044, 0x0040, 0x003D, 0x0039, 0x0036, 0x0033, 0x0030, 0x002D, 0x002B, 0x0028, 0x0026, 0x0024, 0x0022, 0x0020, 0x001E, 0x001C, 0x001B, 0x0019, 0x0018, 0x0016, 0x0015, 0x0014, 0x0013, 0x0012, 0x0011, 0x0010, 0x000F, 0x000E, 0x000D, 0x000C };

        // {Table #0 of Pro Tracker 3.4x - 3.5x}
        private static readonly ushort[] PT3NoteTable_PT_34_35 = { 0x0C22, 0x0B73, 0x0ACF, 0x0A33, 0x09A1, 0x0917, 0x0894, 0x0819, 0x07A4, 0x0737, 0x06CF, 0x066D, 0x0611, 0x05BA, 0x0567, 0x051A, 0x04D0, 0x048B, 0x044A, 0x040C, 0x03D2, 0x039B, 0x0367, 0x0337, 0x0308, 0x02DD, 0x02B4, 0x028D, 0x0268, 0x0246, 0x0225, 0x0206, 0x01E9, 0x01CE, 0x01B4, 0x019B, 0x0184, 0x016E, 0x015A, 0x0146, 0x0134, 0x0123, 0x0112, 0x0103, 0x00F5, 0x00E7, 0x00DA, 0x00CE, 0x00C2, 0x00B7, 0x00AD, 0x00A3, 0x009A, 0x0091, 0x0089, 0x0082, 0x007A, 0x0073, 0x006D, 0x0067, 0x0061, 0x005C, 0x0056, 0x0052, 0x004D, 0x0049, 0x0045, 0x0041, 0x003D, 0x003A, 0x0036, 0x0033, 0x0031, 0x002E, 0x002B, 0x0029, 0x0027, 0x0024, 0x0022, 0x0020, 0x001F, 0x001D, 0x001B, 0x001A, 0x0018, 0x0017, 0x0016, 0x0014, 0x0013, 0x0012, 0x0011, 0x0010, 0x000F, 0x000E, 0x000D, 0x000C };

        // {Table #1 of Pro Tracker 3.3x - 3.5x)}
        private static readonly ushort[] PT3NoteTable_ST = { 0x0EF8, 0x0E10, 0x0D60, 0x0C80, 0x0BD8, 0x0B28, 0x0A88, 0x09F0, 0x0960, 0x08E0, 0x0858, 0x07E0, 0x077C, 0x0708, 0x06B0, 0x0640, 0x05EC, 0x0594, 0x0544, 0x04F8, 0x04B0, 0x0470, 0x042C, 0x03FD, 0x03BE, 0x0384, 0x0358, 0x0320, 0x02F6, 0x02CA, 0x02A2, 0x027C, 0x0258, 0x0238, 0x0216, 0x01F8, 0x01DF, 0x01C2, 0x01AC, 0x0190, 0x017B, 0x0165, 0x0151, 0x013E, 0x012C, 0x011C, 0x010A, 0x00FC, 0x00EF, 0x00E1, 0x00D6, 0x00C8, 0x00BD, 0x00B2, 0x00A8, 0x009F, 0x0096, 0x008E, 0x0085, 0x007E, 0x0077, 0x0070, 0x006B, 0x0064, 0x005E, 0x0059, 0x0054, 0x004F, 0x004B, 0x0047, 0x0042, 0x003F, 0x003B, 0x0038, 0x0035, 0x0032, 0x002F, 0x002C, 0x002A, 0x0027, 0x0025, 0x0023, 0x0021, 0x001F, 0x001D, 0x001C, 0x001A, 0x0019, 0x0017, 0x0016, 0x0015, 0x0013, 0x0012, 0x0011, 0x0010, 0x000F };

        // {Table #2 of Pro Tracker 3.4r}
        private static readonly ushort[] PT3NoteTable_ASM_34r = { 0x0D3E, 0x0C80, 0x0BCC, 0x0B22, 0x0A82, 0x09EC, 0x095C, 0x08D6, 0x0858, 0x07E0, 0x076E, 0x0704, 0x069F, 0x0640, 0x05E6, 0x0591, 0x0541, 0x04F6, 0x04AE, 0x046B, 0x042C, 0x03F0, 0x03B7, 0x0382, 0x034F, 0x0320, 0x02F3, 0x02C8, 0x02A1, 0x027B, 0x0257, 0x0236, 0x0216, 0x01F8, 0x01DC, 0x01C1, 0x01A8, 0x0190, 0x0179, 0x0164, 0x0150, 0x013D, 0x012C, 0x011B, 0x010B, 0x00FC, 0x00EE, 0x00E0, 0x00D4, 0x00C8, 0x00BD, 0x00B2, 0x00A8, 0x009F, 0x0096, 0x008D, 0x0085, 0x007E, 0x0077, 0x0070, 0x006A, 0x0064, 0x005E, 0x0059, 0x0054, 0x0050, 0x004B, 0x0047, 0x0043, 0x003F, 0x003C, 0x0038, 0x0035, 0x0032, 0x002F, 0x002D, 0x002A, 0x0028, 0x0026, 0x0024, 0x0022, 0x0020, 0x001E, 0x001D, 0x001B, 0x001A, 0x0019, 0x0018, 0x0015, 0x0014, 0x0013, 0x0012, 0x0011, 0x0010, 0x000F, 0x000E };

        // {Table #2 of Pro Tracker 3.4x - 3.5x}
        private static readonly ushort[] PT3NoteTable_ASM_34_35 = { 0x0D10, 0x0C55, 0x0BA4, 0x0AFC, 0x0A5F, 0x09CA, 0x093D, 0x08B8, 0x083B, 0x07C5, 0x0755, 0x06EC, 0x0688, 0x062A, 0x05D2, 0x057E, 0x052F, 0x04E5, 0x049E, 0x045C, 0x041D, 0x03E2, 0x03AB, 0x0376, 0x0344, 0x0315, 0x02E9, 0x02BF, 0x0298, 0x0272, 0x024F, 0x022E, 0x020F, 0x01F1, 0x01D5, 0x01BB, 0x01A2, 0x018B, 0x0174, 0x0160, 0x014C, 0x0139, 0x0128, 0x0117, 0x0107, 0x00F9, 0x00EB, 0x00DD, 0x00D1, 0x00C5, 0x00BA, 0x00B0, 0x00A6, 0x009D, 0x0094, 0x008C, 0x0084, 0x007C, 0x0075, 0x006F, 0x0069, 0x0063, 0x005D, 0x0058, 0x0053, 0x004E, 0x004A, 0x0046, 0x0042, 0x003E, 0x003B, 0x0037, 0x0034, 0x0031, 0x002F, 0x002C, 0x0029, 0x0027, 0x0025, 0x0023, 0x0021, 0x001F, 0x001D, 0x001C, 0x001A, 0x0019, 0x0017, 0x0016, 0x0015, 0x0014, 0x0012, 0x0011, 0x0010, 0x000F, 0x000E, 0x000D };

        // {Table #3 of Pro Tracker 3.4r}
        private static readonly ushort[] PT3NoteTable_REAL_34r = { 0x0CDA, 0x0C22, 0x0B73, 0x0ACF, 0x0A33, 0x09A1, 0x0917, 0x0894, 0x0819, 0x07A4, 0x0737, 0x06CF, 0x066D, 0x0611, 0x05BA, 0x0567, 0x051A, 0x04D0, 0x048B, 0x044A, 0x040C, 0x03D2, 0x039B, 0x0367, 0x0337, 0x0308, 0x02DD, 0x02B4, 0x028D, 0x0268, 0x0246, 0x0225, 0x0206, 0x01E9, 0x01CE, 0x01B4, 0x019B, 0x0184, 0x016E, 0x015A, 0x0146, 0x0134, 0x0123, 0x0113, 0x0103, 0x00F5, 0x00E7, 0x00DA, 0x00CE, 0x00C2, 0x00B7, 0x00AD, 0x00A3, 0x009A, 0x0091, 0x0089, 0x0082, 0x007A, 0x0073, 0x006D, 0x0067, 0x0061, 0x005C, 0x0056, 0x0052, 0x004D, 0x0049, 0x0045, 0x0041, 0x003D, 0x003A, 0x0036, 0x0033, 0x0031, 0x002E, 0x002B, 0x0029, 0x0027, 0x0024, 0x0022, 0x0020, 0x001F, 0x001D, 0x001B, 0x001A, 0x0018, 0x0017, 0x0016, 0x0014, 0x0013, 0x0012, 0x0011, 0x0010, 0x000F, 0x000E, 0x000D };

        // {Table #3 of Pro Tracker 3.4x - 3.5x}
        private static readonly ushort[] PT3NoteTable_REAL_34_35 = { 0x0CDA, 0x0C22, 0x0B73, 0x0ACF, 0x0A33, 0x09A1, 0x0917, 0x0894, 0x0819, 0x07A4, 0x0737, 0x06CF, 0x066D, 0x0611, 0x05BA, 0x0567, 0x051A, 0x04D0, 0x048B, 0x044A, 0x040C, 0x03D2, 0x039B, 0x0367, 0x0337, 0x0308, 0x02DD, 0x02B4, 0x028D, 0x0268, 0x0246, 0x0225, 0x0206, 0x01E9, 0x01CE, 0x01B4, 0x019B, 0x0184, 0x016E, 0x015A, 0x0146, 0x0134, 0x0123, 0x0112, 0x0103, 0x00F5, 0x00E7, 0x00DA, 0x00CE, 0x00C2, 0x00B7, 0x00AD, 0x00A3, 0x009A, 0x0091, 0x0089, 0x0082, 0x007A, 0x0073, 0x006D, 0x0067, 0x0061, 0x005C, 0x0056, 0x0052, 0x004D, 0x0049, 0x0045, 0x0041, 0x003D, 0x003A, 0x0036, 0x0033, 0x0031, 0x002E, 0x002B, 0x0029, 0x0027, 0x0024, 0x0022, 0x0020, 0x001F, 0x001D, 0x001B, 0x001A, 0x0018, 0x0017, 0x0016, 0x0014, 0x0013, 0x0012, 0x0011, 0x0010, 0x000F, 0x000E, 0x000D };

        // {Volume table of Pro Tracker 3.3x - 3.4x}
        private static readonly byte[,] PT3VolumeTable_33_34 = { { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01 }, { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01, 0x01, 0x02, 0x02, 0x02, 0x02, 0x02 }, { 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01, 0x02, 0x02, 0x02, 0x02, 0x03, 0x03, 0x03, 0x03 }, { 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x02, 0x02, 0x02, 0x03, 0x03, 0x03, 0x04, 0x04, 0x04 }, { 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x02, 0x02, 0x03, 0x03, 0x03, 0x04, 0x04, 0x04, 0x05, 0x05 }, { 0x00, 0x00, 0x00, 0x01, 0x01, 0x02, 0x02, 0x03, 0x03, 0x03, 0x04, 0x04, 0x05, 0x05, 0x06, 0x06 }, { 0x00, 0x00, 0x01, 0x01, 0x02, 0x02, 0x03, 0x03, 0x04, 0x04, 0x05, 0x05, 0x06, 0x06, 0x07, 0x07 }, { 0x00, 0x00, 0x01, 0x01, 0x02, 0x02, 0x03, 0x03, 0x04, 0x05, 0x05, 0x06, 0x06, 0x07, 0x07, 0x08 }, { 0x00, 0x00, 0x01, 0x01, 0x02, 0x03, 0x03, 0x04, 0x05, 0x05, 0x06, 0x06, 0x07, 0x08, 0x08, 0x09 }, { 0x00, 0x00, 0x01, 0x02, 0x02, 0x03, 0x04, 0x04, 0x05, 0x06, 0x06, 0x07, 0x08, 0x08, 0x09, 0x0A }, { 0x00, 0x00, 0x01, 0x02, 0x03, 0x03, 0x04, 0x05, 0x06, 0x06, 0x07, 0x08, 0x09, 0x09, 0x0A, 0x0B }, { 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07, 0x08, 0x08, 0x09, 0x0A, 0x0B, 0x0C }, { 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D }, { 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E }, { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F } };

        // {Volume table of Pro Tracker 3.5x}
        private static readonly byte[,] PT3VolumeTable_35 = { { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01 }, { 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x02, 0x02, 0x02, 0x02 }, { 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01, 0x01, 0x02, 0x02, 0x02, 0x02, 0x02, 0x03, 0x03, 0x03 }, { 0x00, 0x00, 0x01, 0x01, 0x01, 0x01, 0x02, 0x02, 0x02, 0x02, 0x03, 0x03, 0x03, 0x03, 0x04, 0x04 }, { 0x00, 0x00, 0x01, 0x01, 0x01, 0x02, 0x02, 0x02, 0x03, 0x03, 0x03, 0x04, 0x04, 0x04, 0x05, 0x05 }, { 0x00, 0x00, 0x01, 0x01, 0x02, 0x02, 0x02, 0x03, 0x03, 0x04, 0x04, 0x04, 0x05, 0x05, 0x06, 0x06 }, { 0x00, 0x00, 0x01, 0x01, 0x02, 0x02, 0x03, 0x03, 0x04, 0x04, 0x05, 0x05, 0x06, 0x06, 0x07, 0x07 }, { 0x00, 0x01, 0x01, 0x02, 0x02, 0x03, 0x03, 0x04, 0x04, 0x05, 0x05, 0x06, 0x06, 0x07, 0x07, 0x08 }, { 0x00, 0x01, 0x01, 0x02, 0x02, 0x03, 0x04, 0x04, 0x05, 0x05, 0x06, 0x07, 0x07, 0x08, 0x08, 0x09 }, { 0x00, 0x01, 0x01, 0x02, 0x03, 0x03, 0x04, 0x05, 0x05, 0x06, 0x07, 0x07, 0x08, 0x09, 0x09, 0x0A }, { 0x00, 0x01, 0x01, 0x02, 0x03, 0x04, 0x04, 0x05, 0x06, 0x07, 0x07, 0x08, 0x09, 0x0A, 0x0A, 0x0B }, { 0x00, 0x01, 0x02, 0x02, 0x03, 0x04, 0x05, 0x06, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0A, 0x0B, 0x0C }, { 0x00, 0x01, 0x02, 0x03, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0A, 0x0B, 0x0C, 0x0D }, { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E }, { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F } };

        private static AYSongInfo AYInfo = null;
        private static int interruptCnt;

        public static volatile Int16[] spec_levels = new Int16[PT3Play.SPEC_BANDS];
        private static volatile Int16[] spec_levels_prev = new Int16[PT3Play.SPEC_BANDS];
        public static volatile UInt32[] spec_colors = new UInt32[PT3Play.SPEC_BANDS];

        private static int output_device;

        private static volatile UInt32 sound_stereo_dac = new UInt32();

        private static byte[] sig = new byte[] { 0x50, 0x72, 0x6f, 0x54, 0x72, 0x61, 0x63, 0x6b, 0x65, 0x72, 0x20, 0x33, 0x2e };
        private static byte[] sig1 = new byte[] { 0x56, 0x6f, 0x72, 0x74, 0x65, 0x78, 0x20, 0x54, 0x72, 0x61, 0x63, 0x6b, 0x65, 0x72, 0x20, 0x49, 0x49 };

        public static void EmulateSample(out short sample_l, out short sample_r)
        {
            if (AYInfo == null)
            {
                sample_l = 0;
                sample_r = 0;
                return;
            }

            if (interruptCnt++ >= (PT3Play.SAMPLE_RATE / PT3Play.FRAME_RATE))
            {
                Spec_Update();

                Spec_Add_AY(AYInfo.Chip[0]);

                if (AYInfo.Is2AY)
                    Spec_Add_AY(AYInfo.Chip[1]);

                PT3_Play_Chip(AYInfo, 0);

                interruptCnt = 0;
            }

            AYEmu.AY_Tick(AYInfo.Chip[0], PT3Play.AY_CLOCK / PT3Play.SAMPLE_RATE / 8);

            int out_l = (short)(AYInfo.Chip[0].Out[0] + AYInfo.Chip[0].Out[1] / 2);
            int out_r = (short)(AYInfo.Chip[0].Out[2] + AYInfo.Chip[0].Out[1] / 2);

            if (AYInfo.Is2AY)
            {
                AYEmu.AY_Tick(AYInfo.Chip[1], PT3Play.AY_CLOCK / PT3Play.SAMPLE_RATE / 8);

                out_l += (short)(AYInfo.Chip[0].Out[0] + AYInfo.Chip[0].Out[1] / 2);
                out_r += (short)(AYInfo.Chip[0].Out[2] + AYInfo.Chip[0].Out[1] / 2);
            }

            if (out_l > 32767)
                out_l = 32767;
            if (out_r > 32767)
                out_r = 32767;

            Debug.Assert(out_l >= 0);
            Debug.Assert(out_r >= 0);

            sample_l = (short)out_l;
            sample_r = (short)out_r;
        }

        public static void MusicPlay(ref byte[] musicData)
        {
            AYInfo = new AYSongInfo();
            AYInfo.Song = new AYSong[2];
            AYInfo.Song[0] = new AYSong();
            AYInfo.Song[1] = new AYSong();
            AYInfo.Chip = new AYChip[2];

            AYEmu.AY_Init(ref AYInfo.Chip[0]);
            AYEmu.AY_Init(ref AYInfo.Chip[1]);

            AYInfo.Song[0].Module = musicData;
            AYInfo.ModuleLength = musicData.Length;

            PT3_Init(AYInfo);

            sound_stereo_dac = 0;
            interruptCnt = 0;
        }

        public static ushort AY_Sys_GetWord(ref byte[] data, int offset)
        {
            return (ushort)(data[offset] | (data[offset + 1] << 8));
        }

        public static void AY_ResetAY(AYSongInfo info, int chipnum)
        {
            AYEmu.AY_Init(ref info.Chip[chipnum]);
        }

        public static void AY_WriteAY(AYSongInfo info, AYRegister reg, int val, int chipnum)
        {
            AYEmu.AY_Out(info.Chip[chipnum], reg, val);
        }

        private static bool CompareArrays(byte[] array1, int startIndex, byte[] array2)
        {
            for (int i = 0; i < array2.Length; i++)
            {
                if (array1[startIndex + i] != array2[i])
                    return false;
            }
            return true;
        }

        public static int PT3_FindSig(byte[] buffer, int offset, int length)
        {
            if (length < sig.Length || length < sig1.Length)
                return 0;

            for (int i = 0; i < length - sig.Length; i++)
            {
                if (CompareArrays(buffer, offset + i, sig) || CompareArrays(buffer, offset + i, sig1))
                    return i;
            }
            return 0;
        }

        public static void PT3_Init(AYSongInfo info)
        {
            int i;
            byte b;
            PT3_File header = GetHeader(info, 0);

            int version = 6;

            if ((header.PT3_MusicName[13] >= '0') && (header.PT3_MusicName[13] <= '9'))
            {
                version = header.PT3_MusicName[13] - 0x30;
            }

            int offset = PT3_FindSig(info.Song[0].Module, 0x63, (int)(info.ModuleLength - 0x63));

            if (offset > 0)
            {
                info.Is2AY = true;
                Array.Copy(info.Song[0].Module, offset, info.Song[1].Module, 0, info.Song[0].Module.Length - offset);
            }

            for (short y = 0; y < 2; y++)
            {
                i = header.PT3_PositionList[0];
                b = header.PT3_MusicName[0x62];

                if (b != 0x20)
                {
                    i = b * 3 - 3 - i;
                }

                info.Song[y].Data.PT3.Version = version;
                info.Song[y].Data.PT3.DelayCounter = 1;
                info.Song[y].Data.PT3.Delay = header.PT3_Delay;
                info.Song[y].Data.PT3_A.Address_In_Pattern = AY_Sys_GetWord(ref info.Song[y].Module, PT3_PatternsPointer(header) + i * 2);
                info.Song[y].Data.PT3_B.Address_In_Pattern = AY_Sys_GetWord(ref info.Song[y].Module, PT3_PatternsPointer(header) + i * 2 + 2);
                info.Song[y].Data.PT3_C.Address_In_Pattern = AY_Sys_GetWord(ref info.Song[y].Module, PT3_PatternsPointer(header) + i * 2 + 4);

                info.Song[y].Data.PT3_A.OrnamentPointer = PT3_OrnamentsPointers(header, 0);
                info.Song[y].Data.PT3_A.Loop_Ornament_Position = info.Song[y].Module[info.Song[y].Data.PT3_A.OrnamentPointer];
                info.Song[y].Data.PT3_A.OrnamentPointer++;
                info.Song[y].Data.PT3_A.Ornament_Length = info.Song[y].Module[info.Song[y].Data.PT3_A.OrnamentPointer];
                info.Song[y].Data.PT3_A.OrnamentPointer++;
                info.Song[y].Data.PT3_A.SamplePointer = PT3_SamplesPointers(header, 1);
                info.Song[y].Data.PT3_A.Loop_Sample_Position = info.Song[y].Module[info.Song[y].Data.PT3_A.SamplePointer];
                info.Song[y].Data.PT3_A.SamplePointer++;
                info.Song[y].Data.PT3_A.Sample_Length = info.Song[y].Module[info.Song[y].Data.PT3_A.SamplePointer];
                info.Song[y].Data.PT3_A.SamplePointer++;
                info.Song[y].Data.PT3_A.Volume = 15;
                info.Song[y].Data.PT3_A.Note_Skip_Counter = 1;

                info.Song[y].Data.PT3_B.OrnamentPointer = info.Song[y].Data.PT3_A.OrnamentPointer;
                info.Song[y].Data.PT3_B.Loop_Ornament_Position = info.Song[y].Data.PT3_A.Loop_Ornament_Position;
                info.Song[y].Data.PT3_B.Ornament_Length = info.Song[y].Data.PT3_A.Ornament_Length;
                info.Song[y].Data.PT3_B.SamplePointer = info.Song[y].Data.PT3_A.SamplePointer;
                info.Song[y].Data.PT3_B.Loop_Sample_Position = info.Song[y].Data.PT3_A.Loop_Sample_Position;
                info.Song[y].Data.PT3_B.Sample_Length = info.Song[y].Data.PT3_A.Sample_Length;
                info.Song[y].Data.PT3_B.Volume = 15;
                info.Song[y].Data.PT3_B.Note_Skip_Counter = 1;

                info.Song[y].Data.PT3_C.OrnamentPointer = info.Song[y].Data.PT3_A.OrnamentPointer;
                info.Song[y].Data.PT3_C.Loop_Ornament_Position = info.Song[y].Data.PT3_A.Loop_Ornament_Position;
                info.Song[y].Data.PT3_C.Ornament_Length = info.Song[y].Data.PT3_A.Ornament_Length;
                info.Song[y].Data.PT3_C.SamplePointer = info.Song[y].Data.PT3_A.SamplePointer;
                info.Song[y].Data.PT3_C.Loop_Sample_Position = info.Song[y].Data.PT3_A.Loop_Sample_Position;
                info.Song[y].Data.PT3_C.Sample_Length = info.Song[y].Data.PT3_A.Sample_Length;
                info.Song[y].Data.PT3_C.Volume = 15;
                info.Song[y].Data.PT3_C.Note_Skip_Counter = 1;

                if (!info.Is2AY)
                    break;

                header = GetHeader(info, y);
            }

            AY_ResetAY(info, 0);
            AY_ResetAY(info, 1);
        }

        public static short PT3_GetNoteFreq(AYSongInfo info, byte j, short chip_num)
        {
            PT3_File header = GetHeader(info, chip_num);

            switch (header.PT3_TonTableId)
            {
                case 0:
                    if (info.Song[chip_num].Data.PT3.Version <= 3)
                        return (short)PT3NoteTable_PT_33_34r[j];
                    else
                        return (short)PT3NoteTable_PT_34_35[j];
                case 1:
                    return (short)PT3NoteTable_ST[j];
                case 2:
                    if (info.Song[chip_num].Data.PT3.Version <= 3)
                        return (short)PT3NoteTable_ASM_34r[j];
                    else
                        return (short)PT3NoteTable_ASM_34_35[j];
                default:
                    if (info.Song[chip_num].Data.PT3.Version <= 3)
                        return (short)PT3NoteTable_REAL_34r[j];
                    else
                        return (short)PT3NoteTable_REAL_34_35[j];
            }
        }

        public static void PT3_PatternInterpreter(AYSongInfo info, ref PT3_Channel_Parameters chan, short chip_num)
        {
            PT3_File header = GetHeader(info, chip_num);
            bool quit;
            byte flag9;
            byte flag8;
            byte flag5;
            byte flag4;
            byte flag3;
            byte flag2;
            byte flag1;
            byte counter;
            byte prnote;
            short prsliding;
            prnote = chan.Note;
            prsliding = chan.Current_Ton_Sliding;
            quit = false;
            counter = 0;
            flag9 = flag8 = flag5 = flag4 = flag3 = flag2 = flag1 = 0;
            do
            {
                byte val = info.Song[chip_num].Module[chan.Address_In_Pattern];
                if (val >= 0xf0)
                {
                    chan.OrnamentPointer = PT3_OrnamentsPointers(header, val - 0xf0);
                    chan.Loop_Ornament_Position = info.Song[chip_num].Module[chan.OrnamentPointer];
                    chan.OrnamentPointer++;
                    chan.Ornament_Length = info.Song[chip_num].Module[chan.OrnamentPointer];
                    chan.OrnamentPointer++;
                    chan.Address_In_Pattern++;
                    chan.SamplePointer = PT3_SamplesPointers(header, (info.Song[chip_num].Module[chan.Address_In_Pattern] / 2));
                    chan.Loop_Sample_Position = info.Song[chip_num].Module[chan.SamplePointer];
                    chan.SamplePointer++;
                    chan.Sample_Length = info.Song[chip_num].Module[chan.SamplePointer];
                    chan.SamplePointer++;
                    chan.Envelope_Enabled = false;
                    chan.Position_In_Ornament = 0;
                }
                else if (val >= 0xd1 && val <= 0xef)
                {
                    chan.SamplePointer = PT3_SamplesPointers(header, (val - 0xd0));
                    chan.Loop_Sample_Position = info.Song[chip_num].Module[chan.SamplePointer];
                    chan.SamplePointer++;
                    chan.Sample_Length = info.Song[chip_num].Module[chan.SamplePointer];
                    chan.SamplePointer++;
                }
                else if (val == 0xd0)
                {
                    quit = true;
                }
                else if (val >= 0xc1 && val <= 0xcf)
                {
                    chan.Volume = (byte)(val - 0xc0);
                }
                else if (val == 0xc0)
                {
                    chan.Position_In_Sample = 0;
                    chan.Current_Amplitude_Sliding = 0;
                    chan.Current_Noise_Sliding = 0;
                    chan.Current_Envelope_Sliding = 0;
                    chan.Position_In_Ornament = 0;
                    chan.Ton_Slide_Count = 0;
                    chan.Current_Ton_Sliding = 0;
                    chan.Ton_Accumulator = 0;
                    chan.Current_OnOff = 0;
                    chan.Enabled = false;
                    quit = true;
                }
                else if (val >= 0xb2 && val <= 0xbf)
                {
                    chan.Envelope_Enabled = true;
                    AY_WriteAY(info, AYRegister.AY_ENV_SHAPE, val - 0xb1, chip_num);
                    chan.Address_In_Pattern++;
                    info.Song[chip_num].Data.PT3.Env_Base_hi = info.Song[chip_num].Module[chan.Address_In_Pattern];
                    chan.Address_In_Pattern++;
                    info.Song[chip_num].Data.PT3.Env_Base_lo = info.Song[chip_num].Module[chan.Address_In_Pattern];
                    chan.Position_In_Ornament = 0;
                    info.Song[chip_num].Data.PT3.Cur_Env_Slide = 0;
                    info.Song[chip_num].Data.PT3.Cur_Env_Delay = 0;
                }
                else if (val == 0xb1)
                {
                    chan.Address_In_Pattern++;
                    chan.Number_Of_Notes_To_Skip = info.Song[chip_num].Module[chan.Address_In_Pattern];
                }
                else if (val == 0xb0)
                {
                    chan.Envelope_Enabled = false;
                    chan.Position_In_Ornament = 0;
                }
                else if (val >= 0x50 && val <= 0xaf)
                {
                    chan.Note = (byte)(val - 0x50);
                    chan.Position_In_Sample = 0;
                    chan.Current_Amplitude_Sliding = 0;
                    chan.Current_Noise_Sliding = 0;
                    chan.Current_Envelope_Sliding = 0;
                    chan.Position_In_Ornament = 0;
                    chan.Ton_Slide_Count = 0;
                    chan.Current_Ton_Sliding = 0;
                    chan.Ton_Accumulator = 0;
                    chan.Current_OnOff = 0;
                    chan.Enabled = true;
                    quit = true;
                }
                else if (val >= 0x40 && val <= 0x4f)
                {
                    chan.OrnamentPointer = PT3_OrnamentsPointers(header, val - 0x40);
                    chan.Loop_Ornament_Position = info.Song[chip_num].Module[chan.OrnamentPointer];
                    chan.OrnamentPointer++;
                    chan.Ornament_Length = info.Song[chip_num].Module[chan.OrnamentPointer];
                    chan.OrnamentPointer++;
                    chan.Position_In_Ornament = 0;
                }
                else if (val >= 0x20 && val <= 0x3f)
                {
                    info.Song[chip_num].Data.PT3.Noise_Base = (byte)(val - 0x20);
                }
                else if (val >= 0x10 && val <= 0x1f)
                {
                    if (val == 0x10)
                        chan.Envelope_Enabled = false;
                    else
                    {
                        AY_WriteAY(info, AYRegister.AY_ENV_SHAPE, val - 0x10, chip_num);
                        chan.Address_In_Pattern++;
                        info.Song[chip_num].Data.PT3.Env_Base_hi = info.Song[chip_num].Module[chan.Address_In_Pattern];
                        chan.Address_In_Pattern++;
                        info.Song[chip_num].Data.PT3.Env_Base_lo = info.Song[chip_num].Module[chan.Address_In_Pattern];
                        chan.Envelope_Enabled = true;
                        info.Song[chip_num].Data.PT3.Cur_Env_Slide = 0;
                        info.Song[chip_num].Data.PT3.Cur_Env_Delay = 0;
                    }
                    chan.Address_In_Pattern++;
                    chan.SamplePointer = PT3_SamplesPointers(header, info.Song[chip_num].Module[chan.Address_In_Pattern] / 2);
                    chan.Loop_Sample_Position = info.Song[chip_num].Module[chan.SamplePointer];
                    chan.SamplePointer++;
                    chan.Sample_Length = info.Song[chip_num].Module[chan.SamplePointer];
                    chan.SamplePointer++;
                    chan.Position_In_Ornament = 0;
                }
                else if (val == 0x9)
                {
                    counter++;
                    flag9 = counter;
                }
                else if (val == 0x8)
                {
                    counter++;
                    flag8 = counter;
                }
                else if (val == 0x5)
                {
                    counter++;
                    flag5 = counter;
                }
                else if (val == 0x4)
                {
                    counter++;
                    flag4 = counter;
                }
                else if (val == 0x3)
                {
                    counter++;
                    flag3 = counter;
                }
                else if (val == 0x2)
                {
                    counter++;
                    flag2 = counter;
                }
                else if (val == 0x1)
                {
                    counter++;
                    flag1 = counter;
                }
                chan.Address_In_Pattern++;
            }
            while (!quit);

            while (counter > 0)
            {
                if (counter == flag1)
                {
                    chan.Ton_Slide_Delay = info.Song[chip_num].Module[chan.Address_In_Pattern];
                    chan.Ton_Slide_Count = chan.Ton_Slide_Delay;
                    chan.Address_In_Pattern++;
                    chan.Ton_Slide_Step = (short)AY_Sys_GetWord(ref info.Song[chip_num].Module, chan.Address_In_Pattern);
                    chan.Address_In_Pattern += 2;
                    chan.SimpleGliss = true;
                    chan.Current_OnOff = 0;
                    if ((chan.Ton_Slide_Count == 0) && (info.Song[chip_num].Data.PT3.Version >= 7))
                        chan.Ton_Slide_Count++;
                }
                else if (counter == flag2)
                {
                    chan.SimpleGliss = false;
                    chan.Current_OnOff = 0;
                    chan.Ton_Slide_Delay = info.Song[chip_num].Module[chan.Address_In_Pattern];
                    chan.Ton_Slide_Count = chan.Ton_Slide_Delay;
                    chan.Address_In_Pattern += 3;
                    chan.Ton_Slide_Step = Math.Abs((short)AY_Sys_GetWord(ref info.Song[chip_num].Module, chan.Address_In_Pattern));
                    chan.Address_In_Pattern += 2;
                    chan.Ton_Delta = (short)(PT3_GetNoteFreq(info, chan.Note, chip_num) - PT3_GetNoteFreq(info, prnote, chip_num));
                    chan.Slide_To_Note = chan.Note;
                    chan.Note = prnote;
                    if (info.Song[chip_num].Data.PT3.Version >= 6)
                        chan.Current_Ton_Sliding = prsliding;
                    if ((chan.Ton_Delta - chan.Current_Ton_Sliding) < 0)
                        chan.Ton_Slide_Step = (short)-chan.Ton_Slide_Step;
                }
                else if (counter == flag3)
                {
                    chan.Position_In_Sample = info.Song[chip_num].Module[chan.Address_In_Pattern];
                    chan.Address_In_Pattern++;
                }
                else if (counter == flag4)
                {
                    chan.Position_In_Ornament = info.Song[chip_num].Module[chan.Address_In_Pattern];
                    chan.Address_In_Pattern++;
                }
                else if (counter == flag5)
                {
                    chan.OnOff_Delay = info.Song[chip_num].Module[chan.Address_In_Pattern];
                    chan.Address_In_Pattern++;
                    chan.OffOn_Delay = info.Song[chip_num].Module[chan.Address_In_Pattern];
                    chan.Current_OnOff = chan.OnOff_Delay;
                    chan.Address_In_Pattern++;
                    chan.Ton_Slide_Count = 0;
                    chan.Current_Ton_Sliding = 0;
                }
                else if (counter == flag8)
                {
                    info.Song[chip_num].Data.PT3.Env_Delay = (sbyte)info.Song[chip_num].Module[chan.Address_In_Pattern];
                    info.Song[chip_num].Data.PT3.Cur_Env_Delay = info.Song[chip_num].Data.PT3.Env_Delay;
                    chan.Address_In_Pattern++;
                    info.Song[chip_num].Data.PT3.Env_Slide_Add = (short)AY_Sys_GetWord(ref info.Song[chip_num].Module, chan.Address_In_Pattern);
                    chan.Address_In_Pattern += 2;
                }
                else if (counter == flag9)
                {
                    info.Song[chip_num].Data.PT3.Delay = info.Song[chip_num].Module[chan.Address_In_Pattern];
                    chan.Address_In_Pattern++;
                }
                counter--;
            }
            chan.Note_Skip_Counter = (sbyte)chan.Number_Of_Notes_To_Skip;
        }

        public static void PT3_ChangeRegisters(AYSongInfo info, ref PT3_Channel_Parameters chan, ref sbyte AddToEnv, ref byte TempMixer, short chip_num)
        {
            PT3_File header = GetHeader(info, chip_num);
            byte j;
            byte b1;
            byte b0;
            ushort w;
            if (chan.Enabled)
            {
                chan.Ton = AY_Sys_GetWord(ref info.Song[chip_num].Module, chan.SamplePointer + chan.Position_In_Sample * 4 + 2);
                chan.Ton += (ushort)chan.Ton_Accumulator;
                b0 = info.Song[chip_num].Module[chan.SamplePointer + chan.Position_In_Sample * 4];
                b1 = info.Song[chip_num].Module[chan.SamplePointer + chan.Position_In_Sample * 4 + 1];
                if ((b1 & 0x40) != 0)
                {
                    chan.Ton_Accumulator = (short)chan.Ton;
                }
                j = (byte)(chan.Note + info.Song[chip_num].Module[chan.OrnamentPointer + chan.Position_In_Ornament]);
                if ((sbyte)(j) < 0)
                    j = 0;
                else if (j > 95)
                    j = 95;
                w = (ushort)PT3_GetNoteFreq(info, j, chip_num);
                chan.Ton = (ushort)((chan.Ton + chan.Current_Ton_Sliding + w) & 0xfff);
                if (chan.Ton_Slide_Count > 0)
                {
                    chan.Ton_Slide_Count--;
                    if (chan.Ton_Slide_Count == 0)
                    {
                        chan.Current_Ton_Sliding += chan.Ton_Slide_Step;
                        chan.Ton_Slide_Count = chan.Ton_Slide_Delay;
                        if (!chan.SimpleGliss)
                        {
                            if (((chan.Ton_Slide_Step < 0) && (chan.Current_Ton_Sliding <= chan.Ton_Delta)) || ((chan.Ton_Slide_Step >= 0) && (chan.Current_Ton_Sliding >= chan.Ton_Delta)))
                            {
                                chan.Note = chan.Slide_To_Note;
                                chan.Ton_Slide_Count = 0;
                                chan.Current_Ton_Sliding = 0;
                            }
                        }
                    }
                }
                chan.Amplitude = (byte)(b1 & 0xf);
                if ((b0 & 0x80) != 0)
                {
                    if ((b0 & 0x40) != 0)
                    {
                        if (chan.Current_Amplitude_Sliding < 15)
                            chan.Current_Amplitude_Sliding++;
                    }
                    else if (chan.Current_Amplitude_Sliding > -15)
                    {
                        chan.Current_Amplitude_Sliding--;
                    }
                }
                chan.Amplitude += (byte)chan.Current_Amplitude_Sliding;
                if ((sbyte)(chan.Amplitude) < 0)
                    chan.Amplitude = 0;
                else if (chan.Amplitude > 15)
                    chan.Amplitude = 15;
                if (info.Song[chip_num].Data.PT3.Version <= 4)
                    chan.Amplitude = PT3VolumeTable_33_34[chan.Volume, chan.Amplitude];
                else
                    chan.Amplitude = PT3VolumeTable_35[chan.Volume, chan.Amplitude];
                if (((b0 & 1) == 0) && chan.Envelope_Enabled)
                    chan.Amplitude = (byte)(chan.Amplitude | 16);
                if ((b1 & 0x80) != 0)
                {
                    if ((b0 & 0x20) != 0)
                        j = (byte)(((b0 >> 1) | 0xf0) + chan.Current_Envelope_Sliding);
                    else
                        j = (byte)(((b0 >> 1) & 0xf) + chan.Current_Envelope_Sliding);
                    if ((b1 & 0x20) != 0)
                        chan.Current_Envelope_Sliding = j;
                    AddToEnv += (sbyte)j;
                }
                else
                {
                    info.Song[chip_num].Data.PT3.AddToNoise = (byte)((b0 >> 1) + chan.Current_Noise_Sliding);
                    if ((b1 & 0x20) != 0)
                        chan.Current_Noise_Sliding = info.Song[chip_num].Data.PT3.AddToNoise;
                }
                TempMixer = (byte)(((b1 >> 1) & 0x48) | TempMixer);
                chan.Position_In_Sample++;
                if (chan.Position_In_Sample >= chan.Sample_Length)
                    chan.Position_In_Sample = chan.Loop_Sample_Position;
                chan.Position_In_Ornament++;
                if (chan.Position_In_Ornament >= chan.Ornament_Length)
                    chan.Position_In_Ornament = chan.Loop_Ornament_Position;
            }
            else
                chan.Amplitude = 0;
            TempMixer = (byte)(TempMixer >> 1);
            if (chan.Current_OnOff > 0)
            {
                chan.Current_OnOff--;
                if (chan.Current_OnOff == 0)
                {
                    chan.Enabled = !chan.Enabled;
                    if (chan.Enabled)
                        chan.Current_OnOff = chan.OnOff_Delay;
                    else
                        chan.Current_OnOff = chan.OffOn_Delay;
                }
            }
        }

        public static void PT3_Play_Chip(AYSongInfo info, short chip_num)
        {
            PT3_File header = GetHeader(info, chip_num);
            byte TempMixer;
            sbyte AddToEnv;

            info.Song[chip_num].Data.PT3.DelayCounter--;
            if (info.Song[chip_num].Data.PT3.DelayCounter == 0)
            {
                info.Song[chip_num].Data.PT3_A.Note_Skip_Counter--;
                if (info.Song[chip_num].Data.PT3_A.Note_Skip_Counter == 0)
                {
                    if (info.Song[chip_num].Module[info.Song[chip_num].Data.PT3_A.Address_In_Pattern] == 0)
                    {
                        info.Song[chip_num].Data.PT3.CurrentPosition++;
                        if (info.Song[chip_num].Data.PT3.CurrentPosition == header.PT3_NumberOfPositions)
                            info.Song[chip_num].Data.PT3.CurrentPosition = header.PT3_LoopPosition;
                        info.Song[chip_num].Data.PT3_A.Address_In_Pattern = AY_Sys_GetWord(ref info.Song[chip_num].Module, PT3_PatternsPointer(header) + header.PT3_PositionList[info.Song[chip_num].Data.PT3.CurrentPosition] * 2);
                        info.Song[chip_num].Data.PT3_B.Address_In_Pattern = AY_Sys_GetWord(ref info.Song[chip_num].Module, PT3_PatternsPointer(header) + header.PT3_PositionList[info.Song[chip_num].Data.PT3.CurrentPosition] * 2 + 2);
                        info.Song[chip_num].Data.PT3_C.Address_In_Pattern = AY_Sys_GetWord(ref info.Song[chip_num].Module, PT3_PatternsPointer(header) + header.PT3_PositionList[info.Song[chip_num].Data.PT3.CurrentPosition] * 2 + 4);
                        info.Song[chip_num].Data.PT3.Noise_Base = 0;
                    }
                    PT3_PatternInterpreter(info, ref info.Song[chip_num].Data.PT3_A, chip_num);
                }
                info.Song[chip_num].Data.PT3_B.Note_Skip_Counter--;
                if (info.Song[chip_num].Data.PT3_B.Note_Skip_Counter == 0)
                    PT3_PatternInterpreter(info, ref info.Song[chip_num].Data.PT3_B, chip_num);
                info.Song[chip_num].Data.PT3_C.Note_Skip_Counter--;
                if (info.Song[chip_num].Data.PT3_C.Note_Skip_Counter == 0)
                    PT3_PatternInterpreter(info, ref info.Song[chip_num].Data.PT3_C, chip_num);
                info.Song[chip_num].Data.PT3.DelayCounter = info.Song[chip_num].Data.PT3.Delay;
            }

            AddToEnv = 0;
            TempMixer = 0;
            PT3_ChangeRegisters(info, ref info.Song[chip_num].Data.PT3_A, ref AddToEnv, ref TempMixer, chip_num);
            PT3_ChangeRegisters(info, ref info.Song[chip_num].Data.PT3_B, ref AddToEnv, ref TempMixer, chip_num);
            PT3_ChangeRegisters(info, ref info.Song[chip_num].Data.PT3_C, ref AddToEnv, ref TempMixer, chip_num);

            AY_WriteAY(info, AYRegister.AY_MIXER, TempMixer, chip_num);

            AY_WriteAY(info, AYRegister.AY_CHNL_A_FINE, info.Song[chip_num].Data.PT3_A.Ton & 0xff, chip_num);
            AY_WriteAY(info, AYRegister.AY_CHNL_A_COARSE, (info.Song[chip_num].Data.PT3_A.Ton >> 8) & 0xf, chip_num);
            AY_WriteAY(info, AYRegister.AY_CHNL_B_FINE, info.Song[chip_num].Data.PT3_B.Ton & 0xff, chip_num);
            AY_WriteAY(info, AYRegister.AY_CHNL_B_COARSE, (info.Song[chip_num].Data.PT3_B.Ton >> 8) & 0xf, chip_num);
            AY_WriteAY(info, AYRegister.AY_CHNL_C_FINE, info.Song[chip_num].Data.PT3_C.Ton & 0xff, chip_num);
            AY_WriteAY(info, AYRegister.AY_CHNL_C_COARSE, (info.Song[chip_num].Data.PT3_C.Ton >> 8) & 0xf, chip_num);
            AY_WriteAY(info, AYRegister.AY_CHNL_A_VOL, info.Song[chip_num].Data.PT3_A.Amplitude, chip_num);
            AY_WriteAY(info, AYRegister.AY_CHNL_B_VOL, info.Song[chip_num].Data.PT3_B.Amplitude, chip_num);
            AY_WriteAY(info, AYRegister.AY_CHNL_C_VOL, info.Song[chip_num].Data.PT3_C.Amplitude, chip_num);

            AY_WriteAY(info, AYRegister.AY_NOISE_PERIOD, (info.Song[chip_num].Data.PT3.Noise_Base + info.Song[chip_num].Data.PT3.AddToNoise) & 31, chip_num);
            ushort cur_env = (ushort)(AY_Sys_GetWord(ref info.Song[chip_num].Module, info.Song[chip_num].Data.PT3.Env_Base_lo) + AddToEnv + info.Song[chip_num].Data.PT3.Cur_Env_Slide);
            AY_WriteAY(info, AYRegister.AY_ENV_FINE, cur_env & 0xff, chip_num);
            AY_WriteAY(info, AYRegister.AY_ENV_COARSE, (cur_env >> 8) & 0xff, chip_num);

            if (info.Song[chip_num].Data.PT3.Cur_Env_Delay > 0)
            {
                info.Song[chip_num].Data.PT3.Cur_Env_Delay--;
                if (info.Song[chip_num].Data.PT3.Cur_Env_Delay == 0)
                {
                    info.Song[chip_num].Data.PT3.Cur_Env_Delay = info.Song[chip_num].Data.PT3.Env_Delay;
                    info.Song[chip_num].Data.PT3.Cur_Env_Slide += info.Song[chip_num].Data.PT3.Env_Slide_Add;
                }
            }
        }

        public static void PT3_Play(AYSongInfo info)
        {
            PT3_Play_Chip(info, 0);
            if (info.Is2AY)
                PT3_Play_Chip(info, 1);
        }

        public static void Spec_Add(int hz, int level, UInt32 color)
        {
            int off;
            int[] curve = { PT3Play.SPEC_HEIGHT / 10, PT3Play.SPEC_HEIGHT / 5, PT3Play.SPEC_HEIGHT / 2, PT3Play.SPEC_HEIGHT / 5, PT3Play.SPEC_HEIGHT / 10 };

            if (hz == 0)
                return;

            off = hz / (PT3Play.SPEC_RANGE / PT3Play.SPEC_BANDS) - 2;

            if (off > PT3Play.SPEC_BANDS - 1)
                off = PT3Play.SPEC_BANDS - 1;

            for (int i = 0; i < 5; ++i)
            {
                if (off >= 0 && off < PT3Play.SPEC_BANDS)
                {
                    spec_levels[off] += (short)(curve[i] * level / 16);

                    if (spec_levels[off] > PT3Play.SPEC_HEIGHT)
                        spec_levels[off] = PT3Play.SPEC_HEIGHT;

                    spec_colors[off] = color;
                }

                ++off;
            }
        }

        public static void Spec_Update()
        {
            for (int i = 0; i < PT3Play.SPEC_BANDS; ++i)
            {
                spec_levels[i] -= PT3Play.SPEC_DECAY;
                if (spec_levels[i] < 0)
                    spec_levels[i] = 0;
            }
        }

        public static void Spec_Add_AY(AYChip chip)
        {
            int period;

            if ((chip.Reg[7] & 0x01) == 0 && (chip.Reg[8] & 0x10) == 0)
            {
                period = chip.Reg[0] | (chip.Reg[1] << 8);
                if (period != 0)
                    Spec_Add(PT3Play.AY_CLOCK / 16 / period, chip.Reg[8], PT3Play.SPEC_CHA_COL);
            }
            if ((chip.Reg[7] & 0x02) == 0 && (chip.Reg[9] & 0x10) == 0)
            {
                period = chip.Reg[2] | (chip.Reg[3] << 8);
                if (period != 0)
                    Spec_Add(PT3Play.AY_CLOCK / 16 / period, chip.Reg[9], PT3Play.SPEC_CHB_COL);
            }
            if ((chip.Reg[7] & 0x04) == 0 && (chip.Reg[10] & 0x10) == 0)
            {
                period = chip.Reg[4] | (chip.Reg[5] << 8);
                if (period != 0)
                    Spec_Add(PT3Play.AY_CLOCK / 16 / period, chip.Reg[10], PT3Play.SPEC_CHC_COL);
            }
            if ((chip.Reg[8] & 0x10) != 0 || (chip.Reg[9] & 0x10) != 0 || (chip.Reg[10] & 0x10) != 0)
            {
                period = chip.Reg[11] | (chip.Reg[12] << 8);
                if (period != 0)
                    Spec_Add(PT3Play.AY_CLOCK / 16 / 16 / period, 12, PT3Play.SPEC_ENV_COL);
            }
        }

        private static PT3_File GetHeader(AYSongInfo info, short chip_num)
        {
            int headerSize = Marshal.SizeOf(typeof(PT3_File));
            IntPtr headerPtr = Marshal.AllocHGlobal(headerSize);
            Marshal.Copy(info.Song[chip_num].Module, 0, headerPtr, headerSize);
            return (PT3_File)Marshal.PtrToStructure(headerPtr, typeof(PT3_File));
        }

        private static ushort PT3_PatternsPointer(PT3_File header)
        {
            return (ushort)(header.PT3_PatternsPointer0 | (header.PT3_PatternsPointer1 << 8));
        }

        private static ushort PT3_SamplesPointers(PT3_File header, int x)
        {
            return (ushort)(header.PT3_SamplesPointers0[(x) * 2] | (header.PT3_SamplesPointers0[(x) * 2 + 1] << 8));
        }

        private static ushort PT3_OrnamentsPointers(PT3_File header, int x)
        {
            return (ushort)(header.PT3_OrnamentsPointers0[(x) * 2] | (header.PT3_OrnamentsPointers0[(x) * 2 + 1] << 8));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PT3_Channel_Parameters
    {
        public ushort Address_In_Pattern;
        public ushort OrnamentPointer;
        public ushort SamplePointer;
        public ushort Ton;
        public byte Loop_Ornament_Position;
        public byte Ornament_Length;
        public byte Position_In_Ornament;
        public byte Loop_Sample_Position;
        public byte Sample_Length;
        public byte Position_In_Sample;
        public byte Volume;
        public byte Number_Of_Notes_To_Skip;
        public byte Note;
        public byte Slide_To_Note;
        public byte Amplitude;
        public bool Envelope_Enabled;
        public bool Enabled;
        public bool SimpleGliss;
        public short Current_Amplitude_Sliding;
        public short Current_Noise_Sliding;
        public short Current_Envelope_Sliding;
        public short Ton_Slide_Count;
        public short Current_OnOff;
        public short OnOff_Delay;
        public short OffOn_Delay;
        public short Ton_Slide_Delay;
        public short Current_Ton_Sliding;
        public short Ton_Accumulator;
        public short Ton_Slide_Step;
        public short Ton_Delta;
        public sbyte Note_Skip_Counter;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PT3_Parameters
    {
        public byte Env_Base_lo;
        public byte Env_Base_hi;
        public short Cur_Env_Slide;
        public short Env_Slide_Add;
        public sbyte Cur_Env_Delay;
        public sbyte Env_Delay;
        public byte Noise_Base;
        public byte Delay;
        public byte AddToNoise;
        public byte DelayCounter;
        public byte CurrentPosition;
        public int Version;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PT3_SongInfo
    {
        public PT3_Parameters PT3;
        public PT3_Channel_Parameters PT3_A;
        public PT3_Channel_Parameters PT3_B;
        public PT3_Channel_Parameters PT3_C;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PT3_File
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x63)]
        public byte[] PT3_MusicName;
        public byte PT3_TonTableId;
        public byte PT3_Delay;
        public byte PT3_NumberOfPositions;
        public byte PT3_LoopPosition;
        public byte PT3_PatternsPointer0;
        public byte PT3_PatternsPointer1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32 * 2)]
        public byte[] PT3_SamplesPointers0;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16 * 2)]
        public byte[] PT3_OrnamentsPointers0;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128 * 2)]
        public byte[] PT3_PositionList;
    }

    public class AYSong
    {
        public byte[] Module;
        public PT3_SongInfo Data;
    }

    public class AYSongInfo
    {
        public AYSong[] Song;
        public AYChip[] Chip;
        public int ModuleLength;
        public bool Is2AY;
    }
}