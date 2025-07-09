using System.Collections.Generic;

namespace BBPlusCustomMusics.Plugin.Public;

public class MIDIHolder
{
    internal MIDIHolder(string midiName, MidiDestiny midiDestiny, LevelType[] allowedLevelTypes, string[] allowedFloors)
    {
        MidiName = Constants.CUSTOMMUSICS_MIDI_PREFIX_TAG + midiName;
        this.midiDestiny = midiDestiny;
        this.allowedFloors = allowedFloors == null ? null : [.. allowedFloors];
        this.allowedLevelTypes = allowedLevelTypes == null ? null : [.. allowedLevelTypes];
    }
    public string MidiName = string.Empty;
    public readonly MidiDestiny midiDestiny;
    // Setting either of these to null for MIDIs that need them just make them appear in every floor and level type.
    // Yet, an important reminder that MIDIHolder shouldn't really be edited unless there's a specific use for them.
    public HashSet<string> allowedFloors = null;
    public HashSet<LevelType> allowedLevelTypes = null;

    public bool CanBeInsertedOnFloor(string floor, LevelType floorType) =>
        (allowedFloors == null || allowedFloors.Contains(floor)) && (allowedLevelTypes == null || allowedLevelTypes.Contains(floorType));


    internal void InitializeFloorFeaturesIfNotReady()
    {
        allowedFloors ??= [];
        allowedLevelTypes ??= [];
    }
}
