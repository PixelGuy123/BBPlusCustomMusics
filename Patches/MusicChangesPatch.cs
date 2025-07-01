using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace BBPlusCustomMusics.Patches
{
    /// <summary>
    /// Transpiler patch for custom music selection in MainGameManager and EndlessGameManager.
    /// </summary>
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
                    // Get all non-elevator custom midis
                    List<string> midis = [.. CustomMusicPlug.midis.Where(x => !x.Value).Select(x => x.Key)];
                    for (int i = 0; i < midis.Count; i++)
                    {
                        string[] midiData = midis[i].Split('_');

                        if (midiData.Length == 1) continue;

                        if (!midiData.Contains(Singleton<CoreGameManager>.Instance.sceneObject.levelTitle))
                        {
                            midis.RemoveAt(i--);
                            continue;
                        }
                    }

                    if (midis.Count == 0)
                        return "school";

                    var rng = new System.Random(Singleton<CoreGameManager>.Instance.Seed());
                    for (int i = 0; i < Singleton<CoreGameManager>.Instance.sceneObject.levelNo; i++)
                        rng.Next();

                    int idx = rng.Next(midis.Count + (MusicsOptionsCat.values[0] ? 0 : 1));
                    if (idx >= midis.Count)
                        return "school";

                    return midis[idx];
                }))
                .Insert([new(OpCodes.Ldarg_0)]) // Add the GameManager instance
                .InstructionEnumeration();
        }
    }
}
