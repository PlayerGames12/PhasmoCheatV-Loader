using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PhasmoCheatV_Loader
{
    public class MemoryScanner
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_QUERY_INFORMATION = 0x0400;

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_INFO
        {
            public ushort processorArchitecture;
            ushort reserved;
            public uint pageSize;
            public IntPtr minimumApplicationAddress;
            public IntPtr maximumApplicationAddress;
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        public static string FindVersion(string processName, string versionSignature)
        {
            Process[] procs = Process.GetProcessesByName(processName);
            if (procs.Length == 0) return null;

            Process proc = procs[0];
            IntPtr hProcess = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, proc.Id);
            if (hProcess == IntPtr.Zero) return null;

            byte[] pattern = SignatureToBytes(versionSignature);

            GetSystemInfo(out SYSTEM_INFO sysInfo);
            IntPtr addr = sysInfo.minimumApplicationAddress;
            IntPtr maxAddr = sysInfo.maximumApplicationAddress;

            MEMORY_BASIC_INFORMATION mbi;

            while (addr.ToInt64() < maxAddr.ToInt64())
            {
                if (VirtualQueryEx(hProcess, addr, out mbi, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) == 0)
                    break;

                bool readable = (mbi.State == 0x1000) && // MEM_COMMIT
                                ((mbi.Protect & 0x04) != 0 || // PAGE_READWRITE
                                 (mbi.Protect & 0x02) != 0 || // PAGE_READONLY
                                 (mbi.Protect & 0x20) != 0 || // PAGE_EXECUTE_READ
                                 (mbi.Protect & 0x40) != 0) && // PAGE_EXECUTE_READWRITE
                                ((mbi.Protect & 0x100) == 0); // not PAGE_GUARD

                if (readable)
                {
                    long regionSize = mbi.RegionSize.ToInt64();
                    byte[] buffer = new byte[regionSize];
                    if (ReadProcessMemory(hProcess, mbi.BaseAddress, buffer, buffer.Length, out int bytesRead))
                    {
                        int index = FindPattern(buffer, pattern);
                        if (index >= 0)
                        {
                            StringBuilder version = new StringBuilder();
                            for (int i = index; i < index + 32 && i < bytesRead; i++)
                            {
                                if (buffer[i] == 0) break;
                                version.Append((char)buffer[i]);
                            }

                            CloseHandle(hProcess);
                            return version.ToString();
                        }
                    }
                }

                addr = new IntPtr(mbi.BaseAddress.ToInt64() + mbi.RegionSize.ToInt64());
            }

            CloseHandle(hProcess);
            return null;
        }

        private static byte[] SignatureToBytes(string signature)
        {
            string[] parts = signature.Split(' ');
            byte[] bytes = new byte[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                bytes[i] = Convert.ToByte(parts[i], 16);
            return bytes;
        }

        private static int FindPattern(byte[] buffer, byte[] pattern)
        {
            for (int i = 0; i <= buffer.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (pattern[j] != 0x00 && buffer[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }
    }
}
