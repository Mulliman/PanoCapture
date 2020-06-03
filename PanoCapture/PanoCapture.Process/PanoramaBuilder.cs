using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace PanoCapture.Process
{
    public class PanoramaBuilder
    {
        protected string _outputPath;
        private string _outputFileName;
        protected string _outputProjectPath;
        protected string _huginBinPath;
        protected string _tempDir;

        protected readonly string[] _inputFiles;
        private readonly string _inputFilesAsArguments;
        private readonly StringBuilder _log;
        protected string _generatedFile;

        public PanoramaBuilder(string outputPath, params string[] inputFiles)
        {
            _outputPath = outputPath;
            _outputFileName = Path.GetFileNameWithoutExtension(outputPath);
            _outputProjectPath = outputPath + ".pto";
            _inputFiles = inputFiles;
            _inputFilesAsArguments = string.Join(" ", _inputFiles.Select(f => "\"" + f + "\""));
            _log = new StringBuilder();
        }

        public PanoramaBuilder SetHuginBinPath(string huginPath)
        {
            _huginBinPath = huginPath;

            return this;
        }

        public PanoramaBuilder SetTempDir(string tempDir)
        {
            _tempDir = tempDir;

            return this;
        }

        public PanoramaBuilder SaveLogInTempDir()
        {
            if(string.IsNullOrEmpty(_tempDir))
            {
                return this;
            }

            var logFile = Path.Combine(_tempDir, $"log.{DateTime.Now.Ticks}.txt");

            File.WriteAllText(logFile, _log.ToString());

            return this;
        }

        public PanoramaBuilder Build()
        {
            // TODO: Try and async this

            //   pto_gen -o _test-project.pto *.tif
            //   cpfind -o _test-project.pto --multirow --celeste _test-project.pto
            //   cpclean -o _test-project.pto _test-project.pto
            //   linefind -o _test-project.pto _test-project.pto
            //   autooptimiser -a -m -l -s -o _test-project.pto _test-project.pto
            //   pano_modify --canvas=AUTO --crop=AUTO -o _test-project.pto _test-project.pto
            //   hugin_executor --stitching --prefix=prefix _test-project.pto
            //   nona -m TIFF_m -o project _test-project.pto

            try
            {
                RunProcessToEnd("pto_gen.exe", $" -o {_outputProjectPath} {_inputFilesAsArguments}");
                RunProcessToEnd("cpfind.exe", $" -o {_outputProjectPath} --multirow --celeste {_outputProjectPath}");
                RunProcessToEnd("cpclean.exe", $" -o {_outputProjectPath} {_outputProjectPath}");
                RunProcessToEnd("linefind.exe", $" -o {_outputProjectPath} {_outputProjectPath}");
                RunProcessToEnd("autooptimiser.exe", $" -a -m -l -s -o {_outputProjectPath} {_outputProjectPath}");
                RunProcessToEnd("pano_modify.exe", $" --canvas=AUTO --crop=AUTO -o {_outputProjectPath} {_outputProjectPath}");
                RunProcessToEnd("hugin_executor.exe", $" --stitching --prefix={_outputFileName} {_outputProjectPath}");
                RunProcessToEnd("nona.exe", $" -m TIFF_m -o project {_outputProjectPath}");
                RunProcessToEnd("enblend", $" -o {_outputPath} *.tif");

                _generatedFile = _outputPath;
            }
            catch (Exception e)
            {
                _log.AppendLine("EXCEPTION THROWN!");
                _log.AppendLine(e.Message);
                _log.AppendLine(e.StackTrace);

                SaveLogInTempDir();
                throw;
            }

            return this;
        }

        public string GetGeneratedFile()
        {
            return _generatedFile;
        }

        private void RunProcessToEnd(string exe, string args)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = GetToolPath(exe);
            startInfo.Arguments = args;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;

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

        private string GetToolPath(string tool)
        {
            if (string.IsNullOrEmpty(_huginBinPath))
            {
                return tool;
            }

            return Path.Combine(_huginBinPath, tool);
        }
    }
}