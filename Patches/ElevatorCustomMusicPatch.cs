using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace BBPlusCustomMusics.Patches
{
    /// <summary>
    /// Transpiler patch for custom elevator music selection.
    /// </summary>
    [HarmonyPatch]
    internal static class ElevatorCustomMusicPatch
    {
        [HarmonyPatch(typeof(ElevatorScreen), "ZoomIntro", MethodType.Enumerator)]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ElevatorCustomMusic(IEnumerable<CodeInstruction> instructions)
        {
            // Replace the hardcoded "Elevator" music selection with custom logic
            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Ldstr, "Elevator", "Elevator"))
                .SetInstruction(Transpilers.EmitDelegate(() =>
                {
                    List<string> midis = [.. CustomMusicPlug.allMidis
                        .Where(x => x.Settings.Destiny == CustomMusicPlug.MidiDestiny.Elevator)
                        .Select(x => x.MidiName)
                        ];

                    if (midis.Count == 0)
                        return "Elevator";

                    int idx = Random.Range(0, midis.Count + (MusicsOptionsCat.values[0] ? 0 : 1));
                    if (idx >= midis.Count)
                        return "Elevator";

                    return midis[idx];
                }))
                .InstructionEnumeration();
        }
    }
}
