using HarmonyLib;
using MTM101BaldAPI;

namespace BBPlusCustomMusics.Patches
{
    /// <summary>
    /// Ensures music is stopped after elevator transition.
    /// </summary>
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
