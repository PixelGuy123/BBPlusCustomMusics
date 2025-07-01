using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using MidiPlayerTK;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

namespace BBPlusCustomMusics
{
	[BepInPlugin(guid, PluginInfo.PLUGIN_NAME, "1.1.1")]
	[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]

	[BepInDependency("mtm101.rulerp.baldiplus.endlessfloors", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("Rad.cmr.baldiplus.arcaderenovations", BepInDependency.DependencyFlags.SoftDependency)]
	public class CustomMusicPlug : BaseUnityPlugin
	{
		const string guid = "pixelguy.pixelmodding.baldiplus.custommusics";
		internal static CustomMusicPlug i;

		private void Awake()
		{
			i = this;

			new Harmony(guid).PatchAll();
			string p = AssetLoader.GetModPath(this);
			AssetLoader.LoadLocalizationFolder(Path.Combine(p, "Language", "English"), Language.English);

			// Load all custom music and sound assets from their respective directories
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
			CustomOptionsCore.OnMenuInitialize += (menu, handler) => handler.AddCategory<MusicsOptionsCat>("Musics Config");

			// Register save/load action for custom music settings
			ModdedSaveSystem.AddSaveLoadAction(this, SaveLoadAction);

			// Register asset loading event for boom box prefab
			LoadingEvents.RegisterOnAssetsLoaded(Info, OnAssetsLoaded, false);

			// Register ambience noise injection for level generation
			GeneratorManagement.Register(this, GenerationModType.Finalizer, OnGenerationFinalizer);
		}

		// ===================== Lambda Extraction Methods =====================

		/// <summary>
		/// Handles saving and loading of custom music settings.
		/// </summary>
		private void SaveLoadAction(bool isSave, string path)
		{
			path = Path.Combine(path, "customMusicSettings.cfg");

			if (isSave)
			{
				// Gets a BinaryWriter
				using BinaryWriter writer = new(File.OpenWrite(path));
				// Write all values into it (it's always the same fixed size btw)
				for (int i = 0; i < MusicsOptionsCat.values.Length; i++)
					writer.Write(MusicsOptionsCat.values[i]);
				return;
			}

			if (File.Exists(path))
			{
				// Get a BinaryReader
				using BinaryReader reader = new(File.OpenRead(path));
				try
				{
					// Read the values back with same fixed size length
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
		}

		/// <summary>
		/// Handles asset loading for the boom box prefab.
		/// </summary>
		private void OnAssetsLoaded()
		{
			// ******* Load BoomBox *******
			string p = AssetLoader.GetModPath(this);
			var spr = AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(Path.Combine(p, "boomBox.png")), 146);
			var boomBox = new GameObject("ElevatorBoomBox").AddComponent<Image>();
			boomBox.gameObject.layer = LayerMask.NameToLayer("UI");
			boomBox.material = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == "UI_AsSprite");
			boomBox.sprite = spr;
			boomBox.gameObject.ConvertToPrefab(true);
			boomBoxPre = boomBox.gameObject.AddComponent<BoomBox>();
		}

		/// <summary>
		/// Handles ambience noise injection for level generation.
		/// </summary>
		private void OnGenerationFinalizer(string levelTitle, int num, SceneObject sco)
		{
			if (sco.manager is MainGameManager man)
			{
				var ambienceNoises = new List<SoundObject>(CustomMusicPlug.ambienceNoises);
				for (int i = 0; i < ambienceNoises.Count; i++)
				{
					string[] midiData = ambienceNoises[i].name.Split('_');
					if (midiData.Length == 1) continue; // All floors
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
					if (midiData.Length == 1) continue; // All floors
					if (!midiData.Contains(levelTitle))
					{
						ambienceNoises.RemoveAt(i--);
						continue;
					}
				}
				eman.ambience.sounds = eman.ambience.sounds.AddRangeToArray([.. ambienceNoises]);
				return;
			}
		}

		// ===================== Public Static Methods =====================

		/// <summary>
		/// Loads all MIDI files from the specified directory and adds them to the custom music list.
		/// </summary>
		public static void AddMidisFromDirectory(bool isElevatorMusic, params string[] paths)
		{
			string path = Path.Combine(paths);
			if (!Directory.Exists(path))
				throw new System.ArgumentException($"The directory to load midis from path ({path}) doesn't exist!");

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
					allMidis.Add(new(AssetLoader.MidiFromFile(file, name + $"_{++val}"), new SchoolhouseSettings()));
					repeatedMidis[name]++;
				}
			}
		}

		// -----------------------------------------------------------------

		/// <summary>
		/// Loads all field trip MIDI files from the specified directory and adds them to the field trip music list.
		/// </summary>
		public static void AddFieldTripMusicsFromDirectory(bool isTutorialMusic, params string[] paths)
		{
			string path = Path.Combine(paths);
			if (!Directory.Exists(path))
				throw new System.ArgumentException($"The directory to load fieldtrip midis from path ({path}) doesn't exist!");

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

		// -----------------------------------------------------------------

		/// <summary>
		/// Loads all ambience sound files from the specified directory and adds them to the ambience list.
		/// </summary>
		public static void AddAmbiencesFromDirectory(params string[] paths)
		{
			string path = Path.Combine(paths);
			if (!Directory.Exists(path))
				throw new System.ArgumentException($"The directory to load ambience sounds from path ({path}) doesn't exist!");

			foreach (var file in Directory.EnumerateFiles(path))
			{
				var sd = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(file), string.Empty, SoundType.Effect, Color.white);
				sd.subtitle = false;
				sd.name = "CustomMusics_" + Path.GetFileNameWithoutExtension(file);
				ambienceNoises.Add(sd);
			}
		}

		// -----------------------------------------------------------------

		/// <summary>
		/// Loads all soundfont files from the specified directory and registers them with the MIDI player.
		/// </summary>
		public static void AddSoundFontsFromDirectory(params string[] paths)
		{
			string path = Path.Combine(paths);
			if (!Directory.Exists(path))
				throw new System.ArgumentException($"The directory to load soundfonts from path ({path}) doesn't exist!");

			foreach (var file in Directory.EnumerateFiles(path))
			{
				if (Path.GetExtension(file).StartsWith(".sfs"))
					MidiPlayerGlobal.MPTK_LoadLiveSF("file://" + file);
			}
		}

		// -----------------------------------------------------------------

		/// <summary>
		/// Loads all playtime music files from the specified directory and adds them to the playtime music list.
		/// </summary>
		public static void AddPlaytimeMusicsFromDirectory(params string[] paths)
		{
			string path = Path.Combine(paths);
			if (!Directory.Exists(path))
				throw new System.ArgumentException($"The directory to load playtime musics from path ({path}) doesn't exist!");

			foreach (var file in Directory.EnumerateFiles(path))
			{
				var sd = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(file), "Mfx_mus_Playtime", SoundType.Music, Color.red);
				sd.name = customMusicsPrefix + Path.GetFileNameWithoutExtension(file);
				playtimeMusics.Add(sd);
			}
		}

		// -----------------------------------------------------------------

		/// <summary>
		/// Loads all party event music files from the specified directory and adds them to the party event music list.
		/// </summary>
		public static void AddPartyEventMusicsFromDirectory(params string[] paths)
		{
			string path = Path.Combine(paths);
			if (!Directory.Exists(path))
				throw new System.ArgumentException($"The directory to load Party event musics from path ({path}) doesn't exist!");

			foreach (var file in Directory.EnumerateFiles(path))
			{
				var sd = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(file), "Mfx_Party", SoundType.Music, Color.white);
				sd.name = customMusicsPrefix + Path.GetFileNameWithoutExtension(file);
				partyEventMusics.Add(sd);
			}
		}

		// -----------------------------------------------------------------

		/// <summary>
		/// Loads all Jhonny's music files from the specified directory and adds them to the Jhonny music list.
		/// </summary>
		public static void AddJhonnyMusicsFromDirectory(params string[] paths)
		{
			string path = Path.Combine(paths);
			if (!Directory.Exists(path))
				throw new System.ArgumentException($"The directory to load Jhonny's musics from path ({path}) doesn't exist!");

			foreach (var file in Directory.EnumerateFiles(path))
			{
				var sd = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(file), string.Empty, SoundType.Music, Color.white);
				sd.subtitle = false;
				sd.name = customMusicsPrefix + Path.GetFileNameWithoutExtension(file);
				jhonnyMusic.Add(sd);
			}
		}

		// -----------------------------------------------------------------

		// *********** MIDIs/Sounds stored here ************

		// ******* Private Structs for the system ******
		internal readonly struct GenericMIDIHolder(string midiName, MidiSettings settings)
		{
			public readonly string MidiName = midiName;
			public readonly MidiSettings Settings = settings;
		}
		// Settings for the MIDI
		internal abstract class MidiSettings
		{
			public MidiDestiny Destiny => destiny;
			protected MidiDestiny destiny = MidiDestiny.None;
			public abstract bool CanBeUsed(params object[] args); // Yup! It'll be as dynamic as javascript lol

			// Helper methods for Exception (which helps debugging)
			protected void ThrowIfLengthIsBelowNumber(object[] args, int num)
			{
				if (args.Length < num)
					throw new System.IndexOutOfRangeException($"Args has a length ({args.Length}) below the minimum required {num};");
			}

			protected T CastAsTAndThrowIfInvalid<T>(object arg)
			{
				var argType = arg.GetType();
				var typeOfT = typeof(T);
				if (argType != typeOfT)
					throw new System.InvalidCastException($"Arg of type {argType.Name} cannot be casted as {typeOfT}");
				return (T)arg;
			}

		}

		internal class SchoolhouseSettings : MidiSettings
		{
			// * Setup *
			public SchoolhouseSettings(string[] acceptedFloors, params LevelType[] acceptedLevelTypes)
			{
				destiny = MidiDestiny.Schoolhouse;
				levelTypes = acceptedLevelTypes.Length == 0 ? [.. allTypes] : [.. acceptedLevelTypes];
				this.acceptedFloors = [.. acceptedFloors];
			}
			/// <summary>
			/// Expects first argument as the floor name and second argument as the level type in the floor.
			/// </summary>
			public override bool CanBeUsed(params object[] args)
			{
				ThrowIfLengthIsBelowNumber(args, 2);

				string currentFloor = CastAsTAndThrowIfInvalid<string>(args[0]);
				LevelType levelType = CastAsTAndThrowIfInvalid<LevelType>(args[1]);

				return acceptedFloors.Contains(currentFloor) && levelTypes.Contains(levelType);
			}
			readonly HashSet<string> acceptedFloors;
			readonly HashSet<LevelType> levelTypes;

			internal static LevelType[] allTypes;
		}
		// Enum used
		internal enum MidiDestiny
		{
			None = 0,
			Schoolhouse = 1,
			Elevator = 2,


		}

		// Main List for midis
		readonly internal static List<GenericMIDIHolder> allMidis = [];
		readonly static Dictionary<string, int> repeatedMidis = [];

		// Anything that is not a midi and doesn't need extra settings can just stay in these lists
		internal readonly static List<SoundObject> playtimeMusics = [], partyEventMusics = [], jhonnyMusic = [], ambienceNoises = [];

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


}