// * ----------------------------------------------------------------------------
// * Author: Ben Baker
// * Website: headsoft.com.au
// * E-Mail: benbaker@headsoft.com.au
// * Copyright (C) 2015 Headsoft. All Rights Reserved.
// * ----------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D9;
using SDXRect = SharpDX.Rectangle;

namespace PT3Play
{
    public partial class Win32
    {
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        public static extern void CopyMemory(IntPtr Destination, IntPtr Source, uint Length);

        [DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr MemSet(IntPtr dest, int c, int count);
    }

    public class DirectX9
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static int LoadSurfaceFromIntPtr(Surface destSurface, IntPtr srcPixels, int width, int height, int srcStrideBytes)
        {
            var desc = destSurface.Description;
            var dr = destSurface.LockRectangle(LockFlags.None);
            try
            {
                IntPtr dst = dr.DataPointer;
                int dstPitch = dr.Pitch;

                if (dstPitch == srcStrideBytes)
                {
                    uint total = (uint)(dstPitch * height);
                    Win32.CopyMemory(dst, srcPixels, total);
                }
                else
                {
                    IntPtr src = srcPixels;
                    for (int y = 0; y < height; y++)
                    {
                        Win32.CopyMemory(dst, src, (uint)srcStrideBytes);
                        dst = IntPtr.Add(dst, dstPitch);
                        src = IntPtr.Add(src, srcStrideBytes);
                    }
                }
                return 1;
            }
            finally
            {
                destSurface.UnlockRectangle();
            }
        }

        public static int LoadSurfaceFromBitmap(Surface destSurface, Bitmap bitmap)
        {
            var rect = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bd = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                return LoadSurfaceFromIntPtr(destSurface, bd.Scan0, bitmap.Width, bitmap.Height, bd.Stride);
            }
            finally
            {
                bitmap.UnlockBits(bd);
            }
        }

        public static void LoadTextureDataFromIntPtr(Device device, Texture texture, IntPtr pixelData, int width, int height, int stride)
        {
            using (Surface level0 = texture.GetSurfaceLevel(0))
            {
                LoadSurfaceFromIntPtr(level0, pixelData, width, height, stride);
            }
        }

        public static Bitmap LoadBitmapFromSurface(Surface surface)
        {
            int w = surface.Description.Width;
            int h = surface.Description.Height;

            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            var bd = bmp.LockBits(new System.Drawing.Rectangle(0, 0, w, h),
                                  ImageLockMode.WriteOnly,
                                  PixelFormat.Format32bppArgb);

            var dr = surface.LockRectangle(LockFlags.ReadOnly);
            try
            {
                int srcPitch = dr.Pitch;
                IntPtr src = dr.DataPointer;
                IntPtr dst = bd.Scan0;

                if (srcPitch == bd.Stride)
                {
                    uint total = (uint)(srcPitch * h);
                    Win32.CopyMemory(dst, src, total);
                }
                else
                {
                    for (int y = 0; y < h; y++)
                    {
                        Win32.CopyMemory(dst, src, (uint)Math.Min(srcPitch, bd.Stride));
                        src = IntPtr.Add(src, srcPitch);
                        dst = IntPtr.Add(dst, bd.Stride);
                    }
                }
            }
            finally
            {
                surface.UnlockRectangle();
                bmp.UnlockBits(bd);
            }

            return bmp;
        }

        public static Bitmap TakeSnapshot(Device device, System.Drawing.Rectangle rect)
        {
            Bitmap bitmap;
            using (var surface = Surface.CreateOffscreenPlain(device, rect.Width, rect.Height, Format.A8R8G8B8, Pool.SystemMemory))
            {
                device.GetFrontBufferData(0, surface);

                using (var stream = Surface.ToStream(surface, ImageFileFormat.Bmp))
                {
                    bitmap = new Bitmap(stream);
                }
            }
            return bitmap;
        }

        public static int GetTexturePitch(Texture texture)
        {
            var dr = texture.LockRectangle(0, LockFlags.ReadOnly);
            try
            {
                return dr.Pitch;
            }
            finally
            {
                texture.UnlockRectangle(0);
            }
        }

        public static void ClearTexture(Texture texture)
        {
            var levelDesc = texture.GetLevelDescription(0);
            var dr = texture.LockRectangle(0, LockFlags.None);
            try
            {
                int byteCount = dr.Pitch * levelDesc.Height;
                Win32.MemSet(dr.DataPointer, 0, byteCount);
            }
            finally
            {
                texture.UnlockRectangle(0);
            }
        }


        public static int LoadSurfaceFromMemory(Surface destSurface, ref RECT destRect, ref RECT srcRect, uint srcPitch, IntPtr srcMemory)
        {
            var desc = destSurface.Description;
            var rect = new SDXRect(srcRect.Left, srcRect.Top, srcRect.Right - srcRect.Left, srcRect.Bottom - srcRect.Top);

            var dr = destSurface.LockRectangle(LockFlags.None);
            try
            {
                IntPtr dst = dr.DataPointer;
                int dstPitch = dr.Pitch;

                dst = IntPtr.Add(dst, rect.Top * dstPitch + rect.Left * 4);
                IntPtr src = IntPtr.Add(srcMemory, rect.Top * (int)srcPitch + rect.Left * 4);

                int rowBytes = rect.Width * 4;
                for (int y = 0; y < rect.Height; y++)
                {
                    Win32.CopyMemory(dst, src, (uint)rowBytes);
                    dst = IntPtr.Add(dst, dstPitch);
                    src = IntPtr.Add(src, (int)srcPitch);
                }

                return 1;
            }
            finally
            {
                destSurface.UnlockRectangle();
            }
        }
    }
}