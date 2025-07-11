using HarmonyLib;

namespace BBPlusCustomMusics.Patches
{
    // Patch to flush the alarm audio queue when alarm is set off or closed.
    [HarmonyPatch(typeof(StoreRoomFunction), "SetOffAlarm")]
    [HarmonyPatch(typeof(StoreRoomFunction), "Close")]
    internal static class AlarmOffPatch
    {
        [HarmonyPrefix]
        static void AlarmOff(AudioManager ___alarmAudioManager)
        {
            // Flush the alarm audio queue
            ___alarmAudioManager.FlushQueue(true);
        }
    }
}
