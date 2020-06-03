using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Media.Imaging;
using PhaseOne.Plugin;


namespace DemoPlugin
{
    public partial class DemoPlugin : IPublishingPlugin, ISettingsPlugin, IActionSettings, IVariantProcessing
    {
        private readonly BitmapImage publishActionImage;
        private readonly PluginAction publishAction;
        private readonly PluginAction publishActionEachFileSeparately;
        private readonly Random randomSleepTime;
        private string username;
        private string password;
        private Label showLabel = Label.ShowNothing;

        private  BitmapImage getImage(string argPath)
        {
            Stream stream = File.Open(argPath, FileMode.Open);
            BitmapImage imgsrc = new BitmapImage();
            imgsrc.BeginInit();
            imgsrc.StreamSource = stream;
            imgsrc.EndInit();
            return imgsrc;
        }

        public DemoPlugin()
        {
            publishActionImage = getImage(Environment.CurrentDirectory + "/actionIcon.png"); 
            publishAction = new PluginAction(
                    "publish to dummy service - all files in one task",
                    "publishActionAllFilesId")
                { Image = publishActionImage };
            publishActionEachFileSeparately = new PluginAction(
                    "publish to dummy service - each file as a separate task",
                    "publishActionSeparatelyId")
                { Image = publishActionImage };

            randomSleepTime = new Random();
        }

        #region IPublishingPlugin

        public IEnumerable<PluginAction> GetPublishingActions(int argFilesAmount)
        {
            return argFilesAmount <= 0 ? new PluginAction[0] : new[]
            {
                publishAction,
                publishActionEachFileSeparately
            };
        }

        public IEnumerable<FileHandlingPluginTask> GetTasks(PluginAction argPluginAction, IEnumerable<string> argFiles)
        {
            // one task for all files
            if (argPluginAction.Identifier == publishAction.Identifier)
            {
                return new[]
                {
                    new FileHandlingPluginTask(Guid.NewGuid(), argPluginAction, argFiles),
                };
            }
            // create a task for each file
            else
            {
                var tasks = argFiles.Select(f => new FileHandlingPluginTask(Guid.NewGuid(), argPluginAction, new[] { f }));
                return tasks;
            }
        }

        public PluginActionPublishResult StartPublishingTask(FileHandlingPluginTask argPluginTask, ReportProgress argProgress)
        {
            if (argPluginTask.PluginAction.Identifier != publishAction.Identifier && argPluginTask.PluginAction.Identifier != publishActionEachFileSeparately.Identifier) throw new ArgumentException("Unknown action");
            if (!argPluginTask.Files.Any()) throw new ArgumentException("no files");

            var files = argPluginTask.Files.ToArray();

            var total = files.Length;
            var completed = 0;
            var startDelay = 1500;

            Thread.Sleep(startDelay);

            var msg = $"Publishing {files[completed]})";

            while (completed < total)
            {
                Console.Out.WriteLine($"publishing {files[completed]} started...");
                var steps = 100;
                // simulate publishing
                for (int i = 0; i < steps; i++)
                {
                    var isCancelled = argProgress(completed * steps + i, total * steps, msg);
                    if (isCancelled)
                        throw new PluginOperationCancelled();

                    Thread.Sleep(randomSleepTime.Next(50, 200));
                }

                completed++;
                Console.Out.WriteLine("publishing done");
            }

            // Return a publish result
            return new PluginActionPublishResult(new Uri("https://www.captureone.com/"));
        }

        #endregion

        #region ISettingsPlugin

        public IEnumerable<ElementsGroup> GetSettings()
        {
            var items = new List<Element>();

            var username = new TextItem("username", "Username");
            var password = new PasswordItem("password", "Password");
            var loginButton = new ButtonItem("loginButton", "Login");
            var logoutButton = new ButtonItem("logoutButton", "Logout");

            switch (showLabel)
            {
                case Label.ShowNothing:
                    items.Add(username);
                    items.Add(password);
                    items.Add(loginButton);
                    break;
                case Label.ShowLoginOk:
                    var label = new LabelItem("label", "You are now logged in!");
                    items.Add(logoutButton);
                    items.Add(label);
                    break;
                case Label.ShowLoginFailed:
                    label = new LabelItem("label", "Login failed");
                    items.Add(username);
                    items.Add(password);
                    items.Add(loginButton);
                    items.Add(label);
                    break;
                case Label.ShowLogoutOk:
                    label = new LabelItem("label", "You have logged out");
                    items.Add(username);
                    items.Add(password);
                    items.Add(loginButton);
                    items.Add(label);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var settings = new ElementsGroup("settings", "", items.ToArray());

            return new[]
            {
                settings,
            };
        }

        public bool UpdateSettings(string argKey, object argValue)
        {
            if (argKey == "username") username = (string)argValue;
            if (argKey == "password") password = (string)argValue;

            // don't refresh the settings
            return false;
        }

        public bool HandleEvent(SettingsEvent argSettingsEvent, Item argItem)
        {
            var button = argItem as ButtonItem;

            if (button.Id == "loginButton")
            {
                Console.Out.WriteLine("authenticating user...");
                Thread.Sleep(4000); // simulate loging in
                if (username == "user" && password == "pass")
                {
                    showLabel = Label.ShowLoginOk;
                    Console.Out.WriteLine("user authenticated");
                    return true;
                }
                else
                {
                    showLabel = Label.ShowLoginFailed;
                    Console.Out.WriteLine("authentication failed");
                    return true;
                }
            }
            else if (button.Id == "logoutButton")
            {
                Console.Out.WriteLine("logging out...");
                showLabel = Label.ShowLogoutOk;
                username = "";
                password = "";
                return true;
            }

            return false;
        }

        #endregion

        #region IActionSettings
        public IEnumerable<ElementsGroup> GetSettings(PluginAction argAction, IDictionary<string, object> argCurrentValues)
        {
            return new ElementsGroup[]
            {
                new ElementsGroup("group1", "Options 1", new BoolItem("bool-item", "Bool item", false)), 
            };
        }

        public bool UpdateSettings(PluginAction argAction, IDictionary<string, object> argCurrentValues, string argUpdatedKey, object argUpdatedValue)
        {
            // process the update
            return false; // inform Capture One -> don't call GetSettings for that action
        }

        public ValidationResult ValidateSettings(PluginAction argAction, IDictionary<string, object> argSettings)
        {
            if (argSettings.TryGetValue("bool-item", out var value))
            {
                if (!(bool)value)
                    return new ValidationResult(new Dictionary<string, string>{ { "bool-item", "It has to be checked" } });
                return new ValidationResult(); // validation OK
            }
            throw new PluginException("Unable to validate - internal error");
        }

        #endregion

        #region IVariantProcessingSettings

        public IDictionary<string, object> GetProcessingSettingsForAction(PluginAction argAction)
        {
            return new Dictionary<string, object>
            {
                // Supported file formats
                [ProcessSettings.COSupportedFileFormatsKey] = new[]
                {
                    ProcessSettings.COProcessFileFormat.COProcessFileFormatTIFF,
                    ProcessSettings.COProcessFileFormat.COProcessFileFormatJPEG
                },
                // The default export formats
                [ProcessSettings.COProcessFileFormatKey] = ProcessSettings.COProcessFileFormat.COProcessFileFormatJPEG,
                // Include annotations
                [ProcessSettings.COProcessIncludeAnnotationsKey] = true,
                // Include keywords
                [ProcessSettings.COProcessIncludeKeywordsMetadataKey] = ProcessSettings.COProcessMetadataIncludeKeywords.COProcessMetadataIncludeKeywordsIncludeAll,
                // Scale to 100px on the long edge
                [ProcessSettings.COProcessScaleMethodKey] = ProcessSettings.COProcessScaleMethod.COProcessScaleMethodLongEdge,
                [ProcessSettings.COProcessLongEdgeScaleKey] = new Dictionary<string, object>
                {
                    [ProcessSettings.COProcessScaleLengthKey] = 100,
                    [ProcessSettings.COProcessScaleUnitKey] = ProcessSettings.COProcessSizeUnit.COProcessSizeUnitPixel
                }
            };
        }

        public ProcessingSettingsVisibility GetProcessingSettingsVisibilityForAction(PluginAction argAction)
        {
            return ProcessingSettingsVisibility.ShowAll;
        }

        #endregion
    }
}