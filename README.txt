BB+ Custom Musics - How to add the musics and MIDIs

This small text file will guide you to add your own music and sound effects to the game using this mod!

What You Need
-------------
- Music files (like .mid, .midi, .ogg, .wav, .mp3)
- SoundFont files (only .sfs)

Where to Put Your Files
-----------------------
First, you need to extract all of the files inside the zip! That should be easy lol
Then, you need to find the assets folder in the game files. Here's how:

1. Find your Baldi's Basics Plus game folder.
2. Inside that folder, look for another folder called BALDI_Data.
3. Open BALDI_Data and then open the StreamingAssets folder.
4. Inside StreamingAssets, you should see a folder named Modded.
5. Inside Modded, you'll find the folder named pixelguy.pixelmodding.baldiplus.custommusics.

This pixelguy.pixelmodding.baldiplus.custommusics folder is where you'll put your custom music and sounds. Inside it, you'll find more folders for different types of sounds:

- schoolMusics: For Midi that plays in the schoolhouse.
- elevatorMusics: For Midi that plays in the elevator.
- fieldTripMusic: For Midi during the field trip minigame.
- fieldTripTutorialMusic: For Midi during the field trip tutorial.
- ambiences: For background sounds and music.
- playtimeMusics: For music during the Playtime event.
- partyMusics: For music during the Party event.
- johnnyMusic: For music in Johnny's Store.
- sfsFiles: For custom SoundFont files (.sfs).

File Types
----------
- Midi: You can use .mid, .midi.
- Music: Basically a sound file, like .ogg, .wav, or .mp3 files.
- SoundFonts: Only use .sfs files. These change how MIDI music sounds.

>> Making Music Play on Specific Floors <<
-----------------------------------------------
You can make your music only play on certain floors of the schoolhouse. This works for the "schoolMusics" and "ambiences" folders.

To do this, name your file like this:

[YourMusicName]_F1_F2_F3_END.midi

- F1 means Floor 1.
- F2 means Floor 2, and so on.
- END means Endless.

>> Making Schoolhouse Music Play for Different Game Modes <<
-----------------------------------------------------------------
For music in the `schoolMusics` folder, you can choose which game mode it plays in.

1. Inside the `schoolMusics` folder, create new folders named after the game modes:
   - Schoolhouse
   - Factory
   - Laboratory
   - Maintenance
   - All (This means it will play in all game modes)

2. Put your music files into the correct game mode folder.

Example structure:

schoolMusics/Schoolhouse/NormalSchoolTheme.midi
schoolMusics/Maintenance/PartySchoolTheme.midi
schoolMusics/All/AlwaysPlayTheme.midi

>> Custom SoundFonts <<
-----------------
Put all your .sfs files into the `sfsFiles` folder. The mod will find and use them automatically.

Have fun adding your own music and sounds to the game!

>> For Modders <<
If you're using this mod to add your musics from your mods, your files and folders must follow a similar structure as described above (especially for Schoolhouse MIDI types). To add a music/midi to a specific part of the game, you can use "MusicRegister.AddMIDIsFromDirectory()" and "MusicRegister.AddMusicFilesFromDirectory()" - your IDE should tell the arguments to insert, it's simple!
