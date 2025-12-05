// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
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
        private ArrayPoolTextureUpload? uploadPool;
        private int poolWidth, poolHeight;

        // 同步信号
        private readonly AutoResetEvent captureRequest = new AutoResetEvent(false);
        private readonly AutoResetEvent frameReady = new AutoResetEvent(false);

        // 当前窗口尺寸 & 像素缓冲区
        private int currentWidth, currentHeight;
        private ArrayPoolTextureUpload? pixelBuffer;
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
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Name = $"WindowCapture<{targetWindowTitle}>";
            sprite = new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fit,
            };

            AddInternal(sprite);

            AlwaysPresent = true;

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
            IntPtr hWnd = WindowsAPI.FindWindowByPartialTitle(targetWindowTitle);

            // 预先查一次 HWND
            while (running)
            {
                // 等待 Update 发起请求
                captureRequest.WaitOne();

                if (!running) break;

                if (hWnd != IntPtr.Zero && !WindowsAPI.IsWindow(hWnd))
                {
                    isWindowsLive = false;
                    hWnd = IntPtr.Zero;
                    lock (bufferLock)
                        pixelBuffer = null;
                    frameReady.Set();
                }

                if (hWnd == IntPtr.Zero)
                {
                    hWnd = WindowsAPI.FindWindowByPartialTitle(targetWindowTitle);

                    Thread.Sleep(100);
                    continue;
                }

                isWindowsLive = true;

                try
                {
                    WindowsAPI.GetWindowRect(hWnd, out WindowsAPI.RECT rect);
                    int w = rect.Right - rect.Left;
                    int h = rect.Bottom - rect.Top;

                    if (w <= 0 || h <= 0)
                    {
                        frameReady.Set();
                        continue;
                    }

                    if (bitmapPool == null || graphicsPool == null || rawBufferPool == null || poolWidth != w || poolHeight != h)
                    {
                        bitmapPool?.Dispose();
                        graphicsPool?.Dispose();

                        bitmapPool = new Bitmap(w, h, PixelFormat.Format24bppRgb);
                        graphicsPool = System.Drawing.Graphics.FromImage(bitmapPool);

                        // 注意 LockBits 时的 stride 可能有行填充
                        var tmpData = bitmapPool.LockBits(
                            new Rectangle(0, 0, w, h),
                            ImageLockMode.ReadOnly,
                            PixelFormat.Format24bppRgb);
                        int stride = Math.Abs(tmpData.Stride);
                        bitmapPool.UnlockBits(tmpData);

                        rawBufferPool = new byte[stride * h];

                        poolWidth = w;
                        poolHeight = h;
                    }

                    IntPtr hdcDest = graphicsPool.GetHdc();
                    IntPtr hdcSrc = WindowsAPI.GetWindowDC(hWnd);
                    WindowsAPI.BitBlt(hdcDest, 0, 0, w, h, hdcSrc, 0, 0, 0x00CC0020);
                    graphicsPool.ReleaseHdc(hdcDest);
                    WindowsAPI.ReleaseDC(hWnd, hdcSrc);

                    var bmpData = bitmapPool.LockBits(
                        new Rectangle(0, 0, w, h),
                        ImageLockMode.ReadOnly,
                        PixelFormat.Format24bppRgb);

                    Marshal.Copy(bmpData.Scan0, rawBufferPool, 0, rawBufferPool.Length);
                    bitmapPool.UnlockBits(bmpData);

                    uploadPool = new ArrayPoolTextureUpload(w, h);

                    convertRgr24ToRgba32(rawBufferPool, uploadPool!.RawData);

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

        private static void convertRgr24ToRgba32(byte[] src, Span<Rgba32> dst)
        {
            int dstIdx = 0;

            for (int i = 0; i < src.Length; i += 3)
            {
                byte b = src[i + 0];
                byte g = src[i + 1];
                byte r = src[i + 2];

                const byte a = 255;
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

            captureRequest.Set();

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

            ArrayPoolTextureUpload? frame;
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

            if (texture == null)
            {
                texture?.Dispose();
                texture = renderer.CreateTexture(w, h);
                texture.BypassTextureUploadQueueing = true;
                sprite.Texture = texture;
            }

            if (texture.Width != w || texture.Height != h)
            {
                texture.Width = w;
                texture.Height = h;
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
        }
    }
}
