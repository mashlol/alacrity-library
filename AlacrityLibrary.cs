using System;
using System.IO;
using System.Text;

namespace Alacrity.Library {

    public class AlacrityLibrary {

        private readonly Process process;
        private readonly string[] filePaths;

        private AlacrityLibrary(Process process, string[] filePaths) {
            this.process = process;
            this.filePaths = filePaths;
        }

        public static AlacrityLibrary CreateBrowser(
            string basePath,
            string urlOnLoad,
            int screenWidth,
            int screenHeight,
            int framerateOnLoad,
            int bufferSize,
            string securityString,
            int remoteDebuggingPort,
            int ipcPort,
            int websocketPort,
            string serializedChromiumSwitches,
            string cacheDirectory = null
        ) {
            var alacrityFile = Path.Combine(basePath, "Alacrity.mv");

            var tempDir = Path.GetTempPath();

            var filePath = "Alacrity.exe";
            var finalPath = Path.Combine(tempDir, filePath);

            Process.KillProcessesByName(filePath);

            using (FileStream resourceStream = new FileStream(alacrityFile, FileMode.Open, FileAccess.Read, FileShare.None))
            using (Stream file = File.Create(finalPath)) {
                resourceStream.CopyTo(file);
            }

            // Copy Cefglue to the temp dir
            var cefGluePath = Path.Combine(basePath, "Xilium.CefGlue.dll");
            var cefGlueDest = Path.Combine(tempDir, "Xilium.CefGlue.dll");
            File.Copy(cefGluePath, cefGlueDest, true);

            // Communicate the cef path via a file
            var cefPath = Path.Combine(basePath, "cef");
            var cefAbsPath = Path.GetFullPath(cefPath);
            var cefPathFile = Path.Combine(tempDir, "cefpath");
            File.WriteAllText(cefPathFile, cefAbsPath);

            var chromiumArgsPath = Path.Combine(tempDir, "cargs");
            File.WriteAllText(chromiumArgsPath, serializedChromiumSwitches);

            var args = urlOnLoad.Replace(" ", "%20") + " " +
                screenWidth + " " +
                screenHeight + " " +
                framerateOnLoad + " " +
                bufferSize + " " +
                securityString + " " +
                remoteDebuggingPort + " " +
                ipcPort + " " +
                websocketPort;

            if (cacheDirectory != null) {
                args += " " + cacheDirectory;
            }

            var process = new Process(filePath, args, tempDir);

            return new AlacrityLibrary(process, new string[] {
                finalPath,
                cefPathFile,
                chromiumArgsPath,
            });
        }

        public void Destroy() {
            process?.Kill();
            try {
                foreach (var path in filePaths) {
                    File.Delete(path);
                }
            } catch (Exception) {
                // Ignore
            }
        }

    }
}