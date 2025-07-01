using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BBPlusCustomMusics.Patches
{
    /// <summary>
    /// Patch for custom minigame music (non-tutorial field trip music).
    /// </summary>
    [HarmonyPatch(typeof(MinigameBase), "StartMinigame")]
    internal static class MinigamePatch
    {
        [HarmonyPrefix]
        static void Minigame(ref string ___midiSong)
        {
            // Gather all non-tutorial field trip midis
            List<string> midis = [];

            for (int i = 0; i < CustomMusicPlug.fieldTripMidis.Count; i++)
            {
                if (!CustomMusicPlug.fieldTripMidis[i].Value)
                    midis.Add(CustomMusicPlug.fieldTripMidis[i].Key);
            }

            if (midis.Count == 0)
                return;

            int idx = Random.Range(0, midis.Count + (MusicsOptionsCat.values[0] ? 0 : 1));
            if (idx >= midis.Count)
                return;

            ___midiSong = midis[idx];
        }
    }
}
