using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

using SlimDX;
using SlimDX.Direct3D9;
using SlimDX.Windows;
using SlimDX.DirectSound;
using SlimDX.Multimedia;
using SlimDX.XAudio2;
using System.Security.Cryptography;

namespace PT3Play
{
	struct Vertex
	{
		public Vector3 Position;
		public int Color;
    }

    static class Program
	{
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetKeyboardState(byte[] keystate);

        private const int AUDIO_BUFFER_COUNT = 3;

        private static XAudio2 m_xAudio2 = null;
		private static MasteringVoice m_masteringVoice = null;
		private static SourceVoice m_sourceVoice = null;
		private static AudioBuffer m_audioBuffer = null;
		private static BinaryWriter m_binaryWriter = null;

        private static Vertex[] m_vertexArray = null;
		private static VertexBuffer m_vertexBuffer = null;
		private static VertexDeclaration m_vertexDeclaration = null;

		private static RenderForm m_form = null;
		private static Device m_device = null;

        private static Size m_screenSize;

		private static Timer m_timer = null;

        private static byte[] m_currentKeyStates = new byte[256];
        private static byte[] m_previousKeyStates = new byte[256];

        [STAThread]
		static void Main()
		{
            string musicPath = Path.Combine(Application.StartupPath, "music");
            string sfxPath = Path.Combine(Application.StartupPath, "sfx");
            string[] songArray = Directory.GetFiles(musicPath, "*.pt3");
            string sfxFileName = Path.Combine(sfxPath, "streetsofrage_2.afb");
            int currentSong = 0;
            int currentSfx = 0;

            m_form = new RenderForm("PT3Play");
			m_form.TopMost = false;
			m_form.FormBorderStyle = FormBorderStyle.Sizable;
			m_form.WindowState = FormWindowState.Normal;
            m_form.Show();

            m_screenSize = new Size(m_form.ClientSize.Width, m_form.ClientSize.Height);

            m_device = new Device(new Direct3D(), 0, SlimDX.Direct3D9.DeviceType.Hardware, m_form.Handle, CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded, new PresentParameters()
			{
				BackBufferWidth = m_screenSize.Width,
				BackBufferHeight = m_screenSize.Height,
				//PresentationInterval = PresentInterval.One
			});

            InitVideoEngine(m_screenSize);
            InitAudioEngine();

            byte[] musicBytes = File.ReadAllBytes(songArray[currentSong]);
            PT3Play.MusicPlay(ref musicBytes);

            AYFx.SFX_BankLoad(sfxFileName);

            m_timer = new Timer();
			m_timer.Start();

            //m_form.KeyDown += new KeyEventHandler(OnKeyDown);

            double m_accumulator = 0.0;
            double frameTime = 1.0 / PT3Play.FRAME_RATE;

            MessagePump.Run(m_form, () =>
			{
                GetKeyboardState(m_currentKeyStates);

                m_timer.Update();

                m_accumulator += m_timer.ElapsedTime;

                if (m_accumulator > frameTime * 5)
                    m_accumulator = frameTime * 5;

                if (m_accumulator >= frameTime)
				{
                    UpdateAudio();

                    m_accumulator -= frameTime;

                    Render();
				}

                if (IsKeyUp(Keys.Up))
                {
                    if (--currentSong < 0)
                        currentSong = songArray.Length - 1;

                    musicBytes = File.ReadAllBytes(songArray[currentSong]);
                    PT3Play.MusicPlay(ref musicBytes);
                }

                if (IsKeyUp(Keys.Down))
                {
                    if (++currentSong >= songArray.Length)
                        currentSong = 0;

                    musicBytes = File.ReadAllBytes(songArray[currentSong]);
                    PT3Play.MusicPlay(ref musicBytes);
                }

                if (IsKeyUp(Keys.Left))
                {
                    if (--currentSfx < 0)
                        currentSfx = AYFx.AllEffect - 1;

                    AYFx.SFX_Play(currentSfx);
                }

                if (IsKeyUp(Keys.Right))
                {
                    if (++currentSfx >= AYFx.AllEffect)
                        currentSfx = 0;

                    AYFx.SFX_Play(currentSfx);
                }

                if (IsKeyUp(Keys.Space))
                    AYFx.SFX_Play(currentSfx);

                if (IsKeyUp(Keys.Escape))
					m_form.Close();

                Array.Copy(m_currentKeyStates, m_previousKeyStates, m_currentKeyStates.Length); 
			});

			foreach (var item in ObjectTable.Objects)
				item.Dispose();

            if (m_xAudio2 != null)
    			m_xAudio2.Dispose();
		}

        public static void InitVideoEngine(Size size)
        {
            m_device.Viewport = new Viewport(0, 0, size.Width, size.Height);

            m_vertexArray = new Vertex[3 * 2 * PT3Play.spec_levels.Length];

            m_vertexBuffer = new VertexBuffer(m_device, m_vertexArray.Length * Marshal.SizeOf(typeof(Vertex)), Usage.WriteOnly, VertexFormat.None, Pool.Managed);

            VertexElement[] vertexElems = new VertexElement[] {
                new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.PositionTransformed, 0),
                new VertexElement(0, 12, DeclarationType.Color, DeclarationMethod.Default, DeclarationUsage.Color, 0),
                VertexElement.VertexDeclarationEnd
            };

            m_vertexDeclaration = new VertexDeclaration(m_device, vertexElems);
        }

        public static void InitAudioEngine()
		{
			m_xAudio2 = new XAudio2();
			m_masteringVoice = new MasteringVoice(m_xAudio2);

			WaveFormat waveFormat = new WaveFormat();

            waveFormat.FormatTag = WaveFormatTag.Pcm;
            waveFormat.Channels = 2;
            waveFormat.BitsPerSample = sizeof(short) * 8;
            waveFormat.SamplesPerSecond = PT3Play.SAMPLE_RATE;
            waveFormat.BlockAlignment = (short)(waveFormat.Channels * (waveFormat.BitsPerSample / 8));
            waveFormat.AverageBytesPerSecond = waveFormat.BlockAlignment * waveFormat.SamplesPerSecond;

            m_sourceVoice = new SourceVoice(m_xAudio2, waveFormat);
			m_sourceVoice.StreamEnd += OnStreamEnd;
			m_sourceVoice.BufferStart += OnBufferStart;
			m_sourceVoice.BufferEnd += OnBufferEnd;

            m_audioBuffer = new AudioBuffer();
			m_audioBuffer.AudioData = new MemoryStream();
			//m_audioBuffer.LoopBegin = 0;
			//m_audioBuffer.LoopLength = 81920 / waveFormat.BlockAlignment;
			//m_audioBuffer.LoopCount = XAudio2.LoopInfinite;

			m_binaryWriter = new BinaryWriter(m_audioBuffer.AudioData);

			m_sourceVoice.FlushSourceBuffers();
			m_sourceVoice.SubmitSourceBuffer(m_audioBuffer);
			m_sourceVoice.Start();
        }

        private static void Render()
        {
            m_device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            m_device.BeginScene();

            m_device.SetRenderState(RenderState.AlphaBlendEnable, true);
            m_device.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
            m_device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);

            m_device.SetRenderState(RenderState.FillMode, FillMode.Solid);

            float barWidth = (float)m_screenSize.Width / PT3Play.spec_levels.Length;
            float barHeight = (float)m_screenSize.Height;
            int vertexIndex = 0;

            for (int i = 0; i < PT3Play.spec_levels.Length; i++)
            {
                float x1 = i * barWidth;
                float x2 = (i + 1) * barWidth;
                float y1 = barHeight - ((float)PT3Play.spec_levels[i] / PT3Play.SPEC_HEIGHT) * barHeight;
                float y2 = barHeight;

                m_vertexArray[vertexIndex++] = new Vertex() { Position = new Vector3(x1, y1, 0), Color = (int)PT3Play.spec_colors[i] };
                m_vertexArray[vertexIndex++] = new Vertex() { Position = new Vector3(x2, y1, 0), Color = (int)PT3Play.spec_colors[i] };
                m_vertexArray[vertexIndex++] = new Vertex() { Position = new Vector3(x2, y2, 0), Color = (int)PT3Play.spec_colors[i] };

                m_vertexArray[vertexIndex++] = new Vertex() { Position = new Vector3(x1, y1, 0), Color = (int)PT3Play.spec_colors[i] };
                m_vertexArray[vertexIndex++] = new Vertex() { Position = new Vector3(x2, y2, 0), Color = (int)PT3Play.spec_colors[i] };
                m_vertexArray[vertexIndex++] = new Vertex() { Position = new Vector3(x1, y2, 0), Color = (int)PT3Play.spec_colors[i] };
            }

            DataStream dataStream = m_vertexBuffer.Lock(0, 0, SlimDX.Direct3D9.LockFlags.None);
            dataStream.WriteRange(m_vertexArray);
            m_vertexBuffer.Unlock();

            m_device.SetStreamSource(0, m_vertexBuffer, 0, Marshal.SizeOf(typeof(Vertex)));
            m_device.VertexFormat = VertexFormat.Position | VertexFormat.Diffuse;
            m_device.VertexDeclaration = m_vertexDeclaration;
            m_device.DrawPrimitives(PrimitiveType.TriangleList, 0, m_vertexArray.Length / 3);

            m_device.EndScene();
            m_device.Present();
        }

        private static void OnStreamEnd(object sender, EventArgs e)
		{
            //m_emulator.RenderSounds();
        }

		private static void OnBufferStart(object sender, ContextEventArgs e)
		{
		}

		private static void OnBufferEnd(object sender, ContextEventArgs e)
		{
		}

		private static void UpdateAudio()
		{
			int bufferSize = PT3Play.SAMPLE_RATE / PT3Play.FRAME_RATE;

            for (int i = 0; i < bufferSize; i++)
            {
				short music_l, music_r;
                short sfx_l, sfx_r;

                PT3Play.EmulateSample(out music_l, out music_r);
                AYFx.EmulateSample(out sfx_l, out sfx_r);

                int sample_l = (music_l + sfx_l) / 2;
                int sample_r = (music_r + sfx_r) / 2;

                if (sample_l > 32767)
                    sample_l = 32767;
                if (sample_r > 32767)
                    sample_r = 32767;

                m_binaryWriter.Write((short)sample_l);  // Writing left channel
                m_binaryWriter.Write((short)sample_r);  // Writing right channel
            }

            if (m_sourceVoice.State.BuffersQueued > AUDIO_BUFFER_COUNT)
                 m_sourceVoice.FlushSourceBuffers();

            m_audioBuffer.AudioBytes = (int)m_audioBuffer.AudioData.Length;
            m_audioBuffer.Flags = SlimDX.XAudio2.BufferFlags.EndOfStream;

            m_audioBuffer.AudioData.Position = 0;
            m_sourceVoice.SubmitSourceBuffer(m_audioBuffer);

            m_audioBuffer.AudioData.SetLength(0);

            //m_soundManager.PlayBuffer(ref e.Samples);
        }

        private static bool IsKeyUp(Keys key)
        {
            byte previousState = m_previousKeyStates[(int)key];
            byte currentState = m_currentKeyStates[(int)key];
            
            return (previousState & 0x80) != 0 && (currentState & 0x80) == 0;
        }
    }
}