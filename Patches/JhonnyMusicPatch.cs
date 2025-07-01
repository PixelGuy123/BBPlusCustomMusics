using HarmonyLib;
using UnityEngine;

namespace BBPlusCustomMusics.Patches
{
    /// <summary>
    /// Patch to play custom Jhonny music when the store room is opened.
    /// </summary>
    [HarmonyPatch(typeof(StoreRoomFunction), "Open")]
    internal static class JhonnyMusicPatch
    {
        [HarmonyPrefix]
        static void JhonnyMusic(AudioManager ___alarmAudioManager)
        {
            // If no custom Jhonny musics, do nothing
            if (CustomMusicPlug.jhonnyMusic.Count == 0)
                return;
            // Randomly select a custom Jhonny music
            int idx = Random.Range(0, CustomMusicPlug.jhonnyMusic.Count);
            // Flush the queue and queue the new music
            ___alarmAudioManager.FlushQueue(true);
            ___alarmAudioManager.QueueAudio(CustomMusicPlug.jhonnyMusic[idx]);
            ___alarmAudioManager.SetLoop(true);
        }
    }
}
