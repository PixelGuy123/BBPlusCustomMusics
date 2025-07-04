using HarmonyLib;

namespace BBPlusCustomMusics.Patches
{
    // Ensures music is stopped after elevator transition.
    [HarmonyPatch(typeof(ElevatorScreen), "ZoomIntro", MethodType.Enumerator)]
    internal static class StopMusicAfterPatch
    {
        [HarmonyPostfix]
        private static void StopMusicAfter()
        {
            // Stop any playing music after elevator transition
            Singleton<MusicManager>.Instance.StopFile();
        }
    }
}
