// AYEmu by Sergey Bulba (svbulba@gmail.com)
// Port to C# by benbaker76 (https://github.com/benbaker76)

using System.Runtime.InteropServices;

namespace PT3Play
{
    public static class AYEmu
    {
        public const int VDIV = 3;

        public static AYChip AyChip = null;

        public static int[] volTab = { 0 / VDIV, 836 / VDIV, 1212 / VDIV, 1773 / VDIV, 2619 / VDIV, 3875 / VDIV, 5397 / VDIV, 8823 / VDIV, 10392 / VDIV, 16706 / VDIV, 23339 / VDIV, 29292 / VDIV, 36969 / VDIV, 46421 / VDIV, 55195 / VDIV, 65535 / VDIV };

        static AYEmu()
        {
            AY_Init(ref AyChip);
        }

        public static void AY_Init(ref AYChip ay)
        {
            ay = new AYChip();
            ay.Tone = new Tone[3];
            for (int i = 0; i < 3; i++)
                ay.Tone[i] = new Tone();
            ay.Noise = new Noise();
            ay.Env = new Env();
            ay.Reg = new int[16];
            ay.Dac = new int[3];
            ay.Out = new int[3];
            ay.Noise.Reg = 0x0ffff;
            ay.Noise.Qcc = 0;
            ay.Noise.State = 0;
        }

        public static void AY_Out(AYChip ay, AYRegister reg, int value)
        {
            if ((int)reg > 13)
                return;

            switch (reg)
            {
                case AYRegister.AY_CHNL_A_COARSE:
                case AYRegister.AY_CHNL_B_COARSE:
                case AYRegister.AY_CHNL_C_COARSE:
                    value &= 15;
                    break;
                case AYRegister.AY_CHNL_A_VOL:
                case AYRegister.AY_CHNL_B_VOL:
                case AYRegister.AY_CHNL_C_VOL:
                case AYRegister.AY_NOISE_PERIOD:
                    value &= 31;
                    break;
                case AYRegister.AY_ENV_SHAPE:
                    value &= 15;
                    ay.Env.Count = 0;
                    if ((value & 2) != 0)
                    {
                        ay.Env.Dac = 0;
                        ay.Env.Up = 1;
                    }
                    else
                    {
                        ay.Env.Dac = 15;
                        ay.Env.Up = 0;
                    }
                    break;
            }

            ay.Reg[(int)reg] = value;
        }

        public static void AY_Tick(AYChip ay, int ticks)
        {
            int noise_di;
            int ta;
            int tb;
            int tc;
            int na;
            int nb;
            int nc;

            ay.Out[0] = 0;
            ay.Out[1] = 0;
            ay.Out[2] = 0;

            for (int i = 0; i < ticks; ++i)
            {
                ay.FreqDiv ^= 1;

                if (ay.Tone[0].Count >= (ay.Reg[0] | (ay.Reg[1] << 8)))
                {
                    ay.Tone[0].Count = 0;
                    ay.Tone[0].State ^= 1;
                }
                if (ay.Tone[1].Count >= (ay.Reg[2] | (ay.Reg[3] << 8)))
                {
                    ay.Tone[1].Count = 0;
                    ay.Tone[1].State ^= 1;
                }
                if (ay.Tone[2].Count >= (ay.Reg[4] | (ay.Reg[5] << 8)))
                {
                    ay.Tone[2].Count = 0;
                    ay.Tone[2].State ^= 1;
                }

                ay.Tone[0].Count++;
                ay.Tone[1].Count++;
                ay.Tone[2].Count++;

                if (ay.FreqDiv != 0)
                {
                    if (ay.Noise.Count == 0)
                    {
                        noise_di = (ay.Noise.Qcc ^ ((ay.Noise.Reg >> 13) & 1)) ^ 1;
                        ay.Noise.Qcc = (ay.Noise.Reg >> 15) & 1;
                        ay.Noise.State = ay.Noise.Qcc;
                        ay.Noise.Reg = (ay.Noise.Reg << 1) | noise_di;
                    }

                    ay.Noise.Count = (ay.Noise.Count + 1) & 31;
                    if (ay.Noise.Count >= ay.Reg[6])
                        ay.Noise.Count = 0;

                    if (ay.Env.Count == 0)
                    {
                        switch (ay.Reg[13])
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 3:
                            case 9:
                                if (ay.Env.Dac > 0)
                                    ay.Env.Dac--;
                                break;
                            case 4:
                            case 5:
                            case 6:
                            case 7:
                            case 15:
                                if (ay.Env.Up != 0)
                                {
                                    ay.Env.Dac++;
                                    if (ay.Env.Dac > 15)
                                    {
                                        ay.Env.Dac = 0;
                                        ay.Env.Up = 0;
                                    }
                                }
                                break;

                            case 8:
                                ay.Env.Dac--;
                                if (ay.Env.Dac < 0)
                                    ay.Env.Dac = 15;
                                break;

                            case 10:
                            case 14:
                                if (ay.Env.Up == 0)
                                {
                                    ay.Env.Dac--;
                                    if (ay.Env.Dac < 0)
                                    {
                                        ay.Env.Dac = 0;
                                        ay.Env.Up = 1;
                                    }
                                }
                                else
                                {
                                    ay.Env.Dac++;
                                    if (ay.Env.Dac > 15)
                                    {
                                        ay.Env.Dac = 15;
                                        ay.Env.Up = 0;
                                    }

                                }
                                break;

                            case 11:
                                if (ay.Env.Up == 0)
                                {
                                    ay.Env.Dac--;
                                    if (ay.Env.Dac < 0)
                                    {
                                        ay.Env.Dac = 15;
                                        ay.Env.Up = 1;
                                    }
                                }
                                break;

                            case 12:
                                ay.Env.Dac++;
                                if (ay.Env.Dac > 15)
                                    ay.Env.Dac = 0;
                                break;

                            case 13:
                                if (ay.Env.Dac < 15)
                                    ay.Env.Dac++;
                                break;

                        }
                    }

                    ay.Env.Count++;
                    if (ay.Env.Count >= (ay.Reg[11] | (ay.Reg[12] << 8)))
                        ay.Env.Count = 0;
                }

                ta = ay.Tone[0].State | ((ay.Reg[7] >> 0) & 1);
                tb = ay.Tone[1].State | ((ay.Reg[7] >> 1) & 1);
                tc = ay.Tone[2].State | ((ay.Reg[7] >> 2) & 1);
                na = ay.Noise.State | ((ay.Reg[7] >> 3) & 1);
                nb = ay.Noise.State | ((ay.Reg[7] >> 4) & 1);
                nc = ay.Noise.State | ((ay.Reg[7] >> 5) & 1);

                if ((ay.Reg[8] & 16) != 0)
                {
                    ay.Dac[0] = ay.Env.Dac;
                }
                else
                {
                    if ((ta & na) != 0)
                        ay.Dac[0] = ay.Reg[8];
                    else
                        ay.Dac[0] = 0;
                }

                if ((ay.Reg[9] & 16) != 0)
                {
                    ay.Dac[1] = ay.Env.Dac;
                }
                else
                {
                    if ((tb & nb) != 0)
                        ay.Dac[1] = ay.Reg[9];
                    else
                        ay.Dac[1] = 0;
                }

                if ((ay.Reg[10] & 16) != 0)
                {
                    ay.Dac[2] = ay.Env.Dac;
                }
                else
                {
                    if ((tc & nc) != 0)
                        ay.Dac[2] = ay.Reg[10];
                    else
                        ay.Dac[2] = 0;
                }

                ay.Out[0] += volTab[ay.Dac[0]];
                ay.Out[1] += volTab[ay.Dac[1]];
                ay.Out[2] += volTab[ay.Dac[2]];
            }

            ay.Out[0] /= ticks;
            ay.Out[1] /= ticks;
            ay.Out[2] /= ticks;
        }
    }

    public enum AYRegister : int
    {
        AY_CHNL_A_FINE = 0,
        AY_CHNL_A_COARSE,
        AY_CHNL_B_FINE,
        AY_CHNL_B_COARSE,
        AY_CHNL_C_FINE,
        AY_CHNL_C_COARSE,
        AY_NOISE_PERIOD,
        AY_MIXER,
        AY_CHNL_A_VOL,
        AY_CHNL_B_VOL,
        AY_CHNL_C_VOL,
        AY_ENV_FINE,
        AY_ENV_COARSE,
        AY_ENV_SHAPE,
        AY_GPIO_A,
        AY_GPIO_B
    }

    public class Tone
    {
        public int Count;
        public int State;
    }

    public class Noise
    {
        public int Count;
        public int Reg;
        public int Qcc;
        public int State;
    }

    public class Env
    {
        public int Count;
        public int Dac;
        public int Up;
    }

    public class AYChip
    {
        public Tone[] Tone;
        public Noise Noise;
        public Env Env;
        public int[] Reg;
        public int[] Dac;
        public int[] Out;
        public int FreqDiv;
    }
}