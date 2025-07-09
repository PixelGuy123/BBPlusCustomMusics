using System.Collections.Generic;
using System.IO;
using System.Linq;
using BBPlusCustomMusics.MonoBehaviours;
using BBPlusCustomMusics.Plugin.Public;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

namespace BBPlusCustomMusics.Plugin
{
	[BepInPlugin(guid, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]

	// [BepInDependency("mtm101.rulerp.baldiplus.endlessfloors", BepInDependency.DependencyFlags.SoftDependency)]
	// [BepInDependency("Rad.cmr.baldiplus.arcaderenovations", BepInDependency.DependencyFlags.SoftDependency)]
	internal class CustomMusicPlug : BaseUnityPlugin
	{
		const string guid = "pixelguy.pixelmodding.baldiplus.custommusics";
		internal static CustomMusicPlug i;

		private void Awake()
		{
			i = this;

			new Harmony(guid).PatchAll();
			string modPath = AssetLoader.GetModPath(this);
			AssetLoader.LoadLocalizationFolder(Path.Combine(modPath, "Language", "English"), Language.English);

			// Load all custom music and sound assets from their respective directories
			MusicRegister.AddMIDIsFromDirectory(MidiDestiny.Schoolhouse, modPath, "schoolMusics");
			MusicRegister.AddMIDIsFromDirectory(MidiDestiny.Elevator, modPath, "elevatorMusics");
			MusicRegister.AddMIDIsFromDirectory(MidiDestiny.FieldTrip_Minigame, modPath, "fieldTripMusic");
			MusicRegister.AddMIDIsFromDirectory(MidiDestiny.FieldTrip_Tutorial, modPath, "fieldTripTutorialMusic");
			MusicRegister.AddMIDIsFromDirectory(MidiDestiny.TimeOut, modPath, "timeOutMusic");
			MusicRegister.AddMIDIsFromDirectory(MidiDestiny.Tutorial, modPath, "tutorialMusic");

			MusicRegister.AddMusicFilesFromDirectory(SoundDestiny.Ambience, modPath, "ambiences");
			MusicRegister.AddMusicFilesFromDirectory(SoundDestiny.Playtime, modPath, "playtimeMusics");
			MusicRegister.AddMusicFilesFromDirectory(SoundDestiny.PartyEvent, modPath, "partyMusics");
			MusicRegister.AddMusicFilesFromDirectory(SoundDestiny.JohnnyStore, modPath, "johnnyMusic");

			MusicRegister.AddSoundFontsFromDirectory(modPath, "sfsFiles");

			// Music Config setup
			CustomOptionsCore.OnMenuInitialize += (menu, handler) => handler.AddCategory<MusicsOptionsCat>("Musics Config");

			// Register save/load action for custom music settings
			ModdedSaveSystem.AddSaveLoadAction(this, SaveLoadAction);

			// Register asset loading event for boom box prefab
			LoadingEvents.RegisterOnAssetsLoaded(Info, OnAssetsLoad, LoadingEventOrder.Start);

			// Register ambience noise injection for level generation
			GeneratorManagement.Register(this, GenerationModType.Finalizer, OnGenerationFinalizer);
		}

		// ===================== "Lambda" Methods =====================

		// Handles saving and loading of custom music settings.
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

		// Handles asset loading for the boom box prefab.
		private void OnAssetsLoad()
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


		// Handles ambience noise injection for level generation.
		private void OnGenerationFinalizer(string levelTitle, int num, SceneObject sco)
		{
			if (sco.manager is MainGameManager man)
			{
				man.ambience.sounds = man.ambience.sounds.AddRangeToArray(
				[..
				MusicRegister.allSounds
				.Where(x => x.soundDestiny == SoundDestiny.Ambience && x.CanBeInsertedOnFloor(levelTitle))
				.Select(x => x.soundObject)
				]);
				return;
			}
			if (sco.manager is EndlessGameManager eman)
			{
				eman.ambience.sounds = eman.ambience.sounds.AddRangeToArray(
				[..
				MusicRegister.allSounds
				.Where(x => x.soundDestiny == SoundDestiny.Ambience && x.CanBeInsertedOnFloor(levelTitle))
				.Select(x => x.soundObject)
				]);
				return;
			}
		}

		internal static BoomBox boomBoxPre;
	}
}