using System;

namespace PanoCapture.Process
{
    public class PanoramaBuilder : HuginBuilderBase
    {
        public PanoramaBuilder(string outputPath, params string[] inputFiles) : base(outputPath, inputFiles)
        {
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

        public PanoramaBuilder SetLogDir(string logDir)
        {
            _logDir = logDir;

            return this;
        }

        public PanoramaBuilder ConfigureProgressUpdater(Action<Update> callback)
        {
            _progressUpdateCallback = callback;

            return this;
        }

        public PanoramaBuilder Build(bool crop = true)
        {
            RunBuild(crop);

            return this;
        }

        private void RunBuild(bool crop)
        {
            try
            {
                var numOfSteps = 9;

                SendUpdate(1, numOfSteps, $"Step 1 of {numOfSteps} - Creating Hugin project");
                RunProcessToEnd("pto_gen.exe", $" -o {_outputProjectPath} {_inputFilesAsArguments}");

                SendUpdate(2, numOfSteps, $"Step 2 of {numOfSteps} - Finding points");
                RunProcessToEnd("cpfind.exe", $" -o {_outputProjectPath} --multirow --celeste {_outputProjectPath}");

                SendUpdate(3, numOfSteps, $"Step 3 of {numOfSteps} - Cleaning points");
                RunProcessToEnd("cpclean.exe", $" -o {_outputProjectPath} {_outputProjectPath}");

                SendUpdate(4, numOfSteps, $"Step 4 of {numOfSteps} - Finding lines");
                RunProcessToEnd("linefind.exe", $" -o {_outputProjectPath} {_outputProjectPath}");

                SendUpdate(5, numOfSteps, $"Step 5 of {numOfSteps} - Optimising");
                RunProcessToEnd("autooptimiser.exe", $" -a -m -l -s -o {_outputProjectPath} {_outputProjectPath}");

                if (crop)
                {
                    SendUpdate(6, numOfSteps, $"Step 6 of {numOfSteps} - Modifying");
                    RunProcessToEnd("pano_modify.exe", $" --canvas=AUTO --crop=AUTO -o {_outputProjectPath} {_outputProjectPath}");
                }
                else
                {
                    SendUpdate(6, numOfSteps, $"Step 6 of {numOfSteps} - Modifying");
                    RunProcessToEnd("pano_modify.exe", $" --canvas=AUTO -o {_outputProjectPath} {_outputProjectPath}");
                }

                SendUpdate(7, numOfSteps, $"Step 7 of {numOfSteps} - Executing");
                RunProcessToEnd("hugin_executor.exe", $" --stitching --prefix={_outputFileName} {_outputProjectPath}");

                SendUpdate(8, numOfSteps, $"Step 8 of {numOfSteps} - Generating files");
                RunProcessToEnd("nona.exe", $" -m TIFF_m -o project {_outputProjectPath}");

                SendUpdate(9, numOfSteps, $"Step 9 of {numOfSteps} - Blending");
                RunProcessToEnd("enblend", $" -o {_outputPath} *.tif");

                _generatedFile = _outputPath;

                SaveLog();
            }
            catch (Exception e)
            {
                _log.AppendLine("EXCEPTION THROWN!");
                _log.AppendLine(e.Message);
                _log.AppendLine(e.StackTrace);

                SaveLog();
                throw;
            }
            finally
            {
            }
        }
    }
}