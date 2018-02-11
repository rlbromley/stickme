# stickme
Stick figure "webcam" animation form.  I use this instead of the more traditional webcam for my twitch streams (https://www.twitch.tv/analogcomputer).  It accomplishes this function by using flip-frame animation and monitoring an audio source to determine how to animate the frames.

# use
Edit app.config to change settings to reflect your desired needs.  Create animation frames accordingly.  Launch app, capture output as needed.  Clicking on a surface in the app will close it.  Of particular note, the application is currently limited to recording audio from the default recording device, and uses pure black as a transparency key.

# settings overview
Currently, settings are controlled via editing the file app.config in the program's installation directory.  The adjustable settings are
* closedImage

   Contains the filename to be loaded as the "mouth closed" animation frame

* twentyImage

   Contains the filename to be loaded as the "20% open" animation frame

* fourtyImage

   Contains the filename to be loaded as the "50% open" animation frame

* sixtyImage

   Contains the filename to be loaded as the "60% open" animation frame

* eightyImage

   Contains the filename to be loaded as the "80% open" animation frame

* openImage

   Contains the filename to be loaded as the "100% open" animation frame

* enablePtt

   True or false.  Controls whether the audio source is listened to all the time, or only when a "Push To Talk" button is pressed.
   
* pttButton

   If enablePtt is true, this is the button that will trigger listening of the audio device while it is pushed.  Can be either a keyboard button or mouse button.

* dbFloor

   The value to be considered the minimum dB for audio input.  Considered the bottom level; below this the animation will remain in the closed state regardless of what's heard by the microphone.  Used as the lower endpoint of the dynamic range recalculation.

* dbMax

   The value to be considered the maximum dB for audio input.  Considered the top level; above this the animation will remain fully open regardless of intensities beyond this.  Used as the upper endpoint of the dynamic range recalculation.

* dynamicMax

   When set to false, the dB range used to control the frames is calculated from the floor and max upon launch and kept static.  When set to true and PTT is enabled, upon the PTT button being lifted up the max DB detected during that audio session is used with the floor to recalculate the range.

# todo
In no particular order:
* make form transparency adjustable
* make input device adjustable
* create UI for managing settings
* fix some quirks in the audio math - hoping the range adjustments help in that regard