// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using osu.Framework.Graphics;
using System.Drawing;
using System.Drawing.Imaging;
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
        private Sprite sprite = null!;
        private readonly string targetWindowTitle;
        private Thread? captureThread;
        private volatile bool running;

        // 同步信号
        private readonly AutoResetEvent captureRequest = new AutoResetEvent(false);
        private readonly AutoResetEvent frameReady = new AutoResetEvent(false);

        // 当前窗口尺寸 & 像素缓冲区
        private int currentWidth, currentHeight;
        private byte[]? pixelBuffer;
        private readonly object bufferLock = new object();

        private Texture? texture;

        private bool isWindowsLive = false;

        public CapturedWindowSprite(string windowTitle)
        {
            Masking = true;
            AlwaysPresent = true;
            targetWindowTitle = windowTitle;
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            sprite = new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fit
            };

            AddInternal(sprite);
            texture = renderer.CreateTexture(1, 1);
            sprite.Texture = texture;

            // 启动后台抓取线程
            running = true;
            captureThread = new Thread(captureLoop)
            {
                IsBackground = true,
                Name = $"WindowCapture<{targetWindowTitle}>"
            };
            captureThread.Start();
        }

        [Resolved]
        private IRenderer renderer { get; set; } = null!;

        private void captureLoop()
        {
            IntPtr hWnd = FindWindowByPartialTitle(targetWindowTitle);

            // 预先查一次 HWND
            while (running)
            {
                // 等待 Update 发起请求
                captureRequest.WaitOne();

                if (!running) break;

                if (hWnd != IntPtr.Zero && !IsWindow(hWnd))
                {
                    isWindowsLive = false;
                    hWnd = IntPtr.Zero;
                    lock (bufferLock)
                        pixelBuffer = null;
                    frameReady.Set();
                }

                if (hWnd == IntPtr.Zero)
                {
                    hWnd = FindWindowByPartialTitle(targetWindowTitle);

                    Thread.Sleep(100);
                    continue;
                }

                isWindowsLive = true;

                try
                {
                    GetWindowRect(hWnd, out RECT rect);
                    int w = rect.Right - rect.Left;
                    int h = rect.Bottom - rect.Top;

                    if (w <= 0 || h <= 0)
                    {
                        frameReady.Set();
                        continue;
                    }

                    using var bmp = CaptureWindowFromBitbit(hWnd);

                    var bmpData = bmp.LockBits(
                        new Rectangle(0, 0, w, h),
                        ImageLockMode.ReadOnly,
                        PixelFormat.Format32bppArgb);

                    // 4. 计算字节数 & 拷贝
                    int byteCount = Math.Abs(bmpData.Stride) * h;
                    byte[] buffer = new byte[byteCount];
                    Marshal.Copy(bmpData.Scan0, buffer, 0, byteCount);

                    // 5. 解锁
                    bmp.UnlockBits(bmpData);

                    lock (bufferLock)
                    {
                        pixelBuffer = buffer;
                        currentWidth = w;
                        currentHeight = h;
                    }
                }
                catch
                {
                    hWnd = IntPtr.Zero;
                }
                finally
                {
                    // 通知 Update 可以消费
                    frameReady.Set();
                }
            }
        }

        protected override void Update()
        {
            base.Update();

            var sw = Stopwatch.StartNew();

            // 1) 请求抓一帧
            captureRequest.Set();

            // 2) 等待抓取完成（同步），最多等 10ms 防止卡死，也可以改为无限等待
            if (!frameReady.WaitOne(0))
            {
                sw.Stop();
                return;
            }

            if (!isWindowsLive)
            {
                this.FadeOut(100);
                return;
            }

            sw.Stop();

            this.FadeIn(100);

            // 3) 消费像素缓冲区
            byte[]? frame;
            int w, h;

            lock (bufferLock)
            {
                if (pixelBuffer == null)
                {
                    return;
                }

                frame = pixelBuffer;
                w = currentWidth;
                h = currentHeight;
                pixelBuffer = null;
            }

            if (frame == null) return;

            // 4) 更新或重建纹理
            if (texture == null || texture.Width != w || texture.Height != h)
            {
                texture?.Dispose();
                texture = renderer.CreateTexture(w, h);
                sprite.Texture = texture;
            }

            texture.SetData(new MemoryTextureUpload(frame, w, h));
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            running = false;
            captureRequest.Set();
            captureThread?.Join();
            texture?.Dispose();
        }

        #region Windows API

        public static Bitmap CaptureWindowFromBitbit(IntPtr hWnd)
        {
            GetWindowRect(hWnd, out RECT rect);
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

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

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

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
