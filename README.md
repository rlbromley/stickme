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

# todo
In no particular order:
* make form transparency adjustable
* make input device adjustable
* create UI for managing settings
* fix some quirks in the audio math