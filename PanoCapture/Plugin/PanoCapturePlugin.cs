using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Media.Imaging;
using PhaseOne.Plugin;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Reflection;
using PanoCapture.Process;

namespace PanoCapture.Plugin
{
    public partial class PanoCapturePlugin : ISettingsPlugin, IOpenWithPlugin, IEditingPlugin
    {
        private static readonly BitmapSource Icon = GetImage(Environment.CurrentDirectory + "/miniIcon.png");

        private readonly PluginAction _runProcessAction = new PluginAction(
                    "Create Panorama",
                    "CreatePanorama")
        {
            Image = Icon
        };

        private string _rootDirectory;

        public string TempImagesPath { get; }

        public PanoCapturePlugin()
        {
            _rootDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            TempImagesPath = Path.Combine(_rootDirectory, "TempProjectLocation");
        }

        public string HuginBinPath => @"C:\Program Files\Hugin\bin";

        #region ISettingsPlugin

        public IEnumerable<ElementsGroup> GetSettings()
        {
            var items = new List<Element>();

            var pathToProcessExe = new TextItem("pathToHuginBin", "Hugin Bin Path")
            {
                Value = HuginBinPath,
                InformativeText = $"This needs to be the bin folder in your Hugin installation. E.g. {HuginBinPath}"
            };
            items.Add(pathToProcessExe);

            var settings = new ElementsGroup("settings", "", items.ToArray());

            return new[]
            {
                settings,
            };
        }

        public bool UpdateSettings(string argKey, object argValue)
        {
            // don't refresh the settings
            return false;
        }

        public bool HandleEvent(SettingsEvent argSettingsEvent, Item argItem)
        {
            return false;
        }

        #endregion

        #region Open With and Edit With

        public IEnumerable<PluginAction> GetOpenWithActions(IDictionary<string, int> argInfo, OpenWithPluginRole argRole)
        {
            if (argRole == OpenWithPluginRole.OpenWithPluginRolePostProcessInDocument)
            {
                return Enumerable.Empty<PluginAction>();
            }

            if (argRole == OpenWithPluginRole.OpenWithPluginRolePostProcessOutput)
            {
                return new[] { _runProcessAction };
            }

            if (!argInfo.Any(a => a.Key == ".jpg" || a.Key == ".jpeg" || a.Key == ".tif" || a.Key == ".tiff"))
            {
                return Enumerable.Empty<PluginAction>();
            }

            return new[] { _runProcessAction };
        }

        public IEnumerable<PluginAction> GetEditingActions(IDictionary<string, int> argInfo)
        {
            return new[] { _runProcessAction };
        }

        public PluginActionOpenWithResult StartOpenWithTask(FileHandlingPluginTask argTask, ReportProgress argProgress)
        {
            if (argTask.PluginAction.Identifier == _runProcessAction.Identifier)
            {
                var created = StartPanoCaptureProcessor(argTask, argProgress);
                return new PluginActionOpenWithResult();
            }

            return new PluginActionOpenWithResult(false);
        }

        public PluginActionImageResult StartEditingTask(FileHandlingPluginTask argPluginTask, ReportProgress argProgress)
        {
            if (argPluginTask.PluginAction.Identifier == _runProcessAction.Identifier)
            {
                var created = StartPanoCaptureProcessor(argPluginTask, argProgress);
                return new PluginActionImageResult(new[] { created });
            }

            return new PluginActionImageResult();
        }

        public IEnumerable<FileHandlingPluginTask> GetTasks(PluginAction argPluginAction, IEnumerable<string> argFiles)
        {
            if (argPluginAction.Identifier == _runProcessAction.Identifier)
            {
                var files = argFiles.Where(f => f != null && (f.EndsWith("jpeg") || f.EndsWith(".jpg") || f.EndsWith(".tif") || f.EndsWith(".tiff")));

                if (files.Any())
                {
                    return new[]
                    {
                        new FileHandlingPluginTask(Guid.NewGuid(), argPluginAction, files.ToArray()),
                    };
                }
            }

            var tasks = argFiles.Select(f => new FileHandlingPluginTask(Guid.NewGuid(), argPluginAction, new[] { f }));
            return tasks;
        }

        #endregion

        private string StartPanoCaptureProcessor(FileHandlingPluginTask argTask, ReportProgress argProgress)
        {
            return StartPanoCaptureProcessor(argTask.Files, argProgress);
        }

        private string StartPanoCaptureProcessor(IEnumerable<string> inputFiles, ReportProgress argProgress)
        {
            var files = inputFiles.Where(f => f != null && (f.EndsWith(".jpeg") || f.EndsWith(".jpg") || f.EndsWith(".tif") || f.EndsWith(".tiff")));

            if (!files.Any())
            {
                return null;
            }

            var outputFolder = Path.Combine(TempImagesPath, files.First().Split('.').First() + "-pano");

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            var outputFileName = files.First().Split('.').First() + "-pano.tif";
            var outputFile = Path.Combine(outputFolder, outputFileName);

            var builtFile = new PanoramaBuilder(outputFile, inputFiles.ToArray())
                .SetHuginBinPath(HuginBinPath)
                .SetTempDir(outputFolder)
                .ConfigureProgressUpdater((update) => argProgress(update.StepNumber, update.StepTotal, update.StepText))
                .Build()
                .SaveLogInTempDir()
                .GetGeneratedFile();

            return builtFile;
        }

        private static BitmapImage GetImage(string argPath)
        {
            Stream stream = File.Open(argPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            BitmapImage imgsrc = new BitmapImage();

            imgsrc.BeginInit();
            imgsrc.StreamSource = stream;
            imgsrc.EndInit();

            return imgsrc;
        }
    }
}