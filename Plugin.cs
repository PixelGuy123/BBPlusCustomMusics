using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using MidiPlayerTK;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace BBPlusCustomMusics
{
	[BepInPlugin("pixelguy.pixelmodding.baldiplus.custommusics", PluginInfo.PLUGIN_NAME, "1.0.5")]
	[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]

	[BepInDependency("mtm101.rulerp.baldiplus.endlessfloors", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("Rad.cmr.baldiplus.arcaderenovations", BepInDependency.DependencyFlags.SoftDependency)]
	public class CustomMusicPlug : BaseUnityPlugin
	{
		private void Awake()
		{
			new Harmony("pixelguy.pixelmodding.baldiplus.custommusics").PatchAll();
			string p = AssetLoader.GetModPath(this);
			AddAmbiencesFromDirectory(p, "ambiences");
			AddMidisFromDirectory(false, p, "schoolMusics");
			AddMidisFromDirectory(true, p, "elevatorMusics");
			AddSoundFontsFromDirectory(p, "sfsFiles");

			LoadingEvents.RegisterOnAssetsLoaded(Info, () =>
			{
				var spr = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(Path.Combine(p, "boomBox.png")), 146);
				var boomBox = new GameObject("ElevatorBoomBox").AddComponent<Image>();
				boomBox.gameObject.layer = LayerMask.NameToLayer("UI");
				boomBox.material = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == "UI_AsSprite");
				boomBox.sprite = spr;
				boomBox.gameObject.ConvertToPrefab(true);
				boomBoxPre = boomBox.gameObject.AddComponent<BoomBox>();

			}, false);

			usingEndless = Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.endlessfloors") || Chainloader.PluginInfos.ContainsKey("Rad.cmr.baldiplus.arcaderenovations");

			GeneratorManagement.Register(this, GenerationModType.Finalizer, (levelTitle, num, sco) =>
			{
				if (usingEndless)
				{
					// endless floors confirmation :O
					InfiniteFloorsInjection(num);
					return;
				}

				if (sco.manager is MainGameManager man)
				{
					var ambienceNoises = new List<SoundObject>(CustomMusicPlug.ambienceNoises);
					for (int i = 0; i < ambienceNoises.Count; i++)
					{
						string[] midiData = ambienceNoises[i].name.Split('_');
						if (midiData.Length == 1) continue; // All floors lul


						if (!midiData.Contains(levelTitle))
						{
							ambienceNoises.RemoveAt(i--);
							continue;
						}

					}

					man.ambience.sounds = man.ambience.sounds.AddRangeToArray([.. ambienceNoises]);
					return;
				}

				if (sco.manager is EndlessGameManager eman)
				{
					var ambienceNoises = new List<SoundObject>(CustomMusicPlug.ambienceNoises);
					for (int i = 0; i < ambienceNoises.Count; i++)
					{
						string[] midiData = ambienceNoises[i].name.Split('_');
						if (midiData.Length == 1) continue; // All floors lul


						if (!midiData.Contains(levelTitle))
						{
							ambienceNoises.RemoveAt(i--);
							continue;
						}

					}

					eman.ambience.sounds = eman.ambience.sounds.AddRangeToArray([.. ambienceNoises]);
					return;
				}
			});




		}
		void InfiniteFloorsInjection(int num)
		{
			MusicalInjection.overridingMidis.Clear();
			MusicalInjection.overridingAmbienceSounds.Clear();
			foreach (var midi in midis)
			{
				if (midi.Value) continue;
				foreach (var data in midi.Key.Split('_'))
				{
					if (!data.StartsWith("INF"))
						continue;
					string[] nums = data.Trim('I', 'N', 'F').Split('-');


					if (int.TryParse(nums[0], out int n) && n <= num && (nums.Length == 1 || (int.TryParse(nums[1], out int n2) && n2 >= num)))
					{
						MusicalInjection.overridingMidis.Add(midi.Key);
						break;
					}
				}
			}

			foreach (var midi in ambienceNoises) // SoundObject type
			{
				foreach (var data in midi.name.Split('_'))
				{
					if (!data.StartsWith("INF"))
						continue;
					string[] nums = data.Trim('I', 'N', 'F').Split('-');


					if (int.TryParse(nums[0], out int n) && n <= num && (nums.Length == 1 || (int.TryParse(nums[1], out int n2) && n2 >= num)))
					{
						MusicalInjection.overridingAmbienceSounds.Add(midi);
						break;
					}
				}
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
				{
					string name = customMusicsPrefix + Path.GetFileNameWithoutExtension(file);
					if (!repeatedMidis.TryGetValue(name, out int val))
					{
						repeatedMidis.Add(name, 0);
						val = 0;
					}

					midis.Add(new(AssetLoader.MidiFromFile(file, name + $"_{++val}"), isElevatorMusic));

					repeatedMidis[name]++;
				}

			}
		}

		public static void AddAmbiencesFromDirectory(params string[] paths)
		{
			string path = Path.Combine(paths);
			if (!Directory.Exists(path))
				throw new System.ArgumentException($"The directory to load ambience sounds from path ({path}) doesn\'t exist!");

			foreach (var file in Directory.EnumerateFiles(path))
			{
				var sd = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(file), string.Empty, SoundType.Effect, Color.white);
				sd.subtitle = false;
				sd.name = Path.GetFileNameWithoutExtension(file);

				ambienceNoises.Add(sd);
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
		internal static List<SoundObject> ambienceNoises = []; // bool indicates whether it is elevator music or not
		readonly static Dictionary<string, int> repeatedMidis = [];

		internal static bool usingEndless = false;

		internal static BoomBox boomBoxPre;

		const string customMusicsPrefix = "customMusics_";
	}

	// Boom Box

	public class BoomBox : MonoBehaviour
	{
		void MidiEvent(MPTKEvent midiEvent)
		{
			if (midiEvent.Command == MPTKCommand.NoteOn || midiEvent.Command == MPTKCommand.NoteOff)
			{
				transform.localScale += midiEvent.Value * incrementConstant * one;
				if (transform.localScale.y > maxLimit)
					transform.localScale = one * maxLimit;
				axisOffset += midiEvent.Value * Random.Range(-1, 2);
				axisOffset = Mathf.Clamp(axisOffset, -axisLimit, axisLimit);
			}

		}

		void OnEnable() =>
			MusicManager.OnMidiEvent += MidiEvent;

		void OnDisable() =>
			MusicManager.OnMidiEvent -= MidiEvent;


		void Update()
		{
			transform.localScale += ((one * minLimit) - transform.localScale) * 3.8f * Time.unscaledDeltaTime;
			if (transform.localScale.y < minLimit)
				transform.localScale = one * minLimit;
			if (transform.localScale.y > maxLimit)
				transform.localScale = one * maxLimit;

			axisOffset += (1f - axisOffset) * 3.4f * Time.unscaledDeltaTime;
			transform.rotation = Quaternion.Euler(0f, 0f, axisOffset);
		}

		float axisOffset = 0f;

		const float maxLimit = 0.65f, minLimit = 0.5f, axisLimit = 15f, incrementConstant = 0.65f;

		Vector3 one = Vector3.one;

	}


	// *************************** Patches **********************************

	[HarmonyPatch]
	internal static class MusicalInjection
	{
		[HarmonyPatch(typeof(ElevatorScreen), "Initialize")]
		static void Prefix(ElevatorScreen __instance)
		{
			var boomBox = Object.Instantiate(CustomMusicPlug.boomBoxPre);
			boomBox.transform.SetParent(__instance.transform.Find("ElevatorTransission"));
			boomBox.transform.SetSiblingIndex(6); // Apparently this affects the render order, huh
			boomBox.transform.localScale = Vector3.one;
			boomBox.transform.localPosition = new(-77.74f, 93.05f);
		}


		internal static List<string> overridingMidis = [];
		internal static List<SoundObject> overridingAmbienceSounds = [];

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

				if (CustomMusicPlug.usingEndless)
					man.ambience.sounds = man.ambience.sounds.AddRangeToArray([.. overridingAmbienceSounds]);

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

					int idx = Random.Range(0, midis.Count + 1);
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
