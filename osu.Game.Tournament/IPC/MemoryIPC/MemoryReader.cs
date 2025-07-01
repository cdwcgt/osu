// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace osu.Game.Tournament.IPC.MemoryIPC
{
    [SupportedOSPlatform("windows")]
    public class MemoryReader
    {
        private IntPtr processHandle;
        private Process? process;

        public Process? Process => process;

        public bool IsAttached => process != null && !process.HasExited;

        public bool AttachToProcess(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            return processes.Length != 0 && AttachToProcess(processes[0]);
        }

        public bool AttachToProcessByTitleName(string titleName)
        {
            Process? p = WindowsAPI.GetProcessByWindowTitle(titleName);
            return p != null && AttachToProcess(p);
        }

        public bool AttachToProcess(Process process)
        {
            processHandle = WindowsAPI.OpenProcess(WindowsAPI.ProcessAccessFlags.VMRead | WindowsAPI.ProcessAccessFlags.QueryInformation, false, process.Id);
            return processHandle != IntPtr.Zero;
        }

        #region Basic Method

        public int ReadInt32(IntPtr address)
        {
            if (!IsAttached)
                throw new InvalidOperationException("Process is not attached or has exited.");

            byte[] buffer = new byte[4];
            WindowsAPI.ReadProcessMemory(processHandle, address, buffer, buffer.Length, out _);
            return BitConverter.ToInt32(buffer, 0);
        }

        public short ReadShort(IntPtr address)
        {
            if (!IsAttached)
                throw new InvalidOperationException("Process is not attached or has exited.");

            byte[] buffer = new byte[2];
            WindowsAPI.ReadProcessMemory(processHandle, address, buffer, buffer.Length, out _);
            return BitConverter.ToInt16(buffer, 0);
        }

        public float ReadFloat(IntPtr address)
        {
            if (!IsAttached)
                throw new InvalidOperationException("Process is not attached or has exited.");

            byte[] buffer = new byte[4];
            WindowsAPI.ReadProcessMemory(processHandle, address, buffer, buffer.Length, out _);
            return BitConverter.ToSingle(buffer, 0);
        }

        public double ReadDouble(IntPtr address)
        {
            if (!IsAttached)
                throw new InvalidOperationException("Process is not attached or has exited.");

            byte[] buffer = new byte[8];
            WindowsAPI.ReadProcessMemory(processHandle, address, buffer, buffer.Length, out _);
            return BitConverter.ToDouble(buffer, 0);
        }

        public byte[] ReadBytes(IntPtr address, int length)
        {
            if (!IsAttached)
                throw new InvalidOperationException("Process is not attached or has exited.");

            byte[] buffer = new byte[length];
            WindowsAPI.ReadProcessMemory(processHandle, address, buffer, length, out _);
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

            WindowsAPI.ReadProcessMemory(processHandle, address, buffer, buffer.Length, out _);

            return ByteArrayToStructure<T>(buffer);
        }

        public IntPtr GetModuleBase(string moduleName)
        {
            if (!IsAttached)
                throw new InvalidOperationException("Process is not attached or has exited.");

            foreach (ProcessModule mod in process.Modules)
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
            if (processHandle != IntPtr.Zero)
                WindowsAPI.CloseHandle(processHandle);
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

        public IntPtr? ResolveFromPatternInfo(PatternInfo pattern)
        {
            IntPtr? baseAddress = FindPattern(pattern.Pattern);

            if (baseAddress == null)
                return null;

            return baseAddress + pattern.Offset;
        }

        public static IntPtr? FindPattern(IntPtr processHandle, byte?[] pattern)
        {
            var regions = QueryMemoryRegions(processHandle);

            foreach (var region in regions)
            {
                int size = region.RegionSize.ToInt32();

                byte[] buffer = new byte[size];
                if (!WindowsAPI.ReadProcessMemory(processHandle, region.BaseAddress, buffer, size, out int _))
                    continue;

                for (int i = 0; i <= buffer.Length - pattern.Length; i++)
                {
                    bool matched = true;

                    for (int j = 0; j < pattern.Length; j++)
                    {
                        if (pattern[j] != null && buffer[i + j] != pattern[j])
                        {
                            matched = false;
                            break;
                        }
                    }

                    if (matched)
                    {
                        return region.BaseAddress + i;
                    }
                }
            }

            return null;
        }

        public IntPtr? FindPattern(byte?[] pattern) => FindPattern(processHandle, pattern);

        public static List<MemoryRegion> QueryMemoryRegions(IntPtr processHandle)
        {
            var regions = new List<MemoryRegion>();
            IntPtr address = IntPtr.Zero;
            int mbiSize = Marshal.SizeOf<WindowsAPI.MEMORY_BASIC_INFORMATION>();

            while (true)
            {
                if (WindowsAPI.VirtualQueryEx(processHandle, address, out var mbi, (uint)mbiSize) == 0)
                    break;

                bool isReadable = ((mbi.Protect & (uint)WindowsAPI.AllocationProtect.PAGE_GUARD) == 0) &&
                                  ((mbi.Protect & (uint)WindowsAPI.AllocationProtect.PAGE_NOACCESS) == 0);

                if (mbi.State == 0x1000 /* MEM_COMMIT */ && isReadable)
                {
                    regions.Add(new MemoryRegion
                    {
                        BaseAddress = mbi.BaseAddress,
                        RegionSize = mbi.RegionSize
                    });
                }

                // 下一段区域
                address = IntPtr.Add(mbi.BaseAddress, (int)mbi.RegionSize);
            }

            return regions;
        }

        public class MemoryRegion
        {
            public IntPtr BaseAddress;
            public IntPtr RegionSize;
        }

        #endregion
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
