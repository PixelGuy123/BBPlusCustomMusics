using System.Collections.Generic;
using System.IO;
using MidiPlayerTK;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using UnityEngine;

namespace BBPlusCustomMusics.Plugin.Public;

public static class MusicRegister
{
    // ===================== Public Static Methods =====================
    public static void AddMIDIsFromDirectory(MidiDestiny midiDestiny, params string[] paths)
    {
        string path = Path.Combine(paths);
        if (!Directory.Exists(path))
            throw new System.ArgumentException($"The directory to load MIDIs from path ({path}) doesn't exist!");

        switch (midiDestiny)
        {
            case MidiDestiny.Schoolhouse:
                GetAllMIDIs_Schoolhouse(path);
                return;
            default:
                GetAllMIDIs(path, midiDestiny, null);
                return;
        }
    }
    public static void AddMusicFilesFromDirectory(SoundDestiny soundDestiny, params string[] paths)
    {
        string path = Path.Combine(paths);
        if (!Directory.Exists(path))
            throw new System.ArgumentException($"The directory to load Music Files from path ({path}) doesn't exist!");

        GetAllSoundObjects(path, soundDestiny);
    }
    public static void AddSoundFontsFromDirectory(params string[] paths)
    {
        string path = Path.Combine(paths);
        if (!Directory.Exists(path))
            throw new System.ArgumentException($"The directory to load soundfonts from path ({path}) doesn't exist!");

        foreach (var file in Directory.EnumerateFiles(path))
        {
            if (Path.GetExtension(file).StartsWith(".sfs"))
                MidiPlayerGlobal.MPTK_LoadLiveSF("file://" + file);
        }
    }

    // ============= Private Static Helper Methods ============

    #region Music Helpers

    private static void GetAllSoundObjects(string path, SoundDestiny soundDestiny)
    {
        foreach (var file in Directory.EnumerateFiles(path))
        {
            try
            {
                // Calls this to skip the file if it is not an audio file
                AssetLoader.GetAudioType(file);
            }
            catch
            {
                continue;
            }

            string fileName = Path.GetFileNameWithoutExtension(file);
            TryToExtractFloorsFromMusicName(fileName, out fileName, out var floors);

            var sd = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(file), string.Empty, SoundType.Effect, Color.white);
            sd.subtitle = false;
            sd.name = fileName;

            SoundObjectHolder holder = floors != null ? new(sd, soundDestiny, floors) : new(sd, soundDestiny);
            allSounds.Add(holder);
        }
    }

    #endregion

    // ***** MIDI Helpers *****
    #region MIDI Helpers
    private static void GetAllMIDIs(string path, MidiDestiny midiDestiny, LevelType[] types)
    {
        string[] files = Directory.GetFiles(path);
        for (int i = 0; i < files.Length; i++)
        {
            var file = files[i];
            string extension = Path.GetExtension(file);

            if (extension == ".mid" || extension == ".midi")
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                bool successToExtractFloors = TryToExtractFloorsFromMusicName(fileName, out fileName, out var floors);

                if (
                    midiDestiny == MidiDestiny.Schoolhouse && // If it is for the schoolhouse
                    successToExtractFloors && // AND there are floors to be extracted (the fileName is treated as well)
                    IfMIDIHolderExistsAlready_MergeData(fileName, floors, types)) // AND there's data that can be merged (if not, the fileName is treated anyways, so it can continue)
                    continue;

                MIDIHolder holder = types != null && floors != null ?
                    new(Return_NonDuplicated_MidiName(fileName), midiDestiny, types, floors) : // If, by the end, LevelType and Floors are a thing, the new MIDIHolder should be added here
                    new(Return_NonDuplicated_MidiName(fileName), midiDestiny);

                allMidis.Add(holder);
            }
        }

        // *** Inner Local Functions ***
        static bool IfMIDIHolderExistsAlready_MergeData(string midiName, string[] allowedFloors, LevelType[] allowedLevelTypes)
        {
            if (!_storedMidis.TryGetValue(midiName, out var MIDIHolderReference))
            {
                return false;
            }

            // School house uses a different method since it needs levelType from the directories. Therefore, it re-uses the same reference and merge the allowedLevelTypes and allowedFloors.
            for (int i = 0; i < allowedFloors.Length; i++)
                MIDIHolderReference.allowedFloors.Add(allowedFloors[i]);

            for (int i = 0; i < allowedLevelTypes.Length; i++)
                MIDIHolderReference.allowedLevelTypes.Add(allowedLevelTypes[i]);

            return true;
        }
    }

    // Specialized GetAllMIDIs for Schoolhouse ones, as they contain the LevelType thing
    private static void GetAllMIDIs_Schoolhouse(string path)
    {
        string[] levelTypeDirectories = Directory.GetDirectories(path);
        for (int i = 0; i < levelTypeDirectories.Length; i++)
        {
            string directoryName = Path.GetFileNameWithoutExtension(levelTypeDirectories[i]); // Works for directories
            // Debug.Log("Found directory for schoolhouse: " + directoryName);
            LevelType[] parsedLevelTypes;
            try
            {
                // If the LevelType is equal to a specific *keyword*, use all of the LevelTypes
                if (directoryName.ToLower() == Constants.MIDI_LOADPROCEDURE_LEVELTYPEALL_KEYWORD)
                    parsedLevelTypes = Constants.ALL_LEVELTYPES_SET;
                // Try to get the LevelType through the Directory's name
                else
                    parsedLevelTypes = [EnumExtensions.GetFromExtendedName<LevelType>(directoryName)];
            }
            catch // Any error thrown will just skip over
            {
                continue;
            }

            GetAllMIDIs(levelTypeDirectories[i], MidiDestiny.Schoolhouse, parsedLevelTypes);
        }
    }

    private static bool TryToExtractFloorsFromMusicName(string midiName, out string treatedName, out string[] extractedFloors)
    {
        extractedFloors = null;
        // Attempts to separate the name properly
        string[] separatedStrings = midiName.Split('_');

        // If there's only one item, that means the name itself doesn't have any more "floors" in it
        if (separatedStrings.Length <= 1)
        {
            treatedName = midiName;
            return false;
        }

        extractedFloors = new string[separatedStrings.Length - 1]; // The length of the "floors" splitted without the name itself
        for (int i = 1; i < separatedStrings.Length; i++)
        {
            extractedFloors[i - 1] = separatedStrings[i];
        }
        treatedName = separatedStrings[0]; // The actual name must be this treated one

        return true;
    }
    private static string Return_NonDuplicated_MidiName(string midiName)
    {
        if (_repeatedMidis.TryGetValue(midiName, out int appearances))
        {
            _repeatedMidis[midiName]++; // Add one for the next repeated name
            midiName += $"_Variant_{appearances + 1}";
            return midiName;
        }
        _repeatedMidis.Add(midiName, 1);
        return midiName;
    }

    #endregion

    // *********** MIDIs/Sounds stored here ************

    // ******* Private Structs for the system ******
    internal readonly struct MIDIHolder
    {
        public MIDIHolder(string midiName, MidiDestiny midiDestiny)
        {
            MidiName = Constants.CUSTOMMUSICS_MIDI_PREFIX_TAG + midiName;
            this.midiDestiny = midiDestiny;

            _storedMidis.Add(midiName, this);
        }

        public MIDIHolder(string midiName, MidiDestiny midiDestiny, LevelType[] allowedLevelTypes, string[] allowedFloors) : this(midiName, midiDestiny)
        {
            this.allowedFloors = allowedFloors.Length == 0 ? null : [.. allowedFloors];
            this.allowedLevelTypes = [.. allowedLevelTypes];
        }

        public readonly string MidiName = string.Empty;
        public readonly MidiDestiny midiDestiny;
        internal readonly HashSet<string> allowedFloors = null;
        internal readonly HashSet<LevelType> allowedLevelTypes = null;

        public bool CanBeInsertedOnFloor(string floor, LevelType floorType)
        {
            if (allowedFloors == null || allowedLevelTypes == null)
                return true; // If it is not a Floor-specific MIDI, it'll return true by default

            return allowedFloors.Contains(floor) && allowedLevelTypes.Contains(floorType);
        }
    }

    // ****** SoundObject Holder ******
    internal readonly struct SoundObjectHolder
    {
        public SoundObjectHolder(SoundObject soundObject, SoundDestiny soundDestiny)
        {
            this.soundDestiny = soundDestiny;

            // Default settings for the SoundObject
            soundObject.soundType = SoundType.Music;
            soundObject.subtitle = false;
            soundObject.soundKey = string.Empty;

            // Switch to handle important settings from the SoundObjects
            switch (this.soundDestiny)
            {
                case SoundDestiny.Ambience:
                    soundObject.soundType = SoundType.Effect;
                    break;
                case SoundDestiny.Playtime:
                    soundObject.color = Color.red;
                    soundObject.subtitle = true;
                    soundObject.soundKey = "Mfx_mus_Playtime";
                    break;
                case SoundDestiny.PartyEvent:
                    soundObject.subtitle = true;
                    soundObject.soundKey = "Mfx_Party";
                    break;

                default:
                    // Do nothing
                    break;
            }

            this.soundObject = soundObject;
            soundObject.name = Constants.CUSTOMMUSICS_SOUND_PREFIX_TAG + soundObject.name;
        }
        public SoundObjectHolder(SoundObject soundObject, SoundDestiny soundDestiny, params string[] allowedFloors) : this(soundObject, soundDestiny)
        {
            this.allowedFloors = [.. allowedFloors];
        }
        public readonly SoundObject soundObject = null;
        public readonly SoundDestiny soundDestiny;
        internal readonly HashSet<string> allowedFloors = null;

        public bool CanBeInsertedOnFloor(string floor)
        {
            if (allowedFloors == null)
                return true; // If it is not a Floor-specific MIDI, it'll return true by default

            return allowedFloors.Contains(floor);
        }
    }
    // Main List for midis and music
    readonly internal static List<MIDIHolder> allMidis = [];
    readonly internal static List<SoundObjectHolder> allSounds = [];
    // Required dictionaries for MIDIs
    readonly static Dictionary<string, MIDIHolder> _storedMidis = [];
    readonly static Dictionary<string, int> _repeatedMidis = [];
}