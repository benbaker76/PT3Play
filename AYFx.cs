// AYFx by Shiru (http://shiru.untergrund.net)
// Port to C# by benbaker76 (https://github.com/benbaker76)

using SlimDX.Direct3D9;
using SlimDX.XACT3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PT3Play
{
    public static class AYFx
    {
        private const int MAX_FX_LEN = 0x100;
        private const int MAX_FX_ALL = 0x20;

        private const int SFX_CHANNEL_COUNT = 2;

        private static int interruptCnt;

        private static AyfxBankCell[] AyBank;

        public static int AllEffect;

        public static AyfxChannel[] AyChannels;

        static AYFx()
        {
            AyBank = new AyfxBankCell[MAX_FX_ALL];

            for (int i = 0; i < MAX_FX_ALL; i++)
            {
                AyBank[i] = new AyfxBankCell();

                AyBank[i].AyEffect = new AyfxCell[MAX_FX_LEN];

                for (int j = 0; j < MAX_FX_LEN; j++)
                {
                    AyBank[i].AyEffect[j] = new AyfxCell();
                }
            }

            AyChannels = new AyfxChannel[SFX_CHANNEL_COUNT];

            for (int i = 0; i < SFX_CHANNEL_COUNT; i++)
            {
                AyChannels[i] = new AyfxChannel();

                AYEmu.AY_Init(ref AyChannels[i].Chip);
            }
        }

        public static void EmulateSample(out short sample_l, out short sample_r)
        {
            int out_l = 0;
            int out_r = 0;

            for (int i = 0; i < SFX_CHANNEL_COUNT; i++)
            {
                int channelOut = SFX_UpdateChannel(AyChannels[i]);

                out_l += channelOut;
                out_r += channelOut;
            }

            out_l /= SFX_CHANNEL_COUNT;
            out_r /= SFX_CHANNEL_COUNT;

            if (out_l > 32767)
                out_l = 32767;
            if (out_r > 32767)
                out_r = 32767;

            Debug.Assert(out_l >= 0);
            Debug.Assert(out_r >= 0);

            sample_l = (short)out_l;
            sample_r = (short)out_r;
        }

        public static int SFX_RealLen(int fxn)
        {
            int len = 0;
            for (int aa = MAX_FX_LEN - 1; aa >= 0; aa--)
            {
                if (AyBank[fxn].AyEffect[aa].Volume > 0)
                {
                    len = aa + 1;
                    break;
                }
            }
            return len;
        }

        public static void SFX_DefaultStr(ref AyfxCell cell)
        {
            cell.Tone = 0;
            cell.Noise = 0;
            cell.Volume = 0;
            cell.T = false;
            cell.N = false;
            cell.Sel = false;
        }

        public static void SFX_Init(int fxn)
        {
            for (int i = 0; i < MAX_FX_LEN; i++)
                SFX_DefaultStr(ref AyBank[fxn].AyEffect[i]);
        }

        public static int SFX_Decode(int fxn, byte[] buf, int off, int size)
        {
            AyfxCell[] afx = AyBank[fxn].AyEffect;

            SFX_Init(fxn);

            int pd = 0;
            int pp = 0;
            int tone = 0;
            int noise = 0;

            while (pp < size)
            {
                int it = buf[off + pp++];

                if ((it & (1 << 5)) != 0)
                {
                    tone = RdWordLH(buf, off + pp) & 0xfff;
                    pp += 2;
                }
                if ((it & (1 << 6)) != 0)
                {
                    noise = buf[off + pp++];

                    if (it == 0xd0 && noise >= 0x20)
                        break;

                    noise &= 0x1f;
                }

                afx[pd].Tone = tone;
                afx[pd].Noise = noise;
                afx[pd].Volume = it & 0x0f;
                afx[pd].T = (it & (1 << 4)) == 0;
                afx[pd].N = (it & (1 << 7)) == 0;
                pd++;
            }

            return pp;
        }

        public static bool SFX_Load(int fxn, string filename)
        {
            if (!File.Exists(filename)) return false;

            byte[] buf = File.ReadAllBytes(filename);
            int size = buf.Length;

            SFX_Decode(fxn, buf, 0, size);

            AyBank[fxn].Name = Path.GetFileName(filename);

            return true;
        }

        public static int SFX_Encode(int fxn, byte[] buf)
        {
            int fx_len = SFX_RealLen(fxn);
            int tone = -1;
            int noise = -1;
            int pp = 0;
            AyfxCell[] afx = AyBank[fxn].AyEffect;

            for (int aa = 0; aa < fx_len; aa++)
            {
                int it = (afx[aa].Volume & 0x0f);
                it |= (afx[aa].T ? 0 : (1 << 4));
                it |= (afx[aa].N ? 0 : (1 << 7));
                if (afx[aa].Tone != tone)
                {
                    tone = afx[aa].Tone;
                    it |= (1 << 5);
                }
                if (afx[aa].Noise != noise)
                {
                    noise = afx[aa].Noise;
                    it |= (1 << 6);
                }
                buf[pp++] = (byte)it;
                if ((it & (1 << 5)) != 0)
                {
                    buf[pp++] = (byte)(tone & 0xff);
                    buf[pp++] = (byte)((tone >> 8) & 0xff);
                }
                if ((it & (1 << 6)) != 0)
                {
                    buf[pp++] = (byte)(noise & 0x1f);
                }
            }

            buf[pp++] = 0xd0; // end marker
            buf[pp++] = 0x20;

            return pp;
        }

        public static short SFX_UpdateChannel(AyfxChannel channel)
        {
            if (channel.FxNum == -1)
                return 0;

            int fxNum = channel.FxNum;
            int frames = channel.Length - channel.Offset;
            int ifrq = PT3Play.SAMPLE_RATE / 50;
            int slen = ifrq * frames;

            AYEmu.AY_Tick(channel.Chip, PT3Play.AY_CLOCK / PT3Play.SAMPLE_RATE / 8);

            if (channel.Icnt++ >= ifrq)
            {
                channel.Icnt = 0;

                if (channel.Offset < MAX_FX_LEN)
                {
                    AYEmu.AY_Out(channel.Chip, AYRegister.AY_CHNL_A_FINE, (byte)(AyBank[fxNum].AyEffect[channel.Offset].Tone & 0xFF));
                    AYEmu.AY_Out(channel.Chip, AYRegister.AY_CHNL_A_COARSE, (byte)(AyBank[fxNum].AyEffect[channel.Offset].Tone >> 8));
                    AYEmu.AY_Out(channel.Chip, AYRegister.AY_NOISE_PERIOD, (byte)(AyBank[fxNum].AyEffect[channel.Offset].Noise));
                    AYEmu.AY_Out(channel.Chip, AYRegister.AY_MIXER, (byte)(0xF6 | (AyBank[fxNum].AyEffect[channel.Offset].T ? 0 : 1) | (AyBank[fxNum].AyEffect[channel.Offset].N ? 0 : 8)));
                    AYEmu.AY_Out(channel.Chip, AYRegister.AY_CHNL_A_VOL, (byte)(AyBank[fxNum].AyEffect[channel.Offset].Volume));
                }

                channel.Offset++;
            }

            if (channel.Offset >= slen)
            {
                SFX_ClearChannel(channel);
                return 0;
            }

            return (short)(channel.Chip.Out[0] + channel.Chip.Out[1] / 2);
        }

        public static byte[] SFX_MakeWAV(int fxn, int off)
        {
            int frames = SFX_RealLen(fxn) + 3 - off;
            int ifrq = PT3Play.SAMPLE_RATE / 50;
            int slen = ifrq * frames;
            int flen = slen * 2 + 44;
            byte[] wave = new byte[flen];

            Array.Copy(new byte[] { 0x52, 0x49, 0x46, 0x46 }, wave, 4);
            WrDWordLH(wave, 4, flen - 8);
            Array.Copy(new byte[] { 0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20 }, 0, wave, 8, 8);
            WrDWordLH(wave, 16, 16);
            WrWordLH(wave, 20, 1);
            WrWordLH(wave, 22, 1);
            WrDWordLH(wave, 24, PT3Play.SAMPLE_RATE);
            WrDWordLH(wave, 28, PT3Play.SAMPLE_RATE * 2);
            WrWordLH(wave, 32, 2);
            WrWordLH(wave, 34, 16);
            Array.Copy(new byte[] { 0x64, 0x61, 0x74, 0x61 }, 0, wave, 36, 4);
            WrDWordLH(wave, 40, slen * 2);

            short[] buf = new short[slen];

            int icnt = 0;
            AYChip ayChip = null;
            AYEmu.AY_Init(ref ayChip);
            int pp = off;

            for (int i = 0; i < slen; i++)
            {
                AYEmu.AY_Tick(ayChip, PT3Play.AY_CLOCK / PT3Play.SAMPLE_RATE / 8);

                if (icnt++ >= ifrq)
                {
                    icnt = 0;

                    if (pp < MAX_FX_LEN)
                    {
                        AYEmu.AY_Out(ayChip, AYRegister.AY_CHNL_A_FINE, (byte)(AyBank[fxn].AyEffect[pp].Tone & 0xFF));
                        AYEmu.AY_Out(ayChip, AYRegister.AY_CHNL_A_COARSE, (byte)(AyBank[fxn].AyEffect[pp].Tone >> 8));
                        AYEmu.AY_Out(ayChip, AYRegister.AY_NOISE_PERIOD, (byte)(AyBank[fxn].AyEffect[pp].Noise));
                        AYEmu.AY_Out(ayChip, AYRegister.AY_MIXER, (byte)(0xF6 | (AyBank[fxn].AyEffect[pp].T ? 0 : 1) | (AyBank[fxn].AyEffect[pp].N ? 0 : 8)));
                        AYEmu.AY_Out(ayChip, AYRegister.AY_CHNL_A_VOL, (byte)(AyBank[fxn].AyEffect[pp].Volume));
                    }

                    pp++;
                }

                buf[i] = (short)(ayChip.Out[0] + ayChip.Out[1] / 2);
            }

            Buffer.BlockCopy(buf, 0, wave, 44, slen * 2);
            return wave;
        }

        public static void SFX_Play(int fxn)
        {
            int channelIndex = SFX_GetChannelIndex();

            SFX_ClearChannel(AyChannels[channelIndex]);

            AyChannels[channelIndex].FxNum = fxn;
            AyChannels[channelIndex].Length = SFX_RealLen(fxn) + 3;

            AYEmu.AY_Init(ref AyChannels[channelIndex].Chip);
        }

        public static int SFX_GetChannelIndex()
        {
            for (int i = 0; i < SFX_CHANNEL_COUNT; i++)
            {
                if (AyChannels[i].FxNum == -1)
                    return i;
            }

            return 0;
        }

        public static void SFX_ClearChannel(AyfxChannel channel)
        {
            channel.FxNum = -1;
            channel.Icnt = 0;
            channel.Offset = 0;
            channel.Length = 0;
        }

        public static void SFX_BankInit()
        {
            for (int aa = 0; aa < MAX_FX_ALL; aa++)
            {
                for (int bb = 0; bb < MAX_FX_LEN; bb++) SFX_DefaultStr(ref AyBank[aa].AyEffect[bb]);
                string name = $"noname{aa + 1:D3}";
                AyBank[aa].Name = name;
            }
            AllEffect = 1;
        }

        public static bool SFX_BankSave(string filename, bool names)
        {
            using (FileStream file = new FileStream(filename, FileMode.Create))
            {
                int size = MAX_FX_LEN * AllEffect + 260 * AllEffect;
                byte[] buf = new byte[size];
                buf[0] = (byte)(AllEffect & 0xff);
                int pp = 2 * AllEffect + 1;

                for (int aa = 0; aa < AllEffect; aa++)
                {
                    int off = pp - aa * 2 - 2;
                    WrWordLH(buf, 1 + aa * 2, off);
                    int len = SFX_Encode(aa, buf);
                    pp += len;

                    if (names && !string.IsNullOrEmpty(AyBank[aa].Name))
                    {
                        byte[] nameBytes = System.Text.Encoding.ASCII.GetBytes(AyBank[aa].Name);
                        nameBytes.CopyTo(buf, pp);
                        pp += nameBytes.Length;
                        buf[pp++] = 0;
                    }
                }

                file.Write(buf, 0, pp);
                return true;
            }
        }

        public static bool SFX_BankLoad(string filename)
        {
            using (FileStream file = new FileStream(filename, FileMode.Open))
            {
                int size = (int)file.Length;
                byte[] buf = new byte[size];
                file.Read(buf, 0, size);

                SFX_BankInit();

                AllEffect = buf[0];

                for (int aa = 0; aa < AllEffect; aa++)
                {
                    int off = RdWordLH(buf, 1 + aa * 2) + 2 + aa * 2;
                    int len = (aa < AllEffect - 1) ? RdWordLH(buf, 3 + aa * 2) + 4 + aa * 2 - off : size - off;
                    int rlen = SFX_Decode(aa, buf, off, len);

                    if (rlen != len)
                    {
                        int nul = Array.FindIndex(buf, off + rlen, (x) => x == 0) - (off + rlen);
                        AyBank[aa].Name = System.Text.Encoding.ASCII.GetString(buf, off + rlen, nul);
                    }
                    else
                    {
                        string name = $"noname{aa + 1:D3}";
                        AyBank[aa].Name = name;
                    }
                }

                return true;
            }
        }

        private static void WrWordLH(byte[] buf, int off, int value)
        {
            buf[off] = (byte)(value & 0xff);
            buf[off + 1] = (byte)((value >> 8) & 0xff);
        }

        private static int RdWordLH(byte[] buf, int off)
        {
            return buf[off] | (buf[off + 1] << 8);
        }

        private static void WrDWordLH(byte[] buf, int off, int num)
        {
            buf[off + 3] = (byte)((num >> 24) & 0xff);
            buf[off + 2] = (byte)((num >> 16) & 0xff);
            buf[off + 1] = (byte)((num >> 8) & 0xff);
            buf[off] = (byte)(num & 0xff);
        }

        public static int EffectCount {  get { return AyBank.Length; } }
    }

    public class AyfxBankCell
    {
        public AyfxCell[] AyEffect;
        public string Name;
    }

    public class AyfxCell
    {
        public int Tone;
        public int Noise;
        public int Volume;
        public bool T;
        public bool N;
        public bool Sel;
    }

    public class AyfxChannel
    {
        public int FxNum;
        public int Icnt;
        public int Offset;
        public int Length;
        public AYChip Chip;
        public byte Volume;
    }
}
