using System.Collections.Generic;
using System.Linq;
using BBPlusCustomMusics.Plugin;
using BBPlusCustomMusics.Plugin.Public;
using HarmonyLib;
using UnityEngine;

namespace BBPlusCustomMusics.Patches
{
    // Patch for custom tutorial minigame music.
    [HarmonyPatch(typeof(MinigameTutorial), "Initialize")]
    internal static class TutorialMinigamePatch
    {
        [HarmonyPrefix]
        static void TutorialMinigame(MinigameTutorial __instance)
        {
            _midis ??= [
                ..MusicRegister.allMidis
                    .Where(x => x.midiDestiny == MidiDestiny.FieldTrip_Tutorial)
                    .Select(x => x.MidiName)
            ];

            if (_midis.Count == 0)
                return;

            int idx = Random.Range(0, _midis.Count + (MusicsOptionsCat.ForceOnlyCustomMusics ? 0 : 1));
            if (idx >= _midis.Count)
                return;

            __instance.music = _midis[idx];
        }

        static List<string> _midis = null;
    }
}
