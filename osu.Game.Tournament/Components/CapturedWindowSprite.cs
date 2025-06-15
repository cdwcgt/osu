// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;

namespace osu.Game.Tournament.Components
{
    [SupportedOSPlatform("windows")]
    public partial class CapturedWindowSprite : CompositeDrawable
    {
        private Texture? texture;
        private Sprite sprite;
        private string targetWindowTitle;
        private Thread? captureThread;
        private bool running = true;
        private object frameLock = new object();
        private byte[]? latestFrameBytes;

        public CapturedWindowSprite(string windowTitle)
        {
            targetWindowTitle = windowTitle;
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            sprite = new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fit
            };

            AddInternal(sprite);
        }

        [Resolved]
        private IRenderer renderer { get; set; } = null!;

        protected override void Update()
        {
            base.Update();

            try
            {
                using var bmp = CaptureWindowFromBitbit(targetWindowTitle);
                using var ms = new MemoryStream();
                bmp.Save(ms, ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);

                var newTex = Texture.FromStream(renderer, ms);
                sprite.Texture = newTex;

                texture?.Dispose();
                texture = newTex;
            }
            catch { }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            running = false;
            captureThread?.Join();
            texture?.Dispose();
        }

        #region Windows API

        public static Bitmap CaptureWindowFromBitbit(string windowTitle)
        {
            IntPtr hWnd = FindWindowByPartialTitle(windowTitle);
            if (hWnd == IntPtr.Zero)
                throw new Exception("窗口未找到: " + windowTitle);

            GetWindowRect(hWnd, out RECT rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            Bitmap bmp = new Bitmap(width, height);

            using (System.Drawing.Graphics gfxBmp = System.Drawing.Graphics.FromImage(bmp))
            {
                IntPtr hdcBitmap = gfxBmp.GetHdc();
                IntPtr hdcWindow = GetWindowDC(hWnd);

                BitBlt(hdcBitmap, 0, 0, width, height, hdcWindow, 0, 0, 0x00CC0020); // SRCCOPY

                ReleaseDC(hWnd, hdcWindow);
                gfxBmp.ReleaseHdc(hdcBitmap);
            }

            return bmp;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int w, int h,
                                          IntPtr hdcSrc, int xSrc, int ySrc, int rop);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public static IntPtr FindWindowByPartialTitle(string partialTitle)
        {
            IntPtr result = IntPtr.Zero;
            EnumWindows((hWnd, lParam) =>
            {
                StringBuilder sb = new StringBuilder(256);
                GetWindowText(hWnd, sb, sb.Capacity);

                if (sb.ToString().Contains(partialTitle))
                {
                    result = hWnd;
                    return false; // 停止遍历
                }

                return true;
            }, IntPtr.Zero);

            return result;
        }

        #endregion
    }
}
