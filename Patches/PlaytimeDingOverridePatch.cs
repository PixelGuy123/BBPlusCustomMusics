using System.Collections.Generic;
using System.Linq;
using BBPlusCustomMusics.Plugin;
using BBPlusCustomMusics.Plugin.Public;
using HarmonyLib;
using UnityEngine;

namespace BBPlusCustomMusics.Patches
{
    // Patch for Playtime's music override with custom music.
    [HarmonyPatch(typeof(Playtime), "Initialize")]
    internal static class PlaytimeDingOverridePatch
    {
        [HarmonyPostfix]
        static void PlaytimeDingOverride(Playtime __instance)
        {
            _dingsToPlay ??= [.. MusicRegister.allSounds
                .Where(x => x.soundDestiny == SoundDestiny.Playtime)
                .Select(x => x.soundObject)];

            // If no custom playtime musics, do nothing
            if (_dingsToPlay.Count == 0)
                return;

            // Randomly select a custom playtime music
            int idx = Random.Range(0, _dingsToPlay.Count + (MusicsOptionsCat.ForceOnlyCustomMusics ? 0 : 1));
            if (idx >= _dingsToPlay.Count)
                return;

            // Find the AudioManager with a single soundOnStart
            AudioManager audMan = null;
            foreach (var aud in __instance.GetComponents<AudioManager>())
            {
                if (aud.soundOnStart != null &&
                aud.soundOnStart.Length == 1 &&
                aud.loopOnStart) // Find the one that is technically the music
                {
                    audMan = aud;
                    break;
                }
            }

            if (audMan == null)
                return;

            // Set the custom music
            audMan.soundOnStart[0] = _dingsToPlay[idx];
        }

        static List<SoundObject> _dingsToPlay = null;
    }
}
