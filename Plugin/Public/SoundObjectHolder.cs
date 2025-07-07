using System.Collections.Generic;
using UnityEngine;

namespace BBPlusCustomMusics.Plugin.Public;


// ****** SoundObject Holder ******
public struct SoundObjectHolder
{
    internal SoundObjectHolder(SoundObject soundObject, SoundDestiny soundDestiny)
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
    internal SoundObjectHolder(SoundObject soundObject, SoundDestiny soundDestiny, params string[] allowedFloors) : this(soundObject, soundDestiny)
    {
        this.allowedFloors = [.. allowedFloors];
    }
    public readonly SoundObject soundObject = null;
    public readonly SoundDestiny soundDestiny;
    public HashSet<string> allowedFloors = null;

    public bool CanBeInsertedOnFloor(string floor)
    {
        if (allowedFloors == null)
            return true; // If it is not a Floor-specific MIDI, it'll return true by default

        return allowedFloors.Contains(floor);
    }
}
