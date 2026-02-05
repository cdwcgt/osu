// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using Windows.Foundation.Metadata;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;
using osu.Framework.Extensions.ObjectExtensions;
using SharpGen.Runtime;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace osu.Game.Tournament.Components
{
    [SupportedOSPlatform("windows10.0.19041.0")]
    public sealed class WgcCapture : IDisposable
    {
        private const int frame_buffer_count = 1;

        private readonly ID3D11Device d3dDevice;
        private readonly IDirect3DDevice winrtDevice;

        private GraphicsCaptureItem? item;
        private Direct3D11CaptureFramePool? framePool;
        private GraphicsCaptureSession? session;
        private SizeInt32 lastSize;

        private ID3D11Texture2D? latestTexture;
        private int latestWidth;
        private int latestHeight;

        public bool IsRunning { get; private set; }

        public WgcCapture(ID3D11Device device)
        {
            d3dDevice = device ?? throw new ArgumentNullException(nameof(device));
            winrtDevice = createDirect3DDevice(d3dDevice);
        }

        public void StartForWindow(IntPtr hwnd)
        {
            if (!GraphicsCaptureSession.IsSupported())
                throw new NotSupportedException("Windows Graphics Capture is not supported on this system.");

            var captureItem = createItemForWindow(hwnd);
            startInternal(captureItem);
        }

        public void StartForMonitor(IntPtr hmonitor)
        {
            if (!GraphicsCaptureSession.IsSupported())
                throw new NotSupportedException("Windows Graphics Capture is not supported on this system.");

            var captureItem = createItemForMonitor(hmonitor);
            startInternal(captureItem);
        }

        public void Stop()
        {
            IsRunning = false;

            if (framePool != null)
                framePool.FrameArrived -= onFrameArrived;

            session?.Dispose();
            session = null;

            framePool?.Dispose();
            framePool = null;

            releaseWinrtObject(item);
            item = null;

            var old = Interlocked.Exchange(ref latestTexture, null);
            old?.Release();
        }

        public bool TryAcquireLatestTexture(out ID3D11Texture2D texture, out int width, out int height)
        {
            texture = null!;
            width = 0;
            height = 0;

            var current = Volatile.Read(ref latestTexture);
            if (current == null)
                return false;

            current.AddRef();
            width = Volatile.Read(ref latestWidth);
            height = Volatile.Read(ref latestHeight);
            texture = current;
            return true;
        }

        private void startInternal(GraphicsCaptureItem captureItem)
        {
            Stop();

            item = captureItem;
            lastSize = captureItem.Size;

            framePool = createFramePool(winrtDevice, lastSize);

            framePool.FrameArrived += onFrameArrived;
            session = framePool.CreateCaptureSession(captureItem);
            session.IsCursorCaptureEnabled = false;

            if (ApiInformation.IsPropertyPresent(
                    "Windows.Graphics.Capture.GraphicsCaptureSession",
                    nameof(GraphicsCaptureSession.IsBorderRequired)))
            {
                session.IsBorderRequired = false;
            }

            session.StartCapture();
            IsRunning = true;
        }

        private void onFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            Direct3D11CaptureFrame? frame = null;

            while (true)
            {
                var next = sender.TryGetNextFrame();
                if (next == null)
                    break;

                frame?.Dispose();
                frame = next;
            }

            if (frame == null)
                return;

            using (frame)
            {
                var size = frame.ContentSize;

                if (size.Width != lastSize.Width || size.Height != lastSize.Height)
                {
                    lastSize = size;
                    sender.Recreate(winrtDevice, DirectXPixelFormat.B8G8R8A8UIntNormalized, frame_buffer_count, size);
                }

                var texture = getTextureFromSurface(frame.Surface);
                var old = Interlocked.Exchange(ref latestTexture, texture);
                old?.Release();

                Volatile.Write(ref latestWidth, size.Width);
                Volatile.Write(ref latestHeight, size.Height);
            }
        }

        private static Direct3D11CaptureFramePool createFramePool(IDirect3DDevice device, SizeInt32 size)
        {
            try
            {
                return Direct3D11CaptureFramePool.CreateFreeThreaded(
                    device,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    frame_buffer_count,
                    size);
            }
            catch
            {
                return Direct3D11CaptureFramePool.Create(
                    device,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    frame_buffer_count,
                    size);
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private static void releaseWinrtObject(object? obj)
        {
            if (obj == null)
                return;

            if (obj is IDisposable disposable)
            {
                disposable.Dispose();
                return;
            }

            if (Marshal.IsComObject(obj))
                Marshal.FinalReleaseComObject(obj);
        }

        private static IDirect3DDevice createDirect3DDevice(ID3D11Device device)
        {
            using var dxgiDevice = device.QueryInterface<IDXGIDevice>();
            int hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out var devicePtr);
            Marshal.ThrowExceptionForHR(hr);

            try
            {
                return MarshalInterface<IDirect3DDevice>.FromAbi(devicePtr);
            }
            finally
            {
                Marshal.Release(devicePtr);
            }
        }

        private static ID3D11Texture2D getTextureFromSurface(IDirect3DSurface surface)
        {
            using var surfaceRef = MarshalInterface<IDirect3DSurface>.CreateMarshaler(surface);
            IntPtr unknown = surfaceRef.ThisPtr;

            Guid iid = dxgi_interface_access_iid;
            int hr = Marshal.QueryInterface(unknown, ref iid, out var accessPtr);
            Marshal.ThrowExceptionForHR(hr);

            try
            {
                var access = (IDirect3DDxgiInterfaceAccess)Marshal.GetObjectForIUnknown(accessPtr);
                Guid textureIid = typeof(ID3D11Texture2D).GUID;
                hr = access.GetInterface(ref textureIid, out var resourcePtr);
                Marshal.ThrowExceptionForHR(hr);

                return MarshallingHelpers.FromPointer<ID3D11Texture2D>(resourcePtr).AsNonNull();
            }
            finally
            {
                Marshal.Release(accessPtr);
            }
        }

        private static GraphicsCaptureItem createItemForWindow(IntPtr hwnd)
        {
            var interop = getCaptureItemInterop();
            Guid iid = graphics_capture_item_iid;
            int hr = interop.CreateForWindow(hwnd, ref iid, out var result);
            Marshal.ThrowExceptionForHR(hr);

            var item = MarshalInterface<GraphicsCaptureItem>.FromAbi(result);
            Marshal.Release(result);
            return item;
        }

        private static GraphicsCaptureItem createItemForMonitor(IntPtr hmonitor)
        {
            var interop = getCaptureItemInterop();
            Guid iid = graphics_capture_item_iid;
            int hr = interop.CreateForMonitor(hmonitor, ref iid, out var result);
            Marshal.ThrowExceptionForHR(hr);

            var item = MarshalInterface<GraphicsCaptureItem>.FromAbi(result);
            Marshal.Release(result);
            return item;
        }

        [DllImport("d3d11.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

        [DllImport("combase.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int RoGetActivationFactory(IntPtr activatableClassId, ref Guid iid, out IntPtr factory);

        [DllImport("combase.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern int WindowsCreateString(string sourceString, int length, out IntPtr hstring);

        [DllImport("combase.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int WindowsDeleteString(IntPtr hstring);

        [ComImport]
        [System.Runtime.InteropServices.Guid("3628e81b-3cac-4c60-b7f4-23ce0e0c3356")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IGraphicsCaptureItemInterop
        {
            [PreserveSig]
            int CreateForWindow(IntPtr window, ref Guid iid, out IntPtr result);

            [PreserveSig]
            int CreateForMonitor(IntPtr monitor, ref Guid iid, out IntPtr result);
        }

        [ComImport]
        [System.Runtime.InteropServices.Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDirect3DDxgiInterfaceAccess
        {
            [PreserveSig]
            int GetInterface(ref Guid iid, out IntPtr resource);
        }

        private static readonly Guid graphics_capture_item_iid = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760");
        private static readonly Guid dxgi_interface_access_iid = new Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1");

        private static IGraphicsCaptureItemInterop getCaptureItemInterop()
        {
            IntPtr hstring = IntPtr.Zero;
            IntPtr factoryPtr = IntPtr.Zero;
            Guid iid = typeof(IGraphicsCaptureItemInterop).GUID;

            try
            {
                int hr = WindowsCreateString("Windows.Graphics.Capture.GraphicsCaptureItem", "Windows.Graphics.Capture.GraphicsCaptureItem".Length, out hstring);
                Marshal.ThrowExceptionForHR(hr);

                hr = RoGetActivationFactory(hstring, ref iid, out factoryPtr);
                Marshal.ThrowExceptionForHR(hr);

                return (IGraphicsCaptureItemInterop)Marshal.GetObjectForIUnknown(factoryPtr);
            }
            finally
            {
                if (factoryPtr != IntPtr.Zero)
                    Marshal.Release(factoryPtr);
                if (hstring != IntPtr.Zero)
                    WindowsDeleteString(hstring);
            }
        }
    }
}
