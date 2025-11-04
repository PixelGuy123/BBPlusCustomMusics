using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BBPlusCustomMusics.Plugin;
using BBPlusCustomMusics.Plugin.Public;
using HarmonyLib;
using UnityEngine;

namespace BBPlusCustomMusics.Patches
{
    // Transpiler patch for custom elevator music selection.
    [HarmonyPatch]
    internal static class ElevatorCustomMusicPatch
    {
        [HarmonyPatch(typeof(ElevatorScreen), "ZoomIntro", MethodType.Enumerator)]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ElevatorCustomMusic(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Ldstr, "Elevator", "Elevator"))
                .SetInstruction(Transpilers.EmitDelegate(GetNewRandomElevatorMidi))
                .InstructionEnumeration();
        }

        public static string GetNewRandomElevatorMidi()
        {
            _midis ??= [.. MusicRegister.allMidis
                        .Where(x => x.midiDestiny == MidiDestiny.Elevator)
                        .Select(x => x.MidiName)
                    ];

            if (_midis.Count == 0)
                return "Elevator";

            int idx = Random.Range(0, _midis.Count + (MusicsOptionsCat.ForceOnlyCustomMusics ? 0 : 1));
            while (_midis.Count != 1 && idx == lastUsedIndex)
                idx = Random.Range(0, _midis.Count + (MusicsOptionsCat.ForceOnlyCustomMusics ? 0 : 1));
            if (idx >= _midis.Count)
                return "Elevator";

            lastUsedIndex = idx;
            return _midis[idx];
        }

        static int lastUsedIndex = -1;

        static List<string> _midis = null;
    }
}
