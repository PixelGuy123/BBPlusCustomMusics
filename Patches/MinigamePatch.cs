using System.Collections.Generic;
using System.Linq;
using BBPlusCustomMusics.Plugin;
using BBPlusCustomMusics.Plugin.Public;
using HarmonyLib;
using UnityEngine;

namespace BBPlusCustomMusics.Patches
{
    // Patch for custom minigame music (non-tutorial field trip music).
    [HarmonyPatch(typeof(MinigameBase), "StartMinigame")]
    internal static class MinigamePatch
    {
        [HarmonyPrefix]
        static void Minigame(ref string ___midiSong)
        {
            _midis ??= [
                ..MusicRegister.allMidis
                    .Where(x => x.midiDestiny == MidiDestiny.FieldTrip_Minigame)
                    .Select(x => x.MidiName)
            ];

            if (_midis.Count == 0)
                return;

            int idx = Random.Range(0, _midis.Count + (MusicsOptionsCat.ForceOnlyCustomMusics ? 0 : 1));

            if (idx >= _midis.Count)
                return;

            ___midiSong = _midis[idx];
        }

        static List<string> _midis = null;
    }
}
