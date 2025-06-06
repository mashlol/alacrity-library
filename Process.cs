using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Alacrity.Library {

    public class Process {

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern bool CreateProcessA(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EnumProcesses(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
            uint[] lpidProcess,
            uint cb,
            out uint lpcbNeeded);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            uint processAccess,
            bool bInheritHandle,
            uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int GetCurrentProcessId();

        [DllImport("psapi.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern uint GetModuleBaseNameA(
            IntPtr hProcess,
            IntPtr hModule,
            [Out] StringBuilder lpBaseName,
            uint nSize);

        private readonly PROCESS_INFORMATION processInformation;

        public Process(string fileName, string arguments, string workingDirectory) {
            STARTUPINFO si = new();
            si.cb = Marshal.SizeOf(si);
            // si.dwFlags = 0x00000001; // STARTF_USESHOWWINDOW
            // si.wShowWindow = 0; // SW_HIDE

            fileName = Path.Combine(workingDirectory, fileName);

            bool success = CreateProcessA(
                null,
                fileName + " " + arguments,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                0,
                IntPtr.Zero,
                workingDirectory,
                ref si,
                out processInformation);

            if (!success) {
                throw new Exception($"Unable to spawn Alacrity Process: {Marshal.GetLastWin32Error()}");
            }
        }

        public void Kill() {
            Kill(processInformation.hProcess);

            if (processInformation.hProcess != IntPtr.Zero) {
                CloseHandle(processInformation.hProcess);
            }

            if (processInformation.hThread != IntPtr.Zero) {
                CloseHandle(processInformation.hThread);
            }
        }

        public static void Kill(IntPtr handle) {
            if (handle != IntPtr.Zero) {
                TerminateProcess(handle, 0);
                WaitForSingleObject(handle, 5000);
            }
        }

        public static void KillProcessesByName(string processName) {
            uint[] processIds = new uint[1024];

            if (!EnumProcesses(processIds, (uint) processIds.Length * sizeof(uint), out uint bytesNeeded)) {
                return;
            }

            uint numProcesses = bytesNeeded / sizeof(uint);
            var currentProcessId = GetCurrentProcessId();

            for (uint i = 0; i < numProcesses; i++) {
                if (processIds[i] == currentProcessId) {
                    continue;
                }

                // PROCESS_QUERY_INFORMATION | SYNCHRONIZE | PROCESS_TERMINATE | PROCESS_VM_READ
                IntPtr hProcess = OpenProcess(0x100411, false, processIds[i]);
                if (hProcess == IntPtr.Zero) {
                    CloseHandle(hProcess);
                    continue;
                }

                StringBuilder processNameBuilder = new(1024);

                if (GetModuleBaseNameA(hProcess, IntPtr.Zero, processNameBuilder, 1024) <= 0) {
                    CloseHandle(hProcess);
                    continue;
                }

                if (!string.Equals(processNameBuilder.ToString(), processName, StringComparison.OrdinalIgnoreCase)) {
                    CloseHandle(hProcess);
                    continue;
                }

                Kill(hProcess);
                CloseHandle(hProcess);
            }
        }
    }
}