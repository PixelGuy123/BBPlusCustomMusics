using System.Collections.Generic;

namespace BBPlusCustomMusics.Plugin.Public;

// ******* Private Structs for the system ******
public struct MIDIHolder
{
    internal MIDIHolder(string midiName, MidiDestiny midiDestiny)
    {
        MidiName = Constants.CUSTOMMUSICS_MIDI_PREFIX_TAG + midiName;
        this.midiDestiny = midiDestiny;
    }

    internal MIDIHolder(string midiName, MidiDestiny midiDestiny, LevelType[] allowedLevelTypes, string[] allowedFloors) : this(midiName, midiDestiny)
    {
        this.allowedFloors = allowedFloors.Length == 0 ? null : [.. allowedFloors];
        this.allowedLevelTypes = [.. allowedLevelTypes];
    }

    public string MidiName = string.Empty;
    public readonly MidiDestiny midiDestiny;
    // Setting either of these to null for MIDIs that need them just make them appear in every floor and level type.
    // Yet, an important reminder that MIDIHolder shouldn't really be edited unless there's a specific use for them.
    public HashSet<string> allowedFloors = null;
    public HashSet<LevelType> allowedLevelTypes = null;

    public bool CanBeInsertedOnFloor(string floor, LevelType floorType)
    {
        if (allowedFloors == null || allowedLevelTypes == null)
            return true; // If it is not a Floor-specific MIDI, it'll return true by default

        return allowedFloors.Contains(floor) && allowedLevelTypes.Contains(floorType);
    }

    internal void InitializeFloorFeaturesIfNotReady()
    {
        allowedFloors ??= [];
        allowedLevelTypes ??= [];
    }
}
