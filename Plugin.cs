using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using MidiPlayerTK;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace BBPlusCustomMusics
{
	[BepInPlugin("pixelguy.pixelmodding.baldiplus.custommusics", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
	public class CustomMusicPlug : BaseUnityPlugin
	{
		private void Awake()
		{
			new Harmony("pixelguy.pixelmodding.baldiplus.custommusics").PatchAll();
			string p = AssetLoader.GetModPath(this);
			AddMidisFromDirectory(false, p, "schoolMusics");
			AddMidisFromDirectory(true, p, "elevatorMusics");
			AddSoundFontsFromDirectory(p, "sfsFiles");

			LoadingEvents.RegisterOnAssetsLoaded(Info, () =>
			{
				var spr = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(Path.Combine(p, "boomBox.png")), 146);
				foreach (var el in Resources.FindObjectsOfTypeAll<ElevatorScreen>())
				{
					var boomBox = new GameObject("ElevatorBoomBox").AddComponent<Image>();
					boomBox.gameObject.layer = LayerMask.NameToLayer("UI");
					boomBox.material = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == "UI_AsSprite");
					boomBox.sprite = spr;
					boomBox.transform.SetParent(el.transform.Find("ElevatorTransission"));
					boomBox.transform.SetSiblingIndex(6); // Apparently this affects the render order, huh
					boomBox.transform.localScale = Vector3.one;
					boomBox.transform.localPosition = new(-77.74f, 93.05f);
					boomBox.gameObject.AddComponent<BoomBox>();
				}
			}, false);

			usingEndless = Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.endlessfloors");

			if (usingEndless)
			{  // endless floors confirmation :O
				GeneratorManagement.Register(this, GenerationModType.Finalizer, (name, num, lvlobj) =>
				{
					MusicalInjection.overridingMidis.Clear();
					foreach (var midi in midis)
					{
						if (midi.Value) continue;
						foreach (var data in midi.Key.Split('_'))
						{
							if (!data.StartsWith("INF")) 
								continue;
							string[] nums = data.Trim('I', 'N', 'F').Split('-');


							if (int.TryParse(nums[0], out int n) && n <= num && (nums.Length == 1 || int.TryParse(nums[1], out int n2) && n2 >= num))
							{
								MusicalInjection.overridingMidis.Add(midi.Key);
								break;
							}
						}
					}
				});
			}
		}

		public static void AddMidisFromDirectory(bool isElevatorMusic, params string[] paths)
		{
			string path = Path.Combine(paths);
			if (!Directory.Exists(path))
				throw new System.ArgumentException($"The directory to load midis from path ({path}) doesn\'t exist!");

			foreach (var file in Directory.EnumerateFiles(path))
			{
				string extension = Path.GetExtension(file);
				if (extension == ".mid" || extension == ".midi")
					midis.Add(new(AssetLoader.MidiFromFile(file, Path.GetFileNameWithoutExtension(file)), isElevatorMusic));

			}
		}

		public static void AddSoundFontsFromDirectory(params string[] paths)
		{
			string path = Path.Combine(paths);
			if (!Directory.Exists(path))
				throw new System.ArgumentException($"The directory to load soundfonts from path ({path}) doesn\'t exist!");

			foreach (var file in Directory.EnumerateFiles(path))
			{
				if (Path.GetExtension(file).StartsWith(".sfs"))
					MidiPlayerGlobal.MPTK_LoadLiveSF("file://" + file);
			}


		}

		internal static List<KeyValuePair<string, bool>> midis = []; // bool indicates whether it is elevator music or not

		internal static bool usingEndless = false;
	}

	// Boom Box

	public class BoomBox : MonoBehaviour
	{
		void MidiEvent(MPTKEvent midiEvent)
		{
			if (midiEvent.Command == MPTKCommand.NoteOn || midiEvent.Command == MPTKCommand.NoteOff)
			{
				transform.localScale += 1 / midiEvent.Value * Vector3.one;
				if (transform.localScale.magnitude > maxLimit)
					transform.localScale = Vector3.one * maxLimit;
				axisOffset += midiEvent.Value * UnityEngine.Random.Range(-1, 2);
				axisOffset = Mathf.Clamp(axisOffset, -axisLimit, axisLimit);
			}

		}

		void OnEnable() =>
			MusicManager.OnMidiEvent += MidiEvent;

		void OnDisable() =>
			MusicManager.OnMidiEvent -= MidiEvent;


		void Update()
		{
			transform.localScale += ((Vector3.one * minLimit) - transform.localScale) * 3.8f * Time.unscaledDeltaTime;
			if (transform.localScale.magnitude < minLimit)
				transform.localScale = Vector3.one * minLimit;
			axisOffset += (1f - axisOffset) * 3.4f * Time.unscaledDeltaTime;
			transform.rotation = Quaternion.Euler(0f, 0f, axisOffset);
		}

		float axisOffset = 0f;

		const float maxLimit = 0.65f, minLimit = 0.5f, axisLimit = 15f;

	}


	// *************************** Patches **********************************

	[HarmonyPatch]
	internal static class MusicalInjection
	{
		internal static List<string> overridingMidis = [];
		[HarmonyPatch(typeof(MainGameManager), "BeginPlay")]
		[HarmonyPatch(typeof(EndlessGameManager), "BeginPlay")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> MusicChanges(IEnumerable<CodeInstruction> instructions) =>
			new CodeMatcher(instructions)
			.MatchForward(false, new CodeMatch(OpCodes.Ldstr, "school", "school"))
			.SetInstruction(Transpilers.EmitDelegate((MainGameManager man) =>
			{
				List<string> midis = new(!CustomMusicPlug.usingEndless ? CustomMusicPlug.midis.Where(x => !x.Value).Select(x => x.Key) :
					overridingMidis); // Gets the non elevator musics

				if (!CustomMusicPlug.usingEndless)
				{
					for (int i = 0; i < midis.Count; i++)
					{
						string[] midiData = midis[i].Split('_');
						if (midiData.Length == 1) continue; // All floors lul


						if (!midiData.Contains(Singleton<CoreGameManager>.Instance.sceneObject.levelTitle))
						{
							midis.RemoveAt(i--);
							continue;
						}

					}
				}
#if DEBUG
				Debug.Log("Midis inside:");
				midis.Do(x => Debug.Log(x));
#endif

				if (midis.Count == 0)
					return "school";

				var rng = new System.Random(Singleton<CoreGameManager>.Instance.Seed());
				for (int i = 0; i < Singleton<CoreGameManager>.Instance.sceneObject.levelNo; i++)
					rng.Next();
#if DEBUG
				int idx = rng.Next(midis.Count);
#else
				int idx = rng.Next(midis.Count + 1);
#endif
				if (idx >= midis.Count)
					return "school";

				return midis[idx];
			}))
			.Insert([new(OpCodes.Ldarg_0)]) // Add the GameManager instance
			.InstructionEnumeration();


		[HarmonyPatch(typeof(ElevatorScreen), "ZoomIntro", MethodType.Enumerator)]
		internal class ElevatorScreenPatch
		{
			private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
				new CodeMatcher(instructions)
				.MatchForward(false, new CodeMatch(OpCodes.Ldstr, "Elevator", "Elevator"))
				.SetInstruction(Transpilers.EmitDelegate(() =>
				{

					List<string> midis = new(CustomMusicPlug.midis.Where(x => x.Value).Select(x => x.Key)); // Gets the elevator musics

					int idx = UnityEngine.Random.Range(0, midis.Count + 1);
					if (idx >= midis.Count)
						return "Elevator";
					return midis[idx];
				}))
				.InstructionEnumeration();

			private static void Postfix() =>
				Singleton<MusicManager>.Instance.StopFile(); // Forgot about this
		}
	}
}
