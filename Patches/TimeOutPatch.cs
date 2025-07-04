using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BBPlusCustomMusics.Plugin;
using BBPlusCustomMusics.Plugin.Public;
using HarmonyLib;
using UnityEngine;

namespace BBPlusCustomMusics.Patches
{
    [HarmonyPatch]
    internal static class TimeOutPatch
    {
        [HarmonyPatch(typeof(TimeOut), "Begin")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ElevatorCustomMusic(IEnumerable<CodeInstruction> instructions)
        {
            // Replace the hardcoded "TimeOut" music selection with custom logic
            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Ldstr, "TimeOut_MMP_Corrected", "TimeOut_MMP_Corrected"))
                .SetInstruction(Transpilers.EmitDelegate(() =>
                {
                    _midis ??= [.. MusicRegister.allMidis
                        .Where(x => x.midiDestiny == MidiDestiny.TimeOut)
                        .Select(x => x.MidiName)
                    ];

                    if (_midis.Count == 0)
                        return "TimeOut_MMP_Corrected";

                    int idx = Random.Range(0, _midis.Count + (MusicsOptionsCat.ForceOnlyCustomMusics ? 0 : 1));
                    if (idx >= _midis.Count)
                        return "TimeOut_MMP_Corrected";

                    return _midis[idx];
                }))
                .InstructionEnumeration();
        }

        static List<string> _midis = null;
    }
}
