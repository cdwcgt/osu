// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace osu.Game.Tournament.IPC.MemoryIPC
{
    [SupportedOSPlatform("windows")]
    public class MemoryReader : IDisposable
    {
        public IntPtr ProcessHandle { get; private set; }

        public Process? Process { get; private set; }

        public bool IsAttached => Process != null && !Process.HasExited;

        public bool AttachToProcessByTitleName(string titleName)
        {
            Process? p = WindowsAPI.GetProcessByWindowTitle(titleName, false);
            return p != null && AttachToProcess(p);
        }

        public virtual bool AttachToProcess(Process process)
        {
            this.Process = process;
            ProcessHandle = WindowsAPI.OpenProcess(WindowsAPI.ProcessAccessFlags.VMRead | WindowsAPI.ProcessAccessFlags.QueryInformation, false, process.Id);
            return ProcessHandle != IntPtr.Zero;
        }

        #region Basic Method

        public int ReadInt32(IntPtr address)
        {
            if (!IsAttached)
                throw new InvalidOperationException("Process is not attached or has exited.");

            byte[] buffer = new byte[4];
            WindowsAPI.ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out _);
            return BitConverter.ToInt32(buffer, 0);
        }

        public long ReadInt64(IntPtr address)
        {
            if (!IsAttached)
                throw new InvalidOperationException("Process is not attached or has exited.");

            byte[] buffer = new byte[8];
            WindowsAPI.ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out _);
            return BitConverter.ToInt64(buffer, 0);
        }

        public short ReadShort(IntPtr address)
        {
            if (!IsAttached)
                throw new InvalidOperationException("Process is not attached or has exited.");

            byte[] buffer = new byte[2];
            WindowsAPI.ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out _);
            return BitConverter.ToInt16(buffer, 0);
        }

        public float ReadFloat(IntPtr address)
        {
            if (!IsAttached)
                throw new InvalidOperationException("Process is not attached or has exited.");

            byte[] buffer = new byte[4];
            WindowsAPI.ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out _);
            return BitConverter.ToSingle(buffer, 0);
        }

        public double ReadDouble(IntPtr address)
        {
            if (!IsAttached)
                throw new InvalidOperationException("Process is not attached or has exited.");

            byte[] buffer = new byte[8];
            WindowsAPI.ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out _);
            return BitConverter.ToDouble(buffer, 0);
        }

        public byte[] ReadBytes(IntPtr address, int length)
        {
            if (!IsAttached)
                throw new InvalidOperationException("Process is not attached or has exited.");

            byte[] buffer = new byte[length];
            WindowsAPI.ReadProcessMemory(ProcessHandle, address, buffer, length, out _);
            return buffer;
        }

        public string ReadString(IntPtr address, int length)
        {
            if (!IsAttached)
                throw new InvalidOperationException("Process is not attached or has exited.");

            byte[] bytes = ReadBytes(address, length);
            return System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        }

        public T Read<T>(IntPtr address) where T : struct
        {
            if (!IsAttached)
                throw new InvalidOperationException("Process is not attached or has exited.");

            int ByteSize = Marshal.SizeOf(typeof(T));

            byte[] buffer = new byte[ByteSize];

            WindowsAPI.ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out _);

            return ByteArrayToStructure<T>(buffer);
        }

        public IntPtr GetModuleBase(string moduleName)
        {
            if (!IsAttached)
                throw new InvalidOperationException("Process is not attached or has exited.");

            foreach (ProcessModule mod in Process!.Modules)
            {
                if (mod.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    return mod.BaseAddress;
                }
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (ProcessHandle != IntPtr.Zero)
                WindowsAPI.CloseHandle(ProcessHandle);
        }

        #endregion

        #region Conversion

        // https://stackoverflow.com/a/50672487

        private static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

            try
            {
                return (T)(Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T)) ?? throw new InvalidOperationException());
            }
            finally
            {
                handle.Free();
            }
        }

        // maybe useless unless we write memory :)
        private static byte[] StructureToByteArray(object obj)
        {
            int length = Marshal.SizeOf(obj);

            byte[] array = new byte[length];

            IntPtr pointer = Marshal.AllocHGlobal(length);

            Marshal.StructureToPtr(obj, pointer, true);
            Marshal.Copy(pointer, array, 0, length);
            Marshal.FreeHGlobal(pointer);

            return array;
        }

        #endregion

        #region Pattern Scan

        public IntPtr? ResolveFromPatternInfo(PatternInfo pattern, IEnumerable<MemoryRegion>? regions = null)
        {
            IntPtr? baseAddress = (regions != null)
                ? FindPattern(regions, pattern.Pattern)
                : FindPattern(pattern.Pattern);

            if (baseAddress == null)
                return null;

            return baseAddress + pattern.Offset;
        }

        public static IntPtr? FindPattern(IntPtr processHandle, byte?[] pattern)
        {
            var regions = QueryMemoryRegions(processHandle);
            return FindPattern(regions, processHandle, pattern);
        }

        public static IntPtr? FindPattern(IEnumerable<MemoryRegion> regions, IntPtr processHandle, byte?[] pattern)
        {
            const int buffer_size = 64 * 1024;
            int patternLength = pattern.Length;

            // 保留该块到下一块
            // 设置为 patternLength - 1 后总能让新块的第一个字节开始进行匹配
            int headSize = patternLength - 1;

            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer_size + headSize);

            var handle = GCHandle.Alloc(sharedBuffer, GCHandleType.Pinned);

            try
            {
                foreach (var region in regions)
                {
                    IntPtr regionStart = region.BaseAddress;
                    int regionSize = region.RegionSize.ToInt32();

                    int copiedTail = 0;

                    for (int offset = 0; offset < regionSize; offset += buffer_size)
                    {
                        // 在跨块的时候复制保留的数据到数组前
                        if (copiedTail > 0)
                            Array.Copy(sharedBuffer, buffer_size, sharedBuffer, 0, copiedTail);

                        int readSize = Math.Min(buffer_size + headSize - copiedTail, regionSize - offset);

                        int bytesRead;

                        IntPtr targetPtr = Marshal.UnsafeAddrOfPinnedArrayElement(sharedBuffer, copiedTail);
                        if (!WindowsAPI.ReadProcessMemory(processHandle, regionStart + offset, targetPtr, readSize, out bytesRead)
                            || bytesRead < patternLength - copiedTail)
                            continue;

                        int totalSize = bytesRead + copiedTail;
                        int maxIndex = totalSize - patternLength + 1;

                        // 滑动窗口查找
                        for (int i = 0; i < maxIndex; i++)
                        {
                            bool matched = true;

                            for (int j = 0; j < patternLength; j++)
                            {
                                if (pattern[j] != null && sharedBuffer[i + j] != pattern[j])
                                {
                                    matched = false;
                                    break;
                                }
                            }

                            if (matched)
                                return new IntPtr(regionStart + offset + i - copiedTail);
                        }

                        // 实际逻辑上 bytesRead 不会小于 headSize
                        if (bytesRead >= headSize)
                        {
                            Array.Copy(sharedBuffer, totalSize - headSize, sharedBuffer, buffer_size, headSize);
                            copiedTail = headSize;
                        }
                        else
                        {
                            copiedTail = 0;
                        }
                    }
                }

                return null;
            }
            finally
            {
                handle.Free();
                ArrayPool<byte>.Shared.Return(sharedBuffer);
            }
        }

        public IntPtr? FindPattern(byte?[] pattern) => FindPattern(ProcessHandle, pattern);
        public IntPtr? FindPattern(IEnumerable<MemoryRegion> regions, byte?[] pattern) => FindPattern(regions, ProcessHandle, pattern);

        public static List<MemoryRegion> QueryMemoryRegions(IntPtr processHandle)
        {
            List<MemoryRegion> regions = new List<MemoryRegion>();
            IntPtr address = IntPtr.Zero;

            while (true)
            {
                WindowsAPI.MEMORY_BASIC_INFORMATION memInfo;
                int result = WindowsAPI.VirtualQueryEx(processHandle, address, out memInfo, (uint)Marshal.SizeOf(typeof(WindowsAPI.MEMORY_BASIC_INFORMATION)));
                if (result == 0)
                    break;

                bool isCommitted = (memInfo.State & 0x1000) != 0; // MEM_COMMIT
                bool isReadable =
                    (memInfo.Protect & 0x04) != 0 || // PAGE_READWRITE
                    (memInfo.Protect & 0x40) != 0; // PAGE_EXECUTE_READWRITE

                if (isCommitted && isReadable)
                {
                    regions.Add(new MemoryRegion
                    {
                        BaseAddress = address,
                        RegionSize = memInfo.RegionSize,
                    });
                }

                address = new IntPtr(memInfo.BaseAddress.ToInt64() + (long)memInfo.RegionSize);
            }

            return regions;
        }

        #endregion
    }

    public class MemoryRegion
    {
        public IntPtr BaseAddress;
        public IntPtr RegionSize;
    }

    public class PatternInfo
    {
        public byte?[] Pattern;
        public int Offset;

        public PatternInfo(string pattern, int offset = 0)
        {
            Pattern = ParsePattern(pattern);
            Offset = offset;
        }

        public PatternInfo(byte?[] pattern, int offset = 0)
        {
            Pattern = pattern;
            Offset = offset;
        }

        /// <summary>
        /// 用于将形如 "89 ?? ?? ?? 8B ?? ??" 的字符串解析为 pattern 数组
        /// </summary>
        public static byte?[] ParsePattern(string pattern)
        {
            string[] tokens = pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            List<byte?> result = new List<byte?>();

            foreach (string token in tokens)
            {
                if (token == "??" || token == "?")
                    result.Add(null);
                else
                    result.Add(Convert.ToByte(token, 16));
            }

            return result.ToArray();
        }
    }
}
