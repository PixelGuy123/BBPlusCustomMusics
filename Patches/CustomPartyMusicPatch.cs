using HarmonyLib;
using UnityEngine;

namespace BBPlusCustomMusics.Patches
{
    /// <summary>
    /// Patch for custom party event music.
    /// </summary>
    [HarmonyPatch(typeof(PartyEvent), "Begin")]
    internal static class CustomPartyMusicPatch
    {
        [HarmonyPrefix]
        static void CustomPartyMusic(ref SoundObject ___musParty)
        {
            // If no custom party musics, do nothing
            if (CustomMusicPlug.partyEventMusics.Count == 0)
                return;
            // Randomly select a custom party music
            int idx = Random.Range(0, CustomMusicPlug.partyEventMusics.Count + (MusicsOptionsCat.values[0] ? 0 : 1));
            if (idx >= CustomMusicPlug.partyEventMusics.Count)
                return;
            ___musParty = CustomMusicPlug.partyEventMusics[idx];
        }
    }
}
