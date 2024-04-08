// * ----------------------------------------------------------------------------
// * Author: Ben Baker
// * Website: headsoft.com.au
// * E-Mail: benbaker@headsoft.com.au
// * Copyright (C) 2015 Headsoft. All Rights Reserved.
// * ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SlimDX.Direct3D9;
using SlimDX;

namespace PT3Play
{
	public partial class Win32
	{
		[DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
		public static extern void CopyMemory(IntPtr Destination, IntPtr Source, uint Length);

		[DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
		public static extern IntPtr MemSet(IntPtr dest, int c, int count);
	}

    class DirectX9
    {
        public enum D3dFormat
        {
            D3DFMT_A8R8G8B8 = 21
        }

        public enum D3dxFilter : uint
        {
            Default = unchecked((uint)-1),
            None = 1
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("d3dx9_43.dll")]
        private static extern int D3DXLoadSurfaceFromMemory(IntPtr pDestSurface, IntPtr pDestPalette, ref RECT pDestRect, IntPtr pSrcMemory, D3dFormat SrcFormat, uint SrcPitch, IntPtr pSrcPalette, ref RECT pSrcRect, D3dxFilter Filter, int ColorKey);

        public static int LoadSurfaceFromMemory(Surface destSurface, ref RECT destRect, ref RECT srcRect, uint srcPitch, IntPtr srcMemory)
        {
            return D3DXLoadSurfaceFromMemory(destSurface.ComPointer, IntPtr.Zero, ref srcRect, srcMemory, D3dFormat.D3DFMT_A8R8G8B8, srcPitch, IntPtr.Zero, ref srcRect, D3dxFilter.None, 0);
        }

        public static int LoadSurfaceFromBitmap(Surface destSurface, Bitmap bitmap)
        {
            BitmapData bd = new BitmapData();

            bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb, bd);

            RECT srcRect;
            srcRect.Left = 0;
            srcRect.Top = 0;
            srcRect.Right = bitmap.Width;
            srcRect.Bottom = bitmap.Height;

            int retVal = D3DXLoadSurfaceFromMemory(destSurface.ComPointer, IntPtr.Zero, ref srcRect, bd.Scan0, D3dFormat.D3DFMT_A8R8G8B8, (uint)bd.Stride, IntPtr.Zero, ref srcRect, D3dxFilter.None, 0);

            bitmap.UnlockBits(bd);

            return retVal;
        }

        public static Bitmap LoadBitmapFromSurface(Surface surface)
        {
            Size size = new Size(surface.Description.Width, surface.Description.Height);
            Bitmap b = new Bitmap(size.Width, size.Height);

            BitmapData bd = b.LockBits(new Rectangle(Point.Empty, size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            DataRectangle dr = surface.LockRectangle(LockFlags.ReadOnly);

            Win32.CopyMemory(bd.Scan0, dr.Data.DataPointer, (uint) dr.Data.Length);

            surface.UnlockRectangle();
            b.UnlockBits(bd);

            return b;
        }

		public static void LoadTextureDataFromIntPtr(Device device, Texture texture, IntPtr pixelData, int width, int height, int stride)
		{
			using (Surface destSurface = texture.GetSurfaceLevel(0))
				LoadSurfaceFromIntPtr(destSurface, pixelData, width, height, stride);
		}

		public static int LoadSurfaceFromIntPtr(Surface destSurface, IntPtr pixelData, int width, int height, int stride)
		{
			RECT srcRect;
			srcRect.Left = 0;
			srcRect.Top = 0;
			srcRect.Right = width;
			srcRect.Bottom = height;

			DataRectangle dataRectangle = destSurface.LockRectangle(LockFlags.None);
			IntPtr dataPointer = dataRectangle.Data.DataPointer;

			if (stride == dataRectangle.Pitch)
			{
				Win32.CopyMemory(dataPointer, pixelData, (uint)dataRectangle.Data.Length);
			}
			else
			{
				for (int i = 0; i < height; i++)
				{
					Win32.CopyMemory(dataPointer, pixelData, (uint)stride);

					dataPointer = new IntPtr(dataPointer.ToInt64() + dataRectangle.Pitch);
					pixelData = new IntPtr(pixelData.ToInt64() + stride);
				}
			}

			destSurface.UnlockRectangle();

			//D3DXLoadSurfaceFromMemory(destSurface.ComPointer, IntPtr.Zero, ref srcRect, pixelData, D3dFormat.D3DFMT_A8R8G8B8, (uint)stride, IntPtr.Zero, ref srcRect, D3dxFilter.None, 0);
			//D3DXLoadSurfaceFromMemory(destSurface.ComPointer, IntPtr.Zero, ref srcRect, pixelData, D3dFormat.D3DFMT_A8B8G8R8, (uint)stride, IntPtr.Zero, ref srcRect, D3dxFilter.None, 0);

			return 1;
		}

        public static Bitmap TakeSnapshot(Device device, Rectangle rect)
        {
            Bitmap bitmap = null;

            using (Surface surface = Surface.CreateOffscreenPlain(device, rect.Width, rect.Height, Format.A8R8G8B8, Pool.SystemMemory))
            {
                device.GetFrontBufferData(0, surface);

                bitmap = new Bitmap(Surface.ToStream(surface, ImageFileFormat.Bmp, new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height)));
            }

            return bitmap;
        }

		public static int GetTexturePitch(Texture texture)
		{
			DataRectangle dataRectangle = texture.LockRectangle(0, LockFlags.ReadOnly);

			int pitch = dataRectangle.Pitch;

			texture.UnlockRectangle(0);

			return pitch;
		}

		public static void ClearTexture(Texture texture)
		{
			DataRectangle dataRectangle = texture.LockRectangle(0, LockFlags.None);
			IntPtr dataPointer = dataRectangle.Data.DataPointer;
			Win32.MemSet(dataPointer, 0, (int)dataRectangle.Data.Length);
			texture.UnlockRectangle(0);
		}
    }
}
