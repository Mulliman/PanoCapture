using System;
using System.IO;

namespace PanoCapture.Process
{
    public class BlendedStackBuilder : HuginBuilderBase
    {
        public BlendedStackBuilder(string outputPath, params string[] inputFiles) : base(outputPath, inputFiles)
        {
        }

        public BlendedStackBuilder SetHuginBinPath(string huginPath)
        {
            _huginBinPath = huginPath;

            return this;
        }

        public BlendedStackBuilder SetTempDir(string tempDir)
        {
            _tempDir = tempDir;

            return this;
        }

        public BlendedStackBuilder SetLogDir(string logDir)
        {
            _logDir = logDir;

            return this;
        }

        public BlendedStackBuilder ConfigureProgressUpdater(Action<Update> callback)
        {
            _progressUpdateCallback = callback;

            return this;
        }

        public BlendedStackBuilder Build(bool crop = true)
        {
            RunBuild(crop);

            return this;
        }

        private void RunBuild(bool crop)
        {
            try
            {
                var numOfSteps = 3;

                //SendUpdate(1, numOfSteps, $"Step 1 of {numOfSteps} - Creating Hugin project");
                //RunProcessToEnd("pto_gen.exe", $" -o {_outputProjectPath} {_inputFilesAsArguments}");

                var outputFolder = Path.GetDirectoryName(_outputPath);
                var tempStackedFile = Path.Combine(outputFolder, "tempstacked");
                
                SendUpdate(1, numOfSteps, $"Step 1 of {numOfSteps} - Align Images");
                RunProcessToEnd("align_image_stack.exe", $" -v -m -a {tempStackedFile} {_inputFilesAsArguments}");

                // –exposure-weight=0 –saturation-weight=0 –contrast-weight=1 –hard-mask –output=base.tif OUT.tif

                SendUpdate(2, numOfSteps, $"Step 2 of {numOfSteps} - Enfuse Images");
                RunProcessToEnd("enfuse.exe", $" -v –exposure-weight=0 –saturation-weight=0 –contrast-weight=1 –contrast-edge-scale=0.3 –hard-mask {tempStackedFile}*.tif");

                SendUpdate(3, numOfSteps, $"Step 3 of {numOfSteps} - Renaming");
                // For some reason enfuse was always saving as a.tif no matter what.
                File.Move(Path.Combine(outputFolder, "a.tif"), _outputPath);

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