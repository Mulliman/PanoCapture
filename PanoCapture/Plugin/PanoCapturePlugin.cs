using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using PhaseOne.Plugin;
using System.Reflection;
using PanoCapture.Process;

namespace PanoCapture.Plugin
{
    public partial class PanoCapturePlugin : ISettingsPlugin, IEditingPlugin // IOpenWithPlugin
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

        private SettingsRepository _settingsRepo;
        private Settings _settings;

        public PanoCapturePlugin()
        {
            _rootDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            TempImagesPath = Path.Combine(_rootDirectory, "TempProjectLocation");
            _settingsRepo = new SettingsRepository(Path.Combine(_rootDirectory, "settings.json"));
            _settings = _settingsRepo.Get();
        }

        #region ISettingsPlugin

        public IEnumerable<ElementsGroup> GetSettings()
        {
            // get Fresh settings each time
            _settings = _settingsRepo.Get();

            var items = new List<Element>();

            var note = new TextItem("notelabel1", "Note:") { Value = "Capture One doesn't update until you exit the field." };
            var tip = new TextItem("tiplabel1", "Tip:") { Value = "Plase click or tab off before pressing save or test." };
            
            items.Add(note);
            items.Add(tip);

            var downloadButton = new ButtonItem("downloadButton", "Download Hugin");
            items.Add(downloadButton);

            var pathToProcessExe = new TextItem("pathToHuginBin", "Hugin Bin Path")
            {
                Value = _settings.HuginBinPath,
                InformativeText = $"This needs to be the bin folder in your Hugin installation. E.g. {_settings.HuginBinPath}"
            };
            items.Add(pathToProcessExe);

            var testButton = new ButtonItem("testBinPath", "Test (Hugin will open if valid)");
            items.Add(testButton);

            var saveButton = new ButtonItem("saveBinPath", "Save");
            items.Add(saveButton);

            var settings = new ElementsGroup("settings", "Configure Hugin", items.ToArray());

            return new[]
            {
                settings,
            };
        }

        public bool UpdateSettings(string argKey, object argValue)
        {
            if (argKey == "pathToHuginBin")
            {
                _settings.HuginBinPath = argValue.ToString();
                return false;
            }

            // don't refresh the settings
            return false;
        }

        public bool HandleEvent(SettingsEvent argSettingsEvent, Item argItem)
        {
            if (argItem.Id == "testBinPath")
            {
                if(!IsValidInstallation())
                {
                    throw new PluginException("This is NOT a valid Hugin installation path.");
                }
                else
                {
                    var huginExePath = Path.Combine(_settings.HuginBinPath, "hugin.exe");
                    System.Diagnostics.Process.Start(huginExePath);
                }
            }

            if (argItem.Id == "downloadButton")
            {
                System.Diagnostics.Process.Start("http://hugin.sourceforge.net/download/");
                return false;
            }

            if (argItem.Id == "saveBinPath")
            {
                if (!IsValidInstallation())
                {
                    throw new PluginException("This is NOT a valid Hugin installation path.");
                }

                _settingsRepo.Save(_settings);
                return true;
            }

            return false;
        }

        #endregion

        #region Open With and Edit With

        #region Open With Not used yet

        // NOT SUPPORTING OPEN WITH YET AS I HAVEN'T TESTED IT AND WON'T USE IT MYSELF. 

        //public IEnumerable<PluginAction> GetOpenWithActions(IDictionary<string, int> argInfo, OpenWithPluginRole argRole)
        //{
        //if (argRole == OpenWithPluginRole.OpenWithPluginRolePostProcessInDocument)
        //{
        //    return Enumerable.Empty<PluginAction>();
        //}

        //if (argRole == OpenWithPluginRole.OpenWithPluginRolePostProcessOutput)
        //{
        //    return new[] { _runProcessAction, _runProcessNoCropAction };
        //}

        //if (!argInfo.Any(a => a.Key == ".jpg" || a.Key == ".jpeg" || a.Key == ".tif" || a.Key == ".tiff"))
        //{
        //    return Enumerable.Empty<PluginAction>();
        //}

        //return new[] { _runProcessAction, _runProcessNoCropAction };
        //}

        //public PluginActionOpenWithResult StartOpenWithTask(FileHandlingPluginTask argTask, ReportProgress argProgress)
        //{
        //    if (argTask.PluginAction.Identifier == _runProcessAction.Identifier)
        //    {
        //        var created = StartPanoCaptureProcessor(argTask, argProgress, true);
        //        return new PluginActionOpenWithResult();
        //    } 
        //    else if(argTask.PluginAction.Identifier == _runProcessNoCropAction.Identifier)
        //    {
        //        var created = StartPanoCaptureProcessor(argTask, argProgress, false);
        //        return new PluginActionOpenWithResult();
        //    }

        //    return new PluginActionOpenWithResult(false);
        //}

        #endregion

        public IEnumerable<PluginAction> GetEditingActions(IDictionary<string, int> argInfo)
        {
            return new[] { _runProcessAction };
        }

        public PluginActionImageResult StartEditingTask(FileHandlingPluginTask argPluginTask, ReportProgress argProgress)
        {
            if (argPluginTask.PluginAction.Identifier == _runProcessAction.Identifier)
            {
                if(!IsValidInstallation())
                {
                    throw new PluginException("You do not have valid Hugin installation. Please see plugin settings to download or configure.");
                }

                var created = StartPanoCaptureProcessor(argPluginTask, argProgress, true);
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

        private bool IsValidInstallation()
        {
            var huginExePath = Path.Combine(_settings.HuginBinPath, "hugin.exe");

            return File.Exists(huginExePath);
        }

        private string StartPanoCaptureProcessor(FileHandlingPluginTask argTask, ReportProgress argProgress, bool crop)
        {
            return StartPanoCaptureProcessor(argTask.Files, argProgress, crop);
        }

        private string StartPanoCaptureProcessor(IEnumerable<string> inputFiles, ReportProgress argProgress, bool crop)
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
                .SetHuginBinPath(_settings.HuginBinPath)
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