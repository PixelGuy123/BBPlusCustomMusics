using BBPlusCustomMusics.Plugin;
using HarmonyLib;
using UnityEngine;

namespace BBPlusCustomMusics.Patches
{
    // Handles the creation and placement of the custom elevator boom box UI element.
    [HarmonyPatch(typeof(ElevatorScreen), "Initialize")]
    internal static class ElevatorBoomBoxPatch
    {
        [HarmonyPrefix]
        static void ElevatorBoomBoxThing(ElevatorScreen __instance)
        {
            // Instantiate and set up the custom boom box UI in the elevator screen
            var boomBox = Object.Instantiate(CustomMusicPlug.boomBoxPre);
            boomBox.transform.SetParent(__instance.transform.Find("ElevatorTransission"));
            boomBox.transform.SetSiblingIndex(6); // Controls render order
            boomBox.transform.localScale = Vector3.one;
            boomBox.transform.localPosition = new(-77.74f, 93.05f);
        }
    }
}
