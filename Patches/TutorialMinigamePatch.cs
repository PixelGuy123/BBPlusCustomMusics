using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BBPlusCustomMusics.Patches
{
    /// <summary>
    /// Patch for custom tutorial minigame music.
    /// </summary>
    [HarmonyPatch(typeof(MinigameTutorial), "Initialize")]
    internal static class TutorialMinigamePatch
    {
        [HarmonyPrefix]
        static void TutorialMinigame(MinigameTutorial __instance)
        {
            // Gather all tutorial field trip midis
            List<string> midis = [];
            for (int i = 0; i < CustomMusicPlug.fieldTripMidis.Count; i++)
                if (CustomMusicPlug.fieldTripMidis[i].Value)
                    midis.Add(CustomMusicPlug.fieldTripMidis[i].Key);
            if (midis.Count == 0)
                return;
            int idx = Random.Range(0, midis.Count + (MusicsOptionsCat.values[0] ? 0 : 1));
            if (idx >= midis.Count)
                return;
            __instance.music = midis[idx];
        }
    }
}
