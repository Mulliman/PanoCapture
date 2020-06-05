---
layout: default
---

# How PanoCapture works

PanoCapture is a Capture One plugin that alows you to generatate panoramas using the open source program Hugin without leaving Capture One.
It is not designed to provide precise control over creating the stitched images, it is to streamline creating panoramas with a generic process that should suit the majority of images.

## Hugin Installation

Before installing PanoCapture you must install Hugin.

<a href="http://hugin.sourceforge.net/download/">You can download Hugin here.</a>

PanoCapture looks for Hugin in the directory `C:\Program Files\Hugin` by default, but this can be changed within Capture One if required.

## Installation

<div class="youtube-container">
<iframe src="https://www.youtube.com/embed/LwaZ4JD5AeU" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>

1. <a href="https://github.com/Mulliman/PanoCapture/blob/master/docs/assets/releases/PanoCapture.CaptureOne.coplugin?raw=true">Download the coplugin here</a>
1. Open Capture One and open preferences from the edit menu.
1. Select the plugins tabs press the plus button bottom left.
1. Choose the downloaded coplugin file and press open.
1. PanoCapture is now installed.
1. Press the 'Test' button to ensure that the Hugin and Pano Capture are configured correctly. 
If Hugin loads you are ready to create panoramas, otherwise check that the location you installed Hugin to corresponds with the path in the 'Hugin Bin Path' text box.
This path should end with 'bin' if set up correctly.

## Using PanoCapture

1. Select multiple images from the browser in Capture One
1. Apply any raw adjustments at this stage for best results
1. Right click on one of the images in the browser and select Edit With > Create Panorama.
1. The process will start automatically and when finished will create a new Tiff file directly after you last image suffixed with '-pano'.

If you are working with many large files this process can take a long time. 
PanoCapture is built to send Capture One progress information, with message outlining what is currently processing.
This information isn't currently being shown, and the activities panel doesn't indicate the progress either.
However, if you look in the windows taskbar the progress is actually correctly respresented.