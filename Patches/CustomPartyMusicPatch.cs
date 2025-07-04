using System.Collections.Generic;
using System.Linq;
using BBPlusCustomMusics.Plugin;
using BBPlusCustomMusics.Plugin.Public;
using HarmonyLib;
using UnityEngine;

namespace BBPlusCustomMusics.Patches
{
    // Patch for custom party event music.
    [HarmonyPatch(typeof(PartyEvent), "Begin")]
    internal static class CustomPartyMusicPatch
    {
        [HarmonyPrefix]
        static void CustomPartyMusic(ref SoundObject ___musParty)
        {
            _partyMusics ??= [.. MusicRegister.allSounds
                .Where(x => x.soundDestiny == SoundDestiny.PartyEvent)
                .Select(x => x.soundObject)];
            // If no custom party musics, do nothing
            if (_partyMusics.Count == 0)
                return;
            // Randomly select a custom party music
            int idx = Random.Range(0, _partyMusics.Count + (MusicsOptionsCat.ForceOnlyCustomMusics ? 0 : 1));
            if (idx >= _partyMusics.Count)
                return;
            ___musParty = _partyMusics[idx];
        }

        static List<SoundObject> _partyMusics = null;
    }
}
