using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

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
        private Action<Update> _progressUpdateCallback;

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

        public PanoramaBuilder ConfigureProgressUpdater(Action<Update> callback)
        {
            _progressUpdateCallback = callback;

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

            RunBuild();

            return this;
        }

        private void RunBuild()
        {
            try
            {
                var numOfSteps = 9;

                SendUpdate(1, numOfSteps, $"Step 1 of {numOfSteps} - Creating Hugin project");
                RunProcessToEnd("pto_gen.exe", $" -o {_outputProjectPath} {_inputFilesAsArguments}");

                SendUpdate(2, numOfSteps, $"Step 2 of {numOfSteps} - Finding points");
                RunProcessToEnd("cpfind.exe", $" -o {_outputProjectPath} --multirow --celeste {_outputProjectPath}");

                SendUpdate(3, numOfSteps, $"Step 3 of {numOfSteps} - Cleaing points");
                RunProcessToEnd("cpclean.exe", $" -o {_outputProjectPath} {_outputProjectPath}");

                SendUpdate(4, numOfSteps, $"Step 4 of {numOfSteps} - Finding lines");
                RunProcessToEnd("linefind.exe", $" -o {_outputProjectPath} {_outputProjectPath}");

                SendUpdate(5, numOfSteps, $"Step 5 of {numOfSteps} - Optimising");
                RunProcessToEnd("autooptimiser.exe", $" -a -m -l -s -o {_outputProjectPath} {_outputProjectPath}");

                SendUpdate(6, numOfSteps, $"Step 6 of {numOfSteps} - Modifying");
                RunProcessToEnd("pano_modify.exe", $" --canvas=AUTO --crop=AUTO -o {_outputProjectPath} {_outputProjectPath}");

                SendUpdate(7, numOfSteps, $"Step 7 of {numOfSteps} - Executing");
                RunProcessToEnd("hugin_executor.exe", $" --stitching --prefix={_outputFileName} {_outputProjectPath}");

                SendUpdate(8, numOfSteps, $"Step 8 of {numOfSteps} - Generating files");
                RunProcessToEnd("nona.exe", $" -m TIFF_m -o project {_outputProjectPath}");

                SendUpdate(9, numOfSteps, $"Step 9 of {numOfSteps} - Blending");
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
            finally
            {
            }
        }

        public string GetGeneratedFile()
        {
            return _generatedFile;
        }

        private void SendUpdate(int stepNum, int maxSteps, string message)
        {
            if(_progressUpdateCallback != null)
            {
                _progressUpdateCallback(new Update(stepNum, maxSteps, message));
            }
        }

        private void RunProcessToEnd(string exe, string args)
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

        private string GetToolPath(string tool)
        {
            if (string.IsNullOrEmpty(_huginBinPath))
            {
                return tool;
            }

            return Path.Combine(_huginBinPath, tool);
        }
    }

    public class Update
    {
        public Update()
        {
        }

        public Update(int stepNum, int maxSteps, string message)
        {
            StepNumber = stepNum;
            StepTotal = maxSteps;
            StepText = message;
        }

        public int StepNumber { get; set; }

        public int StepTotal { get; set; }

        public string StepText { get; set; }
    }
}