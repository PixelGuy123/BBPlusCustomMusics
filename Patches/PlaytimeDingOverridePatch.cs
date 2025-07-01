using HarmonyLib;
using UnityEngine;

namespace BBPlusCustomMusics.Patches
{
    /// <summary>
    /// Patch for Playtime's music override with custom music.
    /// </summary>
    [HarmonyPatch(typeof(Playtime), "Initialize")]
    internal static class PlaytimeDingOverridePatch
    {
        [HarmonyPostfix]
        static void PlaytimeDingOverride(Playtime __instance)
        {
            // If no custom playtime musics, do nothing
            if (CustomMusicPlug.playtimeMusics.Count == 0)
                return;
            // Randomly select a custom playtime music
            int idx = Random.Range(0, CustomMusicPlug.playtimeMusics.Count + (MusicsOptionsCat.values[0] ? 0 : 1));
            if (idx >= CustomMusicPlug.playtimeMusics.Count)
                return;
            // Find the AudioManager with a single soundOnStart
            AudioManager audMan = null;
            foreach (var aud in __instance.GetComponents<AudioManager>())
            {
                if (aud.soundOnStart != null && aud.soundOnStart.Length == 1)
                {
                    audMan = aud;
                    break;
                }
            }
            if (audMan == null)
                return;
            // Set the custom music
            audMan.soundOnStart[0] = CustomMusicPlug.playtimeMusics[idx];
        }
    }
}
