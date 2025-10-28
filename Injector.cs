using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Injector
{
    public static class Injector
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandleEx(uint dwFlags, string lpModuleName, out IntPtr phModule);
        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EnumProcessModules(IntPtr hProcess, [Out] IntPtr[] lphModule, uint cb, out uint lpcbNeeded);
        [DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint GetModuleBaseName(IntPtr hProcess, IntPtr hModule, StringBuilder lpBaseName, uint nSize);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);
        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint PAGE_READWRITE = 0x04;
        private const uint INFINITE = 0xFFFFFFFF;
        private const uint DONT_RESOLVE_DLL_REFERENCES = 0x00000001;
        public static int GetProcessIdByName(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName.Replace(".exe", ""));
            return processes.Length > 0 ? processes[0].Id : 0;
        }
        public static bool InjectDLL(int processId, string dllPath)
        {
            string dllLibFullPath = Path.GetFullPath(dllPath);
            if (!File.Exists(dllLibFullPath))
            {
                PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Cannot find DLL: {dllPath}\n";
                return false;
            }
            PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Opening process with PID: {processId}\n";
            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Failed to open process handle. Error: {error}\n";
                return false;
            }
            PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Process handle opened successfully\n";
            IntPtr lpBaseAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)dllLibFullPath.Length + 1, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (lpBaseAddress == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Memory allocation failed. Error: {error}\n";
                CloseHandle(hProcess);
                return false;
            }
            PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Memory allocated at: 0x{lpBaseAddress.ToInt64():X}\n";
            byte[] dllPathBytes = Encoding.ASCII.GetBytes(dllLibFullPath);
            if (!WriteProcessMemory(hProcess, lpBaseAddress, dllPathBytes, (uint)dllPathBytes.Length, out _))
            {
                int error = Marshal.GetLastWin32Error();
                PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Failed to write to process memory. Error: {error}\n";
                CloseHandle(hProcess);
                return false;
            }
            PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] DLL path written to process memory\n";
            IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] LoadLibraryA address: 0x{loadLibraryAddr.ToInt64():X}\n";
            IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, lpBaseAddress, 0, IntPtr.Zero);
            bool success = false;
            if (hThread != IntPtr.Zero)
            {
                PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Remote thread created successfully\n";
                WaitForSingleObject(hThread, INFINITE);
                success = true;
                CloseHandle(hThread);
                PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Remote thread finished execution\n";
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Failed to create remote thread. Error: {error}\n";
            }
            if (success)
            {
                PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Starting PhasmoCheatVThread...\n";
                StartPhasmoCheatThread(hProcess, dllLibFullPath);
            }

            CloseHandle(hProcess);
            PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Process handle closed\n";
            return success;
        }
        private static void StartPhasmoCheatThread(IntPtr hProcess, string dllPath)
        {
            try
            {
                IntPtr hModule = GetRemoteModuleHandle(hProcess, Path.GetFileName(dllPath));
                if (hModule == IntPtr.Zero)
                {
                    PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Failed to get module handle in target process\n";
                    return;
                }
                PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Module handle obtained: 0x{hModule.ToInt64():X}\n";
                IntPtr threadProc = GetRemoteExportAddress(dllPath, "PhasmoCheatVThread", hModule);
                if (threadProc == IntPtr.Zero)
                {
                    PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Failed to get thread procedure address\n";
                    return;
                }
                PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Thread procedure address: 0x{threadProc.ToInt64():X}\n";
                IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, threadProc, IntPtr.Zero, 0, IntPtr.Zero);
                if (hThread != IntPtr.Zero)
                {
                    CloseHandle(hThread);
                    PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] PhasmoCheatVThread started successfully!\n";
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Failed to create thread. Error: {error}\n";
                }
            }
            catch (Exception ex)
            {
                PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Error starting PhasmoCheatVThread: {ex.Message}\n";
            }
        }
        private static IntPtr GetRemoteModuleHandle(IntPtr hProcess, string moduleName)
        {
            IntPtr[] modules = new IntPtr[1024];
            uint cbNeeded;

            if (!EnumProcessModules(hProcess, modules, (uint)(IntPtr.Size * modules.Length), out cbNeeded))
            {
                return IntPtr.Zero;
            }
            uint moduleCount = cbNeeded / (uint)IntPtr.Size;
            for (int i = 0; i < moduleCount; i++)
            {
                StringBuilder moduleNameBuilder = new StringBuilder(260);
                if (GetModuleBaseName(hProcess, modules[i], moduleNameBuilder, (uint)moduleNameBuilder.Capacity) > 0)
                {
                    if (moduleNameBuilder.ToString().Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                    {
                        return modules[i];
                    }
                }
            }
            return IntPtr.Zero;
        }
        private static IntPtr GetRemoteExportAddress(string modulePath, string exportName, IntPtr remoteBase)
        {
            IntPtr localModule = LoadLibraryEx(modulePath, IntPtr.Zero, DONT_RESOLVE_DLL_REFERENCES);
            if (localModule == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Failed to load local copy of DLL. Error: {error}\n";
                return IntPtr.Zero;
            }
            IntPtr localExport = GetProcAddress(localModule, exportName);
            if (localExport == IntPtr.Zero)
            {
                FreeLibrary(localModule);
                PhasmoCheatV_Loader.MainForm.logs += $"\r\n[{DateTime.Now:HH:mm:ss}] Export not found in DLL: {exportName}\n";
                return IntPtr.Zero;
            }
            long offset = localExport.ToInt64() - localModule.ToInt64();
            FreeLibrary(localModule);
            return new IntPtr(remoteBase.ToInt64() + offset);
        }
    }
}