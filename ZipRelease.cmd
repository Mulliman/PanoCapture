echo "Have you run a release build on PanoCapture.Plugin?"

pause

cd ".\PanoCapture\Plugin\bin\Release"

powershell Compress-Archive ./* "..\..\..\..\docs\assets\releases\PanoCapture.CaptureOne.zip" -force

cd "..\..\..\..\docs\assets\releases"

call del PanoCapture.CaptureOne.coplugin

pause

call RENAME PanoCapture.CaptureOne.zip PanoCapture.CaptureOne.coplugin

pause