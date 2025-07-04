using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BBPlusCustomMusics.Plugin;
using BBPlusCustomMusics.Plugin.Public;
using HarmonyLib;

namespace BBPlusCustomMusics.Patches
{
    // Transpiler patch for custom music selection in MainGameManager and EndlessGameManager.
    [HarmonyPatch]
    internal static class MusicChangesPatch
    {
        [HarmonyPatch(typeof(MainGameManager), "BeginPlay")]
        [HarmonyPatch(typeof(EndlessGameManager), "BeginPlay")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> MusicChanges(IEnumerable<CodeInstruction> instructions)
        {
            // Replace the hardcoded "school" music selection with custom logic
            return new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Ldstr, "school", "school"))
                .SetInstruction(Transpilers.EmitDelegate((MainGameManager man) =>
                {
                    var sco = Singleton<CoreGameManager>.Instance.sceneObject;
                    LevelTypeAndTitleSet midiSettings = new(sco.levelTitle, man.levelObject.type);

                    if (!_midiPairs.TryGetValue(midiSettings, out var midis))
                    {
                        midis = [..
                        MusicRegister.allMidis
                        .Where(x => x.midiDestiny == MidiDestiny.Schoolhouse && x.CanBeInsertedOnFloor(sco.levelTitle, man.levelObject.type))
                        .Select(x => x.MidiName)
                        ];
                        _midiPairs.Add(midiSettings, midis);
                    }

                    if (midis.Count == 0)
                        return "school";

                    var rng = new System.Random(Singleton<CoreGameManager>.Instance.Seed());
                    for (int i = 0; i < sco.levelNo; i++)
                        rng.Next();

                    int idx = rng.Next(midis.Count + (MusicsOptionsCat.ForceOnlyCustomMusics ? 0 : 1));

                    if (idx >= midis.Count)
                        return "school";

                    return midis[idx];
                }))
                .Insert([new(OpCodes.Ldarg_0)]) // Add the GameManager instance
                .InstructionEnumeration();
        }

        // To save memory as creating new collections each time can be a little too costly
        readonly static Dictionary<LevelTypeAndTitleSet, List<string>> _midiPairs = [];
        private record struct LevelTypeAndTitleSet(string LevelTitle, LevelType LevelType); // Quick way to store two things for a better equality check and storage
    }
}
