using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.SaveSystem;
using UnityEngine;

namespace BBPlusCustomMusics.Plugin
{
    internal class MusicsOptionsCat : CustomOptionsCategory
    {
        public override void Build()
        {
            var ogToggle = CreateToggle("OgMusicToggle", "CstMscs_Opts_OgMusicToggle", values[0], Vector3.up * 25f, 195f);
            AddTooltip(ogToggle, "CstMscs_Opts_ToolTip_OgMusicToggle");

            AddTooltip(CreateApplyButton(() =>
            {
                values[0] = ogToggle.Value;
                ModdedSaveSystem.CallSaveLoadAction(CustomMusicPlug.i, true, ModdedSaveSystem.GetCurrentSaveFolder(CustomMusicPlug.i));
            }), "CstMscs_Opts_ToolTip_Apply");
        }

        internal static bool[] values = new bool[1]; // only one value for now lol

        public static bool ForceOnlyCustomMusics => values[0];
    }
}