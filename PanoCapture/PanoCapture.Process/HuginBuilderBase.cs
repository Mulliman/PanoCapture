using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace PanoCapture.Process
{
    public abstract class HuginBuilderBase
    {
        protected string _outputPath;
        protected string _outputFileName;
        protected string _outputProjectPath;
        protected string _huginBinPath;
        protected string _tempDir;

        protected readonly string[] _inputFiles;
        protected readonly string _inputFilesAsArguments;
        protected readonly StringBuilder _log;
        protected string _logDir;
        protected string _generatedFile;
        protected Action<Update> _progressUpdateCallback;

        public HuginBuilderBase(string outputPath, params string[] inputFiles)
        {
            _outputPath = outputPath;
            _outputFileName = Path.GetFileNameWithoutExtension(outputPath);
            _outputProjectPath = outputPath + ".pto";
            _inputFiles = inputFiles;
            _inputFilesAsArguments = string.Join(" ", _inputFiles.Select(f => "\"" + f + "\""));
            _log = new StringBuilder();
        }

        protected void SendUpdate(int stepNum, int maxSteps, string message)
        {
            if (_progressUpdateCallback != null)
            {
                _progressUpdateCallback(new Update(stepNum, maxSteps, message));
            }
        }

        protected void RunProcessToEnd(string exe, string args)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = GetToolPath(exe);
            startInfo.Arguments = args;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            if (!string.IsNullOrEmpty(_tempDir))
            {
                startInfo.WorkingDirectory = _tempDir;
            }

            using (var process = System.Diagnostics.Process.Start(startInfo))
            {
                // read chunk-wise while process is running.
                while (!process.HasExited)
                {
                    _log.Append(process.StandardOutput.ReadToEnd());
                }

                // make sure not to miss out on any remaindings.
                _log.Append(process.StandardOutput.ReadToEnd());

                process.WaitForExit();
            }
        }

        protected string GetToolPath(string tool)
        {
            if (string.IsNullOrEmpty(_huginBinPath))
            {
                return tool;
            }

            return Path.Combine(_huginBinPath, tool);
        }

        public void SaveLog()
        {
            try
            {
                if (string.IsNullOrEmpty(_logDir))
                {
                    return;
                }

                if (!Directory.Exists(_logDir))
                {
                    Directory.CreateDirectory(_logDir);
                }

                var logFile = Path.Combine(_logDir, $"{this.GetType().Name}log.{DateTime.Now.Ticks}.txt");

                File.WriteAllText(logFile, _log.ToString());
            }
            catch
            {
                // Don't die just because it can't log.
            }
            
        }

        public string GetGeneratedFile()
        {
            return _generatedFile;
        }
    }
}