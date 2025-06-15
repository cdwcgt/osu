// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Tournament.Models;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Game.Tournament.Components
{
    [SupportedOSPlatform("windows")]
    public partial class CapturedWindowSprite : CompositeDrawable
    {
        private Sprite sprite = null!;
        private readonly string targetWindowTitle;
        private Thread? captureThread;
        private bool running;

        private Bitmap? bitmapPool;
        private System.Drawing.Graphics? graphicsPool;
        private byte[]? rawBufferPool;
        private MemoryTextureUpload? uploadPool;
        private int poolWidth, poolHeight;

        // 同步信号
        private readonly AutoResetEvent captureRequest = new AutoResetEvent(false);
        private readonly AutoResetEvent frameReady = new AutoResetEvent(false);

        // 当前窗口尺寸 & 像素缓冲区
        private int currentWidth, currentHeight;
        private MemoryTextureUpload? pixelBuffer;
        private readonly object bufferLock = new object();

        private Texture? texture;

        private bool isWindowsLive = false;

        [Resolved]
        private LadderInfo? ladder { get; set; }

        public CapturedWindowSprite(string windowTitle)
        {
            Masking = true;
            AlwaysPresent = true;
            targetWindowTitle = windowTitle;
            RelativeSizeAxes = Axes.Both;
            Alpha = 0;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            sprite = new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fit
            };

            Name = $"WindowCapture<{targetWindowTitle}>";

            AddInternal(sprite);

            // 启动后台抓取线程
            running = true;
            captureThread = new Thread(captureLoop)
            {
                IsBackground = true,
                Name = $"WindowCapture<{targetWindowTitle}>"
            };
            captureThread.Start();

            if (ladder != null)
            {
                FrameRate.BindTo(ladder.FrameRate);
            }
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

                    if (bitmapPool == null || graphicsPool == null || poolWidth != w || poolHeight != h)
                    {
                        bitmapPool?.Dispose();
                        graphicsPool?.Dispose();

                        bitmapPool = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                        graphicsPool = System.Drawing.Graphics.FromImage(bitmapPool);

                        // 注意 LockBits 时的 stride 可能有行填充
                        var tmpData = bitmapPool.LockBits(
                            new Rectangle(0, 0, w, h),
                            ImageLockMode.ReadOnly,
                            PixelFormat.Format32bppArgb);
                        int stride = Math.Abs(tmpData.Stride);
                        bitmapPool.UnlockBits(tmpData);

                        rawBufferPool = new byte[stride * h];

                        poolWidth = w;
                        poolHeight = h;
                    }

                    // —— 真正抓图到 bitmapPool —— //
                    IntPtr hdcDest = graphicsPool.GetHdc();
                    IntPtr hdcSrc = GetWindowDC(hWnd);
                    BitBlt(hdcDest, 0, 0, w, h, hdcSrc, 0, 0, 0x00CC0020);
                    graphicsPool.ReleaseHdc(hdcDest);
                    ReleaseDC(hWnd, hdcSrc);

                    // —— 锁像素 + 拷到 rawBufferPool —— //
                    var bmpData = bitmapPool.LockBits(
                        new Rectangle(0, 0, w, h),
                        ImageLockMode.ReadOnly,
                        PixelFormat.Format32bppArgb);

                    Marshal.Copy(bmpData.Scan0, rawBufferPool, 0, rawBufferPool.Length);
                    bitmapPool.UnlockBits(bmpData);

                    if (uploadPool == null || uploadPool.Bounds.Width != w || uploadPool.Bounds.Height != h)
                    {
                        uploadPool?.Dispose();
                        uploadPool = new MemoryTextureUpload(w, h);
                        // ctor 里只分配 new Rgba32[w*h]
                    }

                    ConvertBgraToRgba32(rawBufferPool!, poolWidth, poolHeight, uploadPool!.PixelData);

                    lock (bufferLock)
                    {
                        pixelBuffer = uploadPool;
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

        private void ConvertBgraToRgba32(byte[] src, int width, int height, Span<Rgba32> dst)
        {
            int dstIdx = 0;

            for (int i = 0; i < src.Length; i += 4)
            {
                byte b = src[i + 0];
                byte g = src[i + 1];
                byte r = src[i + 2];

                byte a = 255;
                dst[dstIdx++] = new Rgba32(r, g, b, a);
            }
        }

        public BindableInt FrameRate { get; } = new BindableInt(60)
        {
            MinValue = 30,
            MaxValue = 360,
            Default = 60,
        };

        private double elapsedTime;

        protected override void Update()
        {
            base.Update();

            elapsedTime += Time.Elapsed;

            if (elapsedTime < 1000f / FrameRate.Value)
            {
                return;
            }

            elapsedTime = 0;

            // 1) 请求抓一帧
            captureRequest.Set();

            // 2) 等待抓取完成（同步），最多等 10ms 防止卡死，也可以改为无限等待
            if (!frameReady.WaitOne(0))
            {
                return;
            }

            if (!isWindowsLive)
            {
                this.FadeOut(100);
                return;
            }

            this.FadeIn(100);

            // 3) 消费像素缓冲区
            MemoryTextureUpload? frame;
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

            texture.SetData(frame);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            running = false;
            captureRequest.Set();
            captureThread?.Join();
            texture?.Dispose();
            bitmapPool?.Dispose();
            graphicsPool?.Dispose();
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
            IntPtr result = FindWindow(null, partialTitle);

            if (result != IntPtr.Zero)
                return result;

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
