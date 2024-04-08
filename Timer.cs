using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace PT3Play
{
	public class Timer
	{
		[System.Security.SuppressUnmanagedCodeSecurity]
		[DllImport("kernel32")]
		private static extern bool QueryPerformanceFrequency(ref long PerformanceFrequency);

		[System.Security.SuppressUnmanagedCodeSecurity]
		[DllImport("kernel32")]
		private static extern bool QueryPerformanceCounter(ref long PerformanceCount);

		private long m_ticksPerSecond;
		private long m_currentTime;
		private long m_lastTime;
		private long m_lastFPSUpdate;
		private long m_FPSUpdateInterval;
		private uint m_numFrames;
		private double m_runningTime;
		private double m_timeElapsed;
		private double m_fps;
		private bool m_timerStopped;

		private double[] m_fpsSamples = new double[100];
		private int m_fpsPosition = 0;
		private int m_fpsSampleCount = 0;

		public Timer()
		{
			QueryPerformanceFrequency(ref m_ticksPerSecond);

			m_timerStopped = true;
			m_FPSUpdateInterval = m_ticksPerSecond >> 1;
		}

		public void Start()
		{
			if (!Stopped)
				return;

			QueryPerformanceCounter(ref m_lastTime);
			m_timerStopped = false;
		}

		public void Stop()
		{
			if (Stopped)
				return;

			long stopTime = 0;

			QueryPerformanceCounter(ref stopTime);

			m_runningTime += (double)(stopTime - m_lastTime) / (double)m_ticksPerSecond;
			m_timerStopped = true;
		}

		public void Update()
		{
			if (Stopped)
				return;

			QueryPerformanceCounter(ref m_currentTime);

			m_timeElapsed = (double)(m_currentTime - m_lastTime) / (double)m_ticksPerSecond;
			m_runningTime += m_timeElapsed;

			m_numFrames++;

			if (m_currentTime - m_lastFPSUpdate >= m_FPSUpdateInterval)
			{
				double currentTime = (double)m_currentTime / (double)m_ticksPerSecond;
				double lastTime = (double)m_lastFPSUpdate / (double)m_ticksPerSecond;

				m_fps = (double)m_numFrames / (currentTime - lastTime);

				m_lastFPSUpdate = m_currentTime;
				m_numFrames = 0;
			}

			m_lastTime = m_currentTime;
		}

		public int AverageFPS()
		{
			double fpsTotal = 0;
			int fpsAverage = 0;

			m_fpsSamples[m_fpsPosition] = FPS;

			for (int i = 0; i < m_fpsSampleCount; i++)
				fpsTotal += m_fpsSamples[i];

			fpsAverage = (int)(fpsTotal / (double)m_fpsSampleCount);

			m_fpsPosition++;

			if (m_fpsPosition == m_fpsSamples.Length)
				m_fpsPosition = 0;

			if (m_fpsSampleCount < m_fpsSamples.Length)
				m_fpsSampleCount++;

			return fpsAverage;
		}

		public bool Stopped
		{
			get { return m_timerStopped; }
		}

		public double FPS
		{
			get { return m_fps; }
		}

		public double ElapsedTime
		{
			get
			{
				if (Stopped)
					return 0;

				return m_timeElapsed;
			}
		}

		public double RunningTime
		{
			get { return m_runningTime; }
		}
	}
}
