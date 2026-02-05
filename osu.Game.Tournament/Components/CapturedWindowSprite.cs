// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Logging;
using osu.Game.Tournament.Models;
using Vortice.Direct3D11;
using FillMode = osu.Framework.Graphics.FillMode;

namespace osu.Game.Tournament.Components
{
    [SupportedOSPlatform("windows10.0.19041.0")]
    public partial class CapturedWindowSprite : CompositeDrawable
    {
        private Sprite sprite = null!;
        private readonly string targetWindowTitle;
        private WgcCapture? capture;
        private D3D11ExternalTexture? externalTexture;
        private ID3D11Texture2D? pendingTexture;
        private int pendingWidth;
        private int pendingHeight;
        private IntPtr targetHwnd;
        private bool d3d11Available;
        private Thread? windowWatcherThread;
        private volatile bool watcherRunning;
        private IntPtr watchedHwnd;
        private volatile bool watchedAlive;

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

            if (ladder != null)
            {
                FrameRate.BindTo(ladder.FrameRate);
            }

            d3d11Available = D3D11Interop.TryGetD3D11Device(renderer, out var device, out _, out _);

            if (d3d11Available)
                capture = new WgcCapture(device);

            watcherRunning = true;
            windowWatcherThread = new Thread(watchWindowLoop)
            {
                IsBackground = true,
                Name = $"WindowWatcher<{targetWindowTitle}>"
            };
            windowWatcherThread.Start();
        }

        [Resolved]
        private IRenderer renderer { get; set; } = null!;

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

            if (!d3d11Available || capture == null)
                return;

            if (targetHwnd == IntPtr.Zero || !IsWindow(targetHwnd))
            {
                if (capture.IsRunning)
                    capture.Stop();

                targetHwnd = watchedHwnd;

                if (targetHwnd != IntPtr.Zero && watchedAlive)
                {
                    try
                    {
                        capture.StartForWindow(targetHwnd);
                        isWindowsLive = true;
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "Capture Error");
                        isWindowsLive = false;
                    }
                }
                else
                {
                    isWindowsLive = false;
                }
            }

            if (!isWindowsLive)
            {
                this.FadeOut(100);
                return;
            }

            this.FadeIn(100);

            if (capture.TryAcquireLatestTexture(out var texture, out int w, out int h))
            {
                var old = Interlocked.Exchange(ref pendingTexture, texture);
                old?.Release();

                pendingWidth = w;
                pendingHeight = h;
            }
        }

        private void consumePendingFrame()
        {
            var texture = Interlocked.Exchange(ref pendingTexture, null);
            if (texture == null)
                return;

            try
            {
                if (pendingWidth <= 0 || pendingHeight <= 0)
                    return;

                if (externalTexture == null || externalTexture.Width != pendingWidth || externalTexture.Height != pendingHeight)
                {
                    externalTexture?.Dispose();
                    externalTexture = new D3D11ExternalTexture(renderer, pendingWidth, pendingHeight);
                    sprite.Texture = externalTexture;
                }

                externalTexture.UpdateFrom(texture);
            }
            finally
            {
                texture.Release();
            }
        }

        protected override DrawNode CreateDrawNode() => new CaptureDrawNode(this);

        private sealed class CaptureDrawNode : CompositeDrawableDrawNode
        {
            public CaptureDrawNode(CapturedWindowSprite source)
                : base(source)
            {
            }

            protected override void Draw(IRenderer renderer)
            {
                ((CapturedWindowSprite)Source).consumePendingFrame();
                base.Draw(renderer);
            }
        }

        private void watchWindowLoop()
        {
            while (watcherRunning)
            {
                try
                {
                    IntPtr hwnd = watchedHwnd;

                    if (hwnd != IntPtr.Zero && !IsWindow(hwnd))
                    {
                        watchedHwnd = IntPtr.Zero;
                        watchedAlive = false;
                    }

                    if (watchedHwnd == IntPtr.Zero)
                    {
                        hwnd = FindWindowByPartialTitle(targetWindowTitle);
                        watchedHwnd = hwnd;
                        watchedAlive = hwnd != IntPtr.Zero;
                    }
                    else
                    {
                        watchedAlive = true;
                    }
                }
                catch
                {
                    watchedHwnd = IntPtr.Zero;
                    watchedAlive = false;
                }

                Thread.Sleep(500);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            capture?.Dispose();
            externalTexture?.Dispose();
            var old = Interlocked.Exchange(ref pendingTexture, null);
            old?.Release();

            watcherRunning = false;
            windowWatcherThread?.Join();
        }

        #region Windows API

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

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
