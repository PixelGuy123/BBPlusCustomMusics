using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using MidiPlayerTK;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.UI;

namespace BBPlusCustomMusics
{
	[BepInPlugin("pixelguy.pixelmodding.baldiplus.custommusics", PluginInfo.PLUGIN_NAME, strVersion)]
	[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]

	[BepInDependency("mtm101.rulerp.baldiplus.endlessfloors", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("Rad.cmr.baldiplus.arcaderenovations", BepInDependency.DependencyFlags.SoftDependency)]
	public class CustomMusicPlug : BaseUnityPlugin
	{
		const string strVersion = "1.1.0";
		internal static CustomMusicPlug i;

#pragma warning disable IDE0051 // Remover membros privados não utilizados
		private void Awake()
#pragma warning restore IDE0051 // Remover membros privados não utilizados
		{
			i = this;

			new Harmony("pixelguy.pixelmodding.baldiplus.custommusics").PatchAll();
			string p = AssetLoader.GetModPath(this);
			AssetLoader.LoadLocalizationFolder(Path.Combine(p, "Language", "English"), Language.English);

			AddAmbiencesFromDirectory(p, "ambiences");
			AddMidisFromDirectory(false, p, "schoolMusics");
			AddMidisFromDirectory(true, p, "elevatorMusics");
			AddSoundFontsFromDirectory(p, "sfsFiles");
			AddPlaytimeMusicsFromDirectory(p, "playtimeMusics");
			AddPartyEventMusicsFromDirectory(p, "partyMusics");
			AddJhonnyMusicsFromDirectory(p, "jhonnyMusic");
			AddFieldTripMusicsFromDirectory(true, p, "fieldTripTutorialMusic");
			AddFieldTripMusicsFromDirectory(false, p, "fieldTripMusic");


			// Music Config setup
			CustomOptionsCore.OnMenuInitialize += (optInstance, handler) => handler.AddCategory<MusicsOptionsCat>("Musics Config");

			ModdedSaveSystem.AddSaveLoadAction(this, (isSave, path) => // Save system
			{
				path = Path.Combine(path, "customMusicSettings.cfg");

				if (isSave)
				{
					using BinaryWriter writer = new(File.OpenWrite(path));

					for (int i = 0; i < MusicsOptionsCat.values.Length; i++)
						writer.Write(MusicsOptionsCat.values[i]);

					return;
				}

				if (File.Exists(path))
				{
					using BinaryReader reader = new(File.OpenRead(path));
					try
					{
						for (int i = 0; i < MusicsOptionsCat.values.Length; i++)
							MusicsOptionsCat.values[i] = reader.ReadBoolean();
					}
					catch
					{
						Logger.LogWarning("Failed to load the save - can happen due to corruption or for an old save. Using default values!");
						for (int i = 0; i < MusicsOptionsCat.values.Length; i++)
							MusicsOptionsCat.values[i] = false;
					}
				}
			});

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

		public static void AddFieldTripMusicsFromDirectory(bool isTutorialMusic, params string[] paths)
		{
			string path = Path.Combine(paths);
			if (!Directory.Exists(path))
				throw new System.ArgumentException($"The directory to load fieldtrip midis from path ({path}) doesn\'t exist!");

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

					fieldTripMidis.Add(new(AssetLoader.MidiFromFile(file, name + $"_{++val}"), isTutorialMusic));

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
				sd.name = "CustomMusics_" + Path.GetFileNameWithoutExtension(file);

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

		public static void AddPlaytimeMusicsFromDirectory(params string[] paths)
		{
			string path = Path.Combine(paths);
			if (!Directory.Exists(path))
				throw new System.ArgumentException($"The directory to load playtime musics from path ({path}) doesn\'t exist!");

			foreach (var file in Directory.EnumerateFiles(path))
			{
				var sd = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(file), "Mfx_mus_Playtime", SoundType.Music, Color.red);
				sd.name = customMusicsPrefix + Path.GetFileNameWithoutExtension(file);

				playtimeMusics.Add(sd);
			}
		}

		public static void AddPartyEventMusicsFromDirectory(params string[] paths)
		{
			string path = Path.Combine(paths);
			if (!Directory.Exists(path))
				throw new System.ArgumentException($"The directory to load Party event musics from path ({path}) doesn\'t exist!");

			foreach (var file in Directory.EnumerateFiles(path))
			{
				var sd = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(file), "Mfx_Party", SoundType.Music, Color.white);
				sd.name = customMusicsPrefix + Path.GetFileNameWithoutExtension(file);

				partyEventMusics.Add(sd);
			}
		}

		public static void AddJhonnyMusicsFromDirectory(params string[] paths)
		{
			string path = Path.Combine(paths);
			if (!Directory.Exists(path))
				throw new System.ArgumentException($"The directory to load Jhonny's musics from path ({path}) doesn\'t exist!");

			foreach (var file in Directory.EnumerateFiles(path))
			{
				var sd = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(file), string.Empty, SoundType.Music, Color.white);
				sd.subtitle = false;
				sd.name = customMusicsPrefix + Path.GetFileNameWithoutExtension(file);

				jhonnyMusic.Add(sd);
			}
		}

		readonly internal static List<KeyValuePair<string, bool>> midis = [], fieldTripMidis = []; // bool indicates whether it is elevator music or not
		readonly internal static List<SoundObject> ambienceNoises = [];
		readonly static Dictionary<string, int> repeatedMidis = [];
		internal readonly static List<SoundObject> playtimeMusics = [], partyEventMusics = [], jhonnyMusic = [];

		internal static bool usingEndless = false;

		internal static BoomBox boomBoxPre;

		const string customMusicsPrefix = "customMusics_";
	}

	// 
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
		[HarmonyPrefix]
		[HarmonyPatch(typeof(ElevatorScreen), "Initialize")]
		static void ElevatorBoomBoxThing(ElevatorScreen __instance)
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

		[HarmonyPatch(typeof(ElevatorScreen), "ZoomIntro", MethodType.Enumerator)]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> ElevatorCustomMusic(IEnumerable<CodeInstruction> instructions) =>
				new CodeMatcher(instructions)
				.MatchForward(false, new CodeMatch(OpCodes.Ldstr, "Elevator", "Elevator"))
				.SetInstruction(Transpilers.EmitDelegate(() =>
				{

					List<string> midis = new(CustomMusicPlug.midis.Where(x => x.Value).Select(x => x.Key)); // Gets the elevator musics

					if (midis.Count == 0)
						return "Elevator";

					int idx = Random.Range(0, midis.Count + (MusicsOptionsCat.values[0] ? 0 : 1));
					if (idx >= midis.Count)
						return "Elevator";
					return midis[idx];
				}))
				.InstructionEnumeration();

		[HarmonyPatch(typeof(ElevatorScreen), "ZoomIntro", MethodType.Enumerator)]
		[HarmonyPostfix]
		private static void StopMusicAfter() =>
			Singleton<MusicManager>.Instance.StopFile(); // Forgot about this

		[HarmonyPatch(typeof(Playtime), "Initialize")]
		[HarmonyPostfix]
		static void PlaytimeDingOverride(Playtime __instance)
		{
			if (CustomMusicPlug.playtimeMusics.Count == 0)
				return;

			int idx = Random.Range(0, CustomMusicPlug.playtimeMusics.Count + (MusicsOptionsCat.values[0] ? 0 : 1));
			if (idx >= CustomMusicPlug.playtimeMusics.Count)
				return;

			AudioManager audMan = null;
			foreach (var aud in __instance.GetComponents<AudioManager>())
			{
				if (aud.soundOnStart != null && aud.soundOnStart.Length == 1)
				{
					audMan = aud;
					break;
				}
			}

			if (audMan == null)
				return;

			audMan.soundOnStart[0] = CustomMusicPlug.playtimeMusics[idx];
		}

		[HarmonyPatch(typeof(PartyEvent), "Begin")]
		[HarmonyPrefix]
		static void CustomPartyMusic(ref SoundObject ___musParty)
		{
			if (CustomMusicPlug.partyEventMusics.Count == 0)
				return;

			int idx = Random.Range(0, CustomMusicPlug.partyEventMusics.Count + (MusicsOptionsCat.values[0] ? 0 : 1));
			if (idx >= CustomMusicPlug.partyEventMusics.Count)
				return;

			___musParty = CustomMusicPlug.partyEventMusics[idx];
		}

		[HarmonyPatch(typeof(StoreRoomFunction), "SetOffAlarm")]
		[HarmonyPatch(typeof(StoreRoomFunction), "Close")]
		[HarmonyPrefix]
		static void AlarmOffPatch(AudioManager ___alarmAudioManager) =>
			___alarmAudioManager.FlushQueue(true);

		[HarmonyPatch(typeof(StoreRoomFunction), "Open")]
		[HarmonyPrefix]
		static void JhonnyMusic(AudioManager ___alarmAudioManager)
		{
			if (CustomMusicPlug.jhonnyMusic.Count == 0)
				return;

			int idx = Random.Range(0, CustomMusicPlug.jhonnyMusic.Count);

			___alarmAudioManager.FlushQueue(true);
			___alarmAudioManager.QueueAudio(CustomMusicPlug.jhonnyMusic[idx]);
			___alarmAudioManager.SetLoop(true);
		}



		[HarmonyPatch(typeof(MinigameTutorial), "Initialize")]
		[HarmonyPrefix]
		static void TutorialMinigame(MinigameTutorial __instance)
		{
			List<string> midis = [];
			for (int i = 0; i < CustomMusicPlug.fieldTripMidis.Count; i++)
				if (CustomMusicPlug.fieldTripMidis[i].Value)
					midis.Add(CustomMusicPlug.fieldTripMidis[i].Key);

			if (midis.Count == 0)
				return;

			int idx = Random.Range(0, midis.Count + (MusicsOptionsCat.values[0] ? 0 : 1));
			if (idx >= midis.Count)
				return;

			__instance.music = midis[idx];
		}

		[HarmonyPatch(typeof(MinigameBase), "StartMinigame")]
		[HarmonyPrefix]
		static void Minigame(ref string ___midiSong)
		{
			List<string> midis = [];
			for (int i = 0; i < CustomMusicPlug.fieldTripMidis.Count; i++)
				if (!CustomMusicPlug.fieldTripMidis[i].Value)
					midis.Add(CustomMusicPlug.fieldTripMidis[i].Key);

			if (midis.Count == 0)
				return;

			int idx = Random.Range(0, midis.Count + (MusicsOptionsCat.values[0] ? 0 : 1));
			if (idx >= midis.Count)
				return;

			___midiSong = midis[idx];
		}
	}
}
