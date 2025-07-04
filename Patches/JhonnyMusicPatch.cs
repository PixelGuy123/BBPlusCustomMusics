using System.Collections.Generic;
using System.Linq;
using BBPlusCustomMusics.Plugin.Public;
using HarmonyLib;
using UnityEngine;

namespace BBPlusCustomMusics.Patches
{
    // Patch to play custom Jhonny music when the store room is opened.
    [HarmonyPatch(typeof(StoreRoomFunction), "Open")]
    internal static class JhonnyMusicPatch
    {
        [HarmonyPrefix]
        static void JhonnyMusic(AudioManager ___alarmAudioManager)
        {
            _johnnyMusics ??= [.. MusicRegister.allSounds
                .Where(x => x.soundDestiny == SoundDestiny.JohnnyStore)
                .Select(x => x.soundObject)];

            // If no custom Jhonny musics, do nothing
            if (_johnnyMusics.Count == 0)
                return;
            // Randomly select a custom Jhonny music
            int idx = Random.Range(0, _johnnyMusics.Count);
            // Flush the queue and queue the new music
            ___alarmAudioManager.FlushQueue(true);
            ___alarmAudioManager.QueueAudio(_johnnyMusics[idx]);
            ___alarmAudioManager.SetLoop(true);
        }

        static List<SoundObject> _johnnyMusics = null;
    }
}
