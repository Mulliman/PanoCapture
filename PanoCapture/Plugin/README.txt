To speed-up the development process, you can create a symbolic link from the C1 folder where all plugins are stored, which is:
C:\Users\USERHERE\AppData\Local\CaptureOne\Plugins\

to the plugin folder in your project's output directory.
Example:
mklink /d "C:\Users\USERHERE\AppData\Local\CaptureOne\Plugins\com.phaseone.demoplugin" "C:\DemoPlugin\bin\Debug\" 

So whenever you make a new build, and start C1, you don't have to install it again. 
All changes will be visible right away. 
NOTE: after making a new build - stop and start your plugin in Capture One via Preferences panel. Capture One needs to reload your plugin.

By default the project starts C1 (if installed). The default location is: 
	C:\Program Files\Phase One\Capture One 12\CaptureOne.exe

If you have your C1 installed somewhere else, please remember to update the startup application in the preferences of the project.