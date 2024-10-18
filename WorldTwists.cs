using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ReLogic.Content;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

//using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using Terraria;
using Terraria.Achievements;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Generation;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;
using Terraria.UI;
using Terraria.Utilities;
using Terraria.WorldBuilding;
using Tyfyter.Utils;
using Tyfyter.Utils.ID;
using static Tyfyter.Utils.StructureUtils.StructureTilePlacementType;
using static Tyfyter.Utils.TileUtils;
//using OnDebug;

namespace WorldTwists {
	public class WorldTwists : Mod {
		internal static WorldTwists Instance;
		public override void Load() {
			if(Instance!=null) Logger.Warn("WorldTwists Instance already loaded at Load()");
			Instance = this;
			Main.Achievements.OnAchievementCompleted += OnAchievementCompleted;
			this.AddConfig(typeof(TwistConfig).Name, new TwistConfig());
			this.AddConfig(typeof(RetwistConfig).Name, new RetwistConfig());
		}

		private void OnAchievementCompleted(Achievement achievement) {
			if (Main.netMode == NetmodeID.SinglePlayer) {
				ThreadPool.QueueUserWorkItem(AchievementCallback, 1);
			}
		}
		public static void AchievementCallback(object threadContext) {
			List<GenPass> tasks = [];
			TwistWorld.AddGenTasks(RetwistConfig.Instance.Achievement, tasks);
			foreach (GenPass item in tasks) {
				item.Apply(null, null);
			}
		}

		public override void Unload() {
			if(Instance==null) Logger.Info("WorldTwists Instance already unloaded at Unload()");
			Instance = null;
		}
	}
	[Label("Settings")]
	public class TwistConfig : ModConfig {
		public static TwistConfig Instance;
		public override bool Autoload(ref string name) {
			return false;
		}
		public override ConfigScope Mode => ConfigScope.ServerSide;
		#region randomization
		[Header("Shuffled")]

		[Label("Shuffled Blocks")]
		[DefaultValue(false)]
		public bool Shuffled = true;

		[Label("Shuffled Walls")]
		[DefaultValue(false)]
		public bool ShuffledWalls = true;

		[Label("Shuffled Walls Include Air")]
		[DefaultValue(false)]
		public bool ShuffledAirWalls = true;

		[Label("Randomized Blocks uses world seed")]
		[DefaultValue(true)]
		public bool UseWorldSeed = true;

		[Label("Randomized Blocks seed")]
		[Tooltip("The seed for Block Randomization, ignored if \"Randomized Blocks uses world seed\" is on")]
		[DefaultValue(0)]
		[Range(int.MinValue, int.MaxValue)]
		public int RandomizeSeed = 0;

		[Label("Randomized Blocks uses complex seed")]
		[Tooltip("If \"Randomized Blocks uses world seed\" is on this outputs the complex seed just before shuffling")]
		[DefaultValue(false)]
		public bool UseComplexSeed = false;

		[Label("Complex seed inext")]
		[DefaultValue(0)]
		[Range(int.MinValue, int.MaxValue)]
		public int inext = 0;

		[Label("Complex seed inextp")]
		[DefaultValue(0)]
		[Range(int.MinValue, int.MaxValue)]
		public int inextp = 0;
		[Header("Randomize")]

		[Label("Randomize Blocks")]
		[DefaultValue(false)]
		public bool Randomize = false;
		//internal int[] SeedArray = new int[56];
		/*[DefaultValue(new int[56] {
			0,0,0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,0,0,
			0,0,0,0,0,0})]*/
		#endregion

		[Header("RarityInvert")]

		[Label("Invert Blocks")]
		[DefaultValue(false)]
		public bool Inverted = false;


		[Header("Shear")]

		[Label("Shear")]
		[DefaultValue(0.0f)]
		[Increment(0.25f)]
		[Range(-20f, 20f)]
		public float Shear = 0;


		[Header("Remapper")]
		public List<TypeMapper<TileDefinition>> TileMaps = [];
		public List<TypeMapper<WallDefinition>> WallMaps = [];
		public List<TypeMapper<LiquidDefinition>> LiquidMaps = [];


		[Header("Waver")]

		[Label("Wave Type")]
		[DefaultValue(0)]
		[DrawTicks]
		public TwistExt.WaveType WaveType { get; set; } = 0;

		[Label("Wave Period")]
		[DefaultValue(0f)]
		[Range(0f,100f)]
		public float WavePeriod { get; set; } = 0;

		[Label("Wave Intensity")]
		[DefaultValue(0f)]
		[Range(0f,20f)]
		public float WaveIntensity { get; set; } = 0;


		#region other
		[Header("OtherChanges")]

		#region mini worlds
		[Label("Mini Worlds")]
		[DefaultValue(false)]
		[Tooltip("Note the plural")]
		public bool GreatEnsmallening { get; set; } = false;

		[Label("Mini World Underground Spawn")]
		[Tooltip("-1:never, 0:50/50, 1:always")]
		[DefaultValue(0)]
		[Range(-1, 1)]
		public int HipsterSpawnSkewInt {
			get => HipsterSpawnSkew;
			set { HipsterSpawnSkew = (sbyte)value; }
		}
		internal sbyte HipsterSpawnSkew = 0;
		#endregion

		[Label("Flipped World")]
		[DefaultValue(false)]
		public bool Flipped { get; set; } = false;

		[Label("Hardmode Start")]
		[DefaultValue(false)]
		public bool AlreadyHM { get; set; } = false;

		[Label("Hardmode Start Spawns WoF loot")]
		[DefaultValue(false)]
		public bool HMPyramid { get; set; } = false;

		[Label("Minefield")]
		[DefaultValue(0.0f)]
		[Tooltip("Density of landmines")]
		[Range(0, 1)]
		public float MinefieldDensity {
			get => Minefield;
			set { Minefield = value; }
		}
		internal float Minefield = 0;

		[Label("Paint It,")]
		[Tooltip("Color of paint")]
		[DrawTicks]
		[JsonConverter(typeof(StringEnumConverter))]
		public PaintEnum PaintIt {
			get => Paint;
			set { Paint = value; }
		}
		internal PaintEnum Paint = 0;

		[DrawTicks]
		[JsonConverter(typeof(StringEnumConverter))]
		public CoatingEnum PaintCoating { get; set; } = PaintCoatingID.None;

		[Label("Solidify Trees")]
		[DefaultValue(false)]
		public bool TreeSolidification { get; set; } = false;

		[Label("More WorldGen")]
		[Tooltip("How much more the world should generate")]
		[DefaultValue(0)]
		public int MoreGen { get; set; } = 0;

		[Label("2 Evils")]
		[DefaultValue(false)]
		public bool MoreEvils { get; set; } = false;

		[Label("Bee Hell")]
		[Tooltip("Bee Hell")]
		[DefaultValue(false)]
		public bool BeeHell { get; set; } = false;

		[Label("Skygrids")]
		[JsonConverter(typeof(SkyGridSettingConverter))]
		public SkyGridSetting SkyGrids { get; set; }
		#endregion

		#region secret seeds

		[Header("SecretSeeds")]

		[Label("Pre-Generation Secret Seeds")]
		public SecretSeedConfig preGeneration;
		[Label("Post-Generation Secret Seeds")]
		public SecretSeedConfig postGeneration;
		#endregion

		#region universal
		[Header("Universal")]

		[Label("Janky Door Loot")]
		[DefaultValue(false)]
		public bool JankLoot = false;

		[Label("Include platforms")]
		[DefaultValue(false)]
		public bool RandomizePlatforms = false;

		[Label("Include Unstable Blocks")]
		[DefaultValue(false)]
		public bool RandomizeBoulders = false;

		[Label("Include Falling Blocks")]
		[DefaultValue(true)]
		public bool Sandomize = true;

		[Label("Maintain Biomes")]
		[DefaultValue(false)]
		public bool KeepDungeon = false;

		[Label("Maintain Crafting Stations")]
		[DefaultValue(false)]
		public bool KeepAnvil = false;

		[Header("RandomizeOrShuffle")]
		[Label("Complex seed SeedArray")]
		public List<int> SeedArrayList {
			get => SeedArray;
			set {
				if(value.Count>56) {
					value.RemoveRange(56, value.Count-56);
				} else for(; value.Count<56; value.Add(0));
				SeedArray = value;
			}
		}
		internal List<int> SeedArray = new List<int>(56);
		#endregion
	}
	[Label("Re-twisting Events")]
	public class RetwistConfig : ModConfig {
		public static RetwistConfig Instance;
		public override bool Autoload(ref string name) {
			return false;
		}
		public override ConfigScope Mode => ConfigScope.ServerSide;
		[Label("Boss Kills Only Twist Once")]
		[Tooltip("Determines whether repeatedly killing the same boss will continue twisting the world")]
		public bool TrackKillBoss;

		[Label("Boss Kill")]
		[Tooltip("Does not trigger on Wall of Flesh if \"Boss Kills Only Twist Once\" is enabled")]
		public TwistConfig KillBoss;
		[Label("Wall Of Flesh Kill")]
		[Tooltip("Does not trigger if \"Boss Kills Only Twist Once\" is disabled")]
		public TwistConfig KillWOF;
		[Label("Player Death")]
		public TwistConfig PlayerDeath;
		[Label("Achievement")]
		public TwistConfig Achievement;
	}
	public class SecretSeedConfig : ModConfig {
		public override bool Autoload(ref string name) => false;
		public override ConfigScope Mode => ConfigScope.ServerSide;
		[Label("Drunk World")]
		public OnOffNeutralEnum DrunkWorld;
		[Label("Not The Bees")]
		[Tooltip("Incompatible with some other secret seeds")]
		public OnOffNeutralEnum BeeWorld;
		[Label("For The Worthy")]
		public OnOffNeutralEnum GitGudWorld;
		[Label("Celebrationmk10")]
		public OnOffNeutralEnum AniversaryWorld;
		[Label("The Constant")]
		public OnOffNeutralEnum DSTWorld;
	}
	public class TwistWorld : ModSystem {
		public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight) {
			WorldTwists.Instance.Logger.Info("adding worldgen tasks with config:\n" + new Regex("\"SeedArrayList\":\\s*\\[(\\s*0,)+\\s*0\\s*\\]").Replace(JsonConvert.SerializeObject(TwistConfig.Instance, ConfigManager.serializerSettings), ""));
			AddGenTasks(TwistConfig.Instance, tasks);
			WorldTwists.Instance.Logger.Info("added worldgen tasks:\n" + string.Join(", ", tasks.Select(t => t.Name)));
		}
		public override void ModifyHardmodeTasks(List<GenPass> tasks) {
			if(RetwistConfig.Instance.TrackKillBoss) AddGenTasks(RetwistConfig.Instance.KillWOF, tasks);
		}
		public static void AddGenTasks(TwistConfig twistConfig, List<GenPass> tasks) {
			if (twistConfig is null || tasks is null) {
				return;
			}
			if (twistConfig.MoreEvils) {
				int genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Corruption"));
				GenPass task;
				if (genIndex >= 0) {
					task = tasks[genIndex];
					bool crimson = WorldGen.crimson;
					tasks[genIndex] = new PassLegacy("Corruption", (p, config) => {
						crimson = WorldGen.crimson;
						task.Apply(p, config);
						WorldGen.crimson = !crimson;
						task.Apply(p, config);
						WorldGen.crimson = false;
					});
					genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Mirco Biomes"));
					if (genIndex >= 0) {
						task = tasks[genIndex];
						tasks[genIndex] = new PassLegacy("Mirco Biomes", (p, config) => {
							task.Apply(p, config);
							WorldGen.crimson = crimson;
						});
					}
				}
			}
			if (twistConfig.BeeHell) tasks.Add(new PassLegacy("BeeHell", BeeHell));
			int moreGen = twistConfig.MoreGen;
			if (moreGen > 0) {
				GenPass[] _tasks = tasks.ToArray();
				int indexOffset = 0;
				GenPass duplicate;
				int dupeCount;
				for (int i = 0; i < _tasks.Length; i++) {
					dupeCount = 0;
					for (int i2 = moreGen; i2-- > 0;) {
						duplicate = TwistExt.GetDuplicatePassForMoreGen(_tasks[i], dupeCount++);
						if (duplicate is not null) {
							tasks.Insert(i + (++indexOffset), duplicate);
						}
					}
				}
			}
			if (twistConfig.AlreadyHM) {
				if (!twistConfig.HMPyramid) tasks.Add(new PassLegacy("Starting Hardmode", (p, config) => WorldGen.StartHardmode()));
				else tasks.Add(new PassLegacy("Starting Hardmode and Placing loot", HMLooter));
			}
			if (twistConfig.LiquidMaps.Count > 0) {
				int genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Settle Liquids Again"));
				if (genIndex < 0) genIndex = 0;
				tasks.Insert(genIndex, new PassLegacy("Cycle Liquids", LiquidSwitch(twistConfig)));
			}
			if (twistConfig.TreeSolidification)
				tasks.Add(new PassLegacy("Solidifying Trees", TreeSolidifier));
			if (twistConfig.Flipped) {
				tasks.Add(new PassLegacy("Flipping World", Flipper));
				tasks.Add(tasks[tasks.FindIndex(genpass => genpass.Name.Equals("Settle Liquids Again"))]);
			}
			if (twistConfig.GreatEnsmallening) tasks.Add(new PassLegacy("Mini Worlds", GreatEnsmallener(twistConfig)));
			if (twistConfig.WavePeriod != 0 && twistConfig.WaveIntensity != 0) tasks.Add(new PassLegacy("Waving", Waver(twistConfig)));
			if (twistConfig.SkyGrids != default) tasks.Add(new PassLegacy("SkyGrid", Grid(twistConfig)));
			if (twistConfig.Shear != 0) tasks.Add(new PassLegacy("Shear", Shear(twistConfig.Shear)));
			if (twistConfig.Minefield > 0) tasks.Add(new PassLegacy("Placing Landmines", Minefield(twistConfig)));
			if (twistConfig.TileMaps.Count > 0 || twistConfig.WallMaps.Count > 0) tasks.Add(new PassLegacy("Switcheroo", Switcher(twistConfig)));
			if (twistConfig.Shuffled) tasks.Add(new PassLegacy("Shuffle", ShuffledBlocks(twistConfig)));
			else if (twistConfig.Inverted) tasks.Add(new PassLegacy("Rarity Invert", Invert(twistConfig)));
			else if (twistConfig.Randomize) tasks.Add(new PassLegacy("Randomize", RandomizedBlocks(twistConfig)));
			if (twistConfig.ShuffledWalls) tasks.Add(new PassLegacy("WallShuffle", ShuffledWalls(twistConfig)));
			if (twistConfig.Paint > 0) tasks.Add(new PassLegacy("Painting it,", Painter(twistConfig)));
			if (twistConfig.PaintCoating > 0) tasks.Add(new PassLegacy("Coating it", Coater(twistConfig)));
		}
		public static WorldGenLegacyMethod LiquidSwitch(TwistConfig twistConfig) => (GenerationProgress progress, GameConfiguration configuration) => {
			if (progress is not null) progress.Message = "Wet Switcheroo";
			Tile tile;
			var watch = new Stopwatch();
			watch.Start();
			Dictionary<int, int> liquids = new(4);
			for (int i = 0; i < 4; i++) {
				LiquidDefinition newTile = new(i);
				foreach (TypeMapper<LiquidDefinition> current in twistConfig.LiquidMaps) {
					if (current.input.Contains(newTile) != current.inverted) {
						if (i == current.output.Type) break;
						liquids.Add(i, current.output.Type);
						break;
					}
				}
			}
			for (int y = 0; y < Main.maxTilesY - 1; y++) {
				for (int x = 0; x < Main.maxTilesX; x++) {
					tile = Main.tile[x, y];
					if (liquids.TryGetValue(tile.LiquidType, out int newLiquid)) tile.LiquidType = (ushort)newLiquid;
					//if(Switch(tile))WorldGen.SquareTileFrame(x,y);
				}
			}
			watch.Stop();
			WorldTwists.Instance.Logger.Info("Switched liquids in" + watch.Elapsed);
		};
		public static WorldGenLegacyMethod ShuffledBlocks(TwistConfig twistConfig) => (GenerationProgress progress, GameConfiguration configuration) => {
			WorldTwists.Instance.Logger.Info("Shuffling Blocks");
			if(progress is not null) progress.Message = "Shuffling Blocks";
			//Dictionary<int,int> count
			List<ushort> types = new List<ushort>() { };
			List<ushort> invalidTypes = new List<ushort>() { };
			if(!twistConfig.RandomizeBoulders) {
				invalidTypes.Add(TileID.Boulder);
			}
			if(!twistConfig.Sandomize) {
				for(ushort i = 0; i < TileID.Sets.Falling.Length; i++) {
					if(TileID.Sets.Falling[i]) invalidTypes.Add(i);
				}
			}
			if(!twistConfig.JankLoot) {
				invalidTypes.Add(TileID.ClosedDoor);
			}
			Tile tile;
			for(int y = 0; y < Main.maxTilesY; y++) {
				for(int x = 0; x < Main.maxTilesX; x++) {
					tile = Main.tile[x, y];
					if (tile.TileType == TileID.WorkBenches) {

					}
					if(!types.Contains(tile.TileType))
						types.Add(tile.TileType);
					if(!TwistExt.Solid(tile.TileType) && !invalidTypes.Contains(tile.TileType))
						invalidTypes.Add(tile.TileType);
				}
			}
			for(int i = 0; i < types.Count; i++) {
				if(invalidTypes.Contains(types[i])) {
					types.RemoveAt(i);
					i--;
				}
			}
			List<ushort> shuffledTypes = types.ToList();
			//WorldTwists.Instance.Logger.Info("Randomize: Shuffled "+WorldGen.genRand.);
			bool UseWorldSeed = twistConfig.UseWorldSeed;
			bool UseComplexSeed = twistConfig.UseComplexSeed;
			WorldTwists.Instance.Logger.Info($"Shuffle: Using settings UseWorldSeed: {UseWorldSeed}; UseComplexSeed: {UseComplexSeed}");
			if(UseWorldSeed) {
				if(UseComplexSeed) {
					WorldTwists.Instance.Logger.Info("Shuffle: inext "+typeof(UnifiedRandom).GetField("inext", BindingFlags.NonPublic|BindingFlags.Instance).GetValue(WorldGen.genRand));
					WorldTwists.Instance.Logger.Info("Shuffle: inextp "+typeof(UnifiedRandom).GetField("inextp", BindingFlags.NonPublic|BindingFlags.Instance).GetValue(WorldGen.genRand));
					WorldTwists.Instance.Logger.Info("Shuffle: SeedArray: "+string.Join(",", (int[])typeof(UnifiedRandom).GetField("SeedArray", BindingFlags.NonPublic|BindingFlags.Instance).GetValue(WorldGen.genRand)));
				}
				shuffledTypes.Shuffle(WorldGen.genRand);
			} else {
				UnifiedRandom rand = new UnifiedRandom(twistConfig.RandomizeSeed);
				if(UseComplexSeed) {
					typeof(UnifiedRandom).GetField("inext", BindingFlags.NonPublic|BindingFlags.Instance).SetValue(rand, twistConfig.inext);
					typeof(UnifiedRandom).GetField("inextp", BindingFlags.NonPublic|BindingFlags.Instance).SetValue(rand, twistConfig.inextp);
					typeof(UnifiedRandom).GetField("SeedArray", BindingFlags.NonPublic|BindingFlags.Instance).SetValue(rand, twistConfig.SeedArray.ToArray());
				}
				shuffledTypes.Shuffle(rand);
			}
			string log = "";

			Dictionary<ushort, ushort> pairings = [];
			for(int i = 0; i < types.Count; i++) {
				pairings.Add(types[i], shuffledTypes[i]);
				log+=$"[ {types[i]} ( {TwistExt.GetTileName(types[i])}), {shuffledTypes[i]} ( {TwistExt.GetTileName(shuffledTypes[i])})] ";
			}
			//TileID.Sets.Falling
			for(int i = 0; i < invalidTypes.Count; i++) {
				pairings.Add(invalidTypes[i], invalidTypes[i]);
			}
			ModContent.GetInstance<TwistWorld>().AddPairings(pairings);

			WorldTwists.Instance.Logger.Info("Shuffle: Shuffled "+log);
			for(int y = 0; y < Main.maxTilesY; y++) {
				for(int x = 0; x < Main.maxTilesX; x++) {
					try {
						if(Main.tile[x, y].HasTile&&TwistExt.Solid(Main.tile[x, y].TileType))
							Main.tile[x, y].TileType = pairings[Main.tile[x, y].TileType];
						if (Main.tileCut[Main.tile[x, y].TileType] && Main.tile[x, y].TileType != TileID.Pots && Main.tile[x, y].TileType != TileID.JunglePlants && Main.tile[x, y].TileType != TileID.Larva) {
							var currentTile = Main.tile[x, y];
							currentTile.HasTile = false;
						}
					} catch(Exception) {
						WorldTwists.Instance.Logger.Info("Shuffle: Ran into issue randomizing "+Main.tile[x, y].TileType);
						throw;
					}
				}
			}
		};
		public static WorldGenLegacyMethod RandomizedBlocks(TwistConfig twistConfig) => (GenerationProgress progress, GameConfiguration configuration) => {
			WorldTwists.Instance.Logger.Info("Randomizing Blocks");
			if (progress is not null) progress.Message = "Randomizing Blocks";
			//Dictionary<int,int> count
			List<ushort> types = new List<ushort>() { };
			List<ushort> invalidTypes = new List<ushort>() { };
			if (!twistConfig.RandomizeBoulders) {
				invalidTypes.Add(TileID.Boulder);
			}
			if (!twistConfig.Sandomize) {
				for (ushort i = 0; i < TileID.Sets.Falling.Length; i++) {
					if (TileID.Sets.Falling[i]) invalidTypes.Add(i);
				}
			}
			if (!twistConfig.JankLoot) {
				invalidTypes.Add(TileID.ClosedDoor);
			}
			Tile tile;
			for (int y = 0; y < Main.maxTilesY; y++) {
				for (int x = 0; x < Main.maxTilesX; x++) {
					tile = Main.tile[x, y];
					if (!types.Contains(tile.TileType)) types.Add(tile.TileType);
					if (!TwistExt.Solid(tile.TileType) && !invalidTypes.Contains(tile.TileType)) invalidTypes.Add(tile.TileType);
				}
			}
			for (int i = 0; i < types.Count; i++) {
				if (invalidTypes.Contains(types[i])) {
					types.RemoveAt(i);
					i--;
				}
			}
			//WorldTwists.Instance.Logger.Info("Randomize: Shuffled "+WorldGen.genRand.);
			bool UseWorldSeed = twistConfig.UseWorldSeed;
			bool UseComplexSeed = twistConfig.UseComplexSeed;
			WorldTwists.Instance.Logger.Info($"Randomize: Using settings UseWorldSeed: {UseWorldSeed}; UseComplexSeed: {UseComplexSeed}");
			UnifiedRandom rand;
			if (UseWorldSeed) {
				rand = WorldGen.genRand;
				if (UseComplexSeed) {
					WorldTwists.Instance.Logger.Info("Randomize: inext " + typeof(UnifiedRandom).GetField("inext", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(WorldGen.genRand));
					WorldTwists.Instance.Logger.Info("Randomize: inextp " + typeof(UnifiedRandom).GetField("inextp", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(WorldGen.genRand));
					WorldTwists.Instance.Logger.Info("Randomize: SeedArray: " + string.Join(",", (int[])typeof(UnifiedRandom).GetField("SeedArray", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(WorldGen.genRand)));
				}
			} else {
				rand = new UnifiedRandom(twistConfig.RandomizeSeed);
				if (UseComplexSeed) {
					typeof(UnifiedRandom).GetField("inext", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rand, twistConfig.inext);
					typeof(UnifiedRandom).GetField("inextp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rand, twistConfig.inextp);
					typeof(UnifiedRandom).GetField("SeedArray", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rand, twistConfig.SeedArray.ToArray());
				}
			}
			for (int y = 0; y < Main.maxTilesY; y++) {
				for (int x = 0; x < Main.maxTilesX; x++) {
					try {
						tile = Main.tile[x, y];
						if (Main.tile[x, y].HasTile && types.Contains(tile.TileType))
							tile.TileType = rand.Next(types);
						if (Main.tileCut[Main.tile[x, y].TileType] && Main.tile[x, y].TileType != TileID.Pots && Main.tile[x, y].TileType != TileID.JunglePlants && Main.tile[x, y].TileType != TileID.Larva) {
							var currentTile = Main.tile[x, y];
							currentTile.HasTile = false;
						}
					} catch (Exception) {
						WorldTwists.Instance.Logger.Info("Randomize: Ran into issue randomizing " + Main.tile[x, y].TileType);
						throw;
					}
				}
			}
		};
		public static WorldGenLegacyMethod Invert(TwistConfig twistConfig) => (GenerationProgress progress, GameConfiguration configuration) => {
			WorldTwists.Instance.Logger.Info("Inverting Blocks");
			if (progress is not null) progress.Message = "Inverting Blocks";
			//Dictionary<int,int> count
			Dictionary<ushort, int> types = new Dictionary<ushort, int>() { };
			List<ushort> invalidTypes = new List<ushort>() { };
			if (!twistConfig.RandomizeBoulders) {
				invalidTypes.Add(TileID.Boulder);
				for (ushort i = 0; i < TileLoader.TileCount; i++) {
					if (Main.tileFrameImportant[i]) invalidTypes.Add(i);
				}
			}
			if (!twistConfig.JankLoot) {
				invalidTypes.Add(TileID.ClosedDoor);
			}
			Tile tile;
			for (int y = 0; y < Main.maxTilesY; y++) {
				for (int x = 0; x < Main.maxTilesX; x++) {
					tile = Main.tile[x, y];
					if (!TwistExt.Solid(tile.TileType)) {
						if (!invalidTypes.Contains(tile.TileType)) invalidTypes.Add(tile.TileType);
						continue;
					}
					if (invalidTypes.Contains(tile.TileType)) continue;
					if (types.ContainsKey(tile.TileType)) {
						types[tile.TileType]++;
					} else {
						types.Add(tile.TileType, 1);
					}
				}
			}
			for (int i = 0; i < invalidTypes.Count; i++) {
				if (types.ContainsKey(invalidTypes[i])) {
					types.Remove(invalidTypes[i]);
					i--;
				}
			}
			List<KeyValuePair<ushort, int>> listTypes = types.ToList();
			listTypes.Sort((p1, p2) => p1.Value - p2.Value);
			string log = "";

			Dictionary<ushort, ushort> pairings = [];
			for (int i = 0; i < types.Count; i++) {
				pairings.Add(listTypes[i].Key, listTypes[types.Count - i - 1].Key);
				log += $"( {listTypes[i]}, {listTypes[types.Count - i - 1]}) ";
			}
			for (int i = 0; i < invalidTypes.Count; i++) {
				if (!pairings.ContainsKey(invalidTypes[i])) pairings.Add(invalidTypes[i], invalidTypes[i]);
			}
			ModContent.GetInstance<TwistWorld>().AddPairings(pairings);

			WorldTwists.Instance.Logger.Info("Invert: Inverted " + log);
			for (int y = 0; y < Main.maxTilesY; y++) {
				for (int x = 0; x < Main.maxTilesX; x++) {
					try {
						if (Main.tile[x, y].HasTile && TwistExt.Solid(Main.tile[x, y].TileType))
							Main.tile[x, y].TileType = pairings[Main.tile[x, y].TileType];
						if (Main.tileCut[Main.tile[x, y].TileType]) {
							var currentTile = Main.tile[x, y];
							currentTile.HasTile = false;
						}
					} catch (Exception) {
						WorldTwists.Instance.Logger.Info("Randomize: Ran into issue randomizing " + Main.tile[x, y].TileType);
						throw;
					}
				}
			}
		};
		public static WorldGenLegacyMethod GreatEnsmallener(TwistConfig twistConfig) => (GenerationProgress progress, GameConfiguration configuration) => {
			if (progress is not null) progress.Message = "Ensmallening";
			int width = Main.maxTilesX;
			int height = Main.maxTilesY;
			int halfX = width / 2;
			int halfY = height / 2;
			int x, y;
			TileData[,] tiles = new TileData[Main.tile.Width, Main.tile.Height];
			for (int y1 = 0; y1 < height; y1++) {
				for (int x1 = 0; x1 < width; x1++) {
					x = x1 / 2 + ((x1 % 2) * halfX);
					y = y1 / 2 + ((y1 % 2) * halfY);
					tiles[x, y] = Main.tile[x1, y1];
				}
			}
			for (int y2 = 0; y2 < height; y2++) {
				for (int x2 = 0; x2 < width; x2++) {
					Main.tile[x2, y2].SetTileData(tiles[x2, y2]);
				}
			}
			Chest c;
			Tile chestTile;
			//Tile[,] chestTiles = new Tile[3,3];
			for (int i = 0; i < Main.chest.Length; i++) {
				c = Main.chest[i];
				if (c is null) {
					continue;
				}
				c.x = c.x / 2 + ((c.x % 2) * halfX);
				c.y = c.y / 2 + ((c.y % 2) * halfY);
				chestTile = Main.tile[c.x, c.y];
				c.y--;
				/*chestTiles = new Tile[3, 3];
				for(int cy = -1; cy < 2; cy++) {
					for(int cx = -1; cx < 2; cx++) {
						chestTiles[cx + 1, cy + 1] = Main.tile[c.x+cx, c.y+cy];
					}
				}*/
				try {
					MultiTileUtils.AggressivelyPlace(new Point(c.x, c.y), chestTile.TileType, chestTile.TileFrameX / MultiTileUtils.GetStyleWidth(chestTile.TileType));
				} catch (Exception e) {
					WorldTwists.Instance.Logger.Warn(e);
					Exception _ = e;
				}
			}
			Main.spawnTileX = (Main.spawnTileX / 2) + WorldGen.genRand.Next(2) * halfX;
			Main.spawnTileY = (Main.spawnTileY / 2) + (WorldGen.genRand.Next(2) + twistConfig.HipsterSpawnSkew > 0 ? halfY : 0);
			Main.dungeonX = (Main.dungeonX / 2) + WorldGen.genRand.Next(2) * halfX;
			Main.dungeonY = (Main.dungeonY / 2);
			int npci;
			for (npci = 0; npci < Main.npc.Length; npci++) {
				if (Main.npc[npci].type == NPCID.OldMan) {
					break;
				}
			}
			if (npci < 201) {
				Main.npc[npci].position = new Vector2(Main.dungeonX, Main.dungeonY) * 16;
			}
			for (npci = 0; npci < Main.npc.Length; npci++) {
				if (Main.npc[npci].type == NPCID.Guide) {
					break;
				}
			}
			if (npci < 201) {
				Main.npc[npci].position.X = Main.npc[npci].position.X + (WorldGen.genRand.Next(2) * halfX * 16);
				Main.npc[npci].position.Y = Main.spawnTileY;
			}
			smol = true;
		};
		public static void Flipper(GenerationProgress progress, GameConfiguration configuration) {
			if (progress is not null) progress.Message = "Flipping";
			int width = Main.maxTilesX;
			int height = Main.maxTilesY;
			int halfX = width/2;
			int halfY = height/2;
			int y;
			int debX = -1;
			TileData[,] tiles = new TileData[Main.tile.Width,Main.tile.Height];
			for(int y1 = 0; y1 < height; y1++) {
				for(int x = 0; x < width; x++) {
					y = Main.maxTilesY - y1;
					tiles[x, y - 1] = Main.tile[x, y1];
					switch (tiles[x, y - 1].TileWallWireStateData.Slope) {
						case SlopeType.SlopeDownLeft:
						tiles[x, y - 1].TileWallWireStateData.Slope = SlopeType.SlopeUpLeft;
						break;
						case SlopeType.SlopeUpLeft:
						tiles[x, y - 1].TileWallWireStateData.Slope = SlopeType.SlopeDownLeft;
						break;
						case SlopeType.SlopeDownRight:
						tiles[x, y - 1].TileWallWireStateData.Slope = SlopeType.SlopeUpRight;
						break;
						case SlopeType.SlopeUpRight:
						tiles[x, y - 1].TileWallWireStateData.Slope = SlopeType.SlopeDownRight;
						break;
					}
					if(x>debX)debX = x;
				}
				y = y1;
			}
			debX = -1;
			for (int y2 = 0; y2 < height; y2++) {
				for(int x2 = 0; x2 < width; x2++) {
					Main.tile[x2, y2].SetTileData(tiles[x2, y2]);
					if (x2>debX)debX = x2;
				}
				y = y2;
			}
			Chest c;
			Tile chestTile;
			//Tile[,] chestTiles = new Tile[3,3];
			for(int i = 0; i < Main.chest.Length; i++) {
				c = Main.chest[i];
				if(c is null) {
					continue;
				}
				c.y = Main.maxTilesY - c.y - 1;
				chestTile = Main.tile[c.x, c.y];
				/*chestTiles = new Tile[3, 3];
				for(int cy = -1; cy < 2; cy++) {
					for(int cx = -1; cx < 2; cx++) {
						chestTiles[cx + 1, cy + 1] = Main.tile[c.x+cx, c.y+cy];
					}
				}*/
				try {
					int styleWidth = MultiTileUtils.GetStyleWidth(chestTile.TileType);
					int style = styleWidth != 0 ? chestTile.TileFrameX / styleWidth : 0;
					if (TileObjectData.GetTileData(chestTile.TileType, style) is not null) MultiTileUtils.AggressivelyPlace(new Point(c.x, c.y), chestTile.TileType, style);
				} catch(Exception e) {
					WorldTwists.Instance.Logger.Warn(e);
					Exception _ = e;
				}
			}
			int found = 0;
			for(y = 0; y < height; y++) {
				Main.spawnTileY = y - 1;
				if(Main.tileSolid[Main.tile[Main.spawnTileX, y].TileType]) {
					if(found>2)break;
					found = 0;
				} else {
					found++;
				}
			}
			//Main.spawnTileX = (Main.spawnTileX / 2) + WorldGen.genRand.Next(2) * halfX;
			//Main.spawnTileY = Main.maxTilesY - Main.spawnTileY;//(Main.spawnTileY / 2) + (WorldGen.genRand.Next(2) + twistConfig.HipsterSpawnSkew>0 ? halfY : 0);
			//Main.dungeonX = (Main.dungeonX / 2) + WorldGen.genRand.Next(2) * halfX;
			//Main.dungeonY = (Main.dungeonY / 2);
			int npci;
			for(npci = 0;npci<Main.npc.Length;npci++) {
				if(Main.npc[npci].type==NPCID.OldMan) {
					break;
				}
			}
			if(npci<201) {
				Main.npc[npci].position = new Vector2(Main.dungeonX,Main.dungeonY)*16;
			}
			for(npci = 0;npci<Main.npc.Length;npci++) {
				if(Main.npc[npci].type==NPCID.Guide) {
					break;
				}
			}
			if(npci<201) {
				Main.npc[npci].position.Y = Main.spawnTileY*16;
			}
		}
		public static void HMLooter(GenerationProgress progress, GameConfiguration configuration) {
			if (progress is not null) progress.Message = "Starting Hardmode";
			Item[] items = Main.item;
			Main.item = new Item[Main.maxItems+1].Select((v)=>new Item()).ToArray();

			NPC npc = Main.npc[NPC.NewNPC(Entity.GetSource_None(), 16*16,16*16,NPCID.WallofFlesh)];
			npc.NPCLoot();
			npc.active = false;

			Item[] loot = Main.item.Where((i)=>!i.IsAir).ToArray();
			Main.item = items;

			int centerI = (int)(npc.position.X + (npc.width / 2)) / 16;
			int centerJ = (int)(npc.position.Y + (npc.height / 2)) / 16;
			int size = npc.width / 2 / 16 + 1;
			for (int i = centerI - size; i <= centerI + size; i++) {
				for (int j = centerJ - size; j <= centerJ + size; j++) {
					if ((i == centerI - size || i == centerI + size || j == centerJ - size || j == centerJ + size) && (Main.tile[i, j].TileType == 347 || Main.tile[i, j].TileType == 140)) {
						var currentTile = Main.tile[i, j];
						currentTile.HasTile = false;
					}
					if (Main.netMode == NetmodeID.Server) {
						NetMessage.SendTileSquare(-1, i, j, 1);
					}
				}
			}
			int x = Main.maxTilesX / 2;
			int y = Main.maxTilesY - 150;
			ushort brickType = WorldGen.crimson ? TileID.CrimtaneBrick : TileID.DemoniteBrick;
			StructureUtils.Structure pyramid = new StructureUtils.Structure(
			new string[] {
			"   _____l_c__l_____   ",
			"  ____qbbbbbbbbp____  ",
			" ___qbbbbbbbbbbbbp___ ",
			"__qbbbbbbbbbbbbbbbbp__",
			"qbbbbbbbbbbbbbbbbbbbbp"
			},
			new Dictionary<char, StructureUtils.StructureTile>() {
				{'b',new StructureUtils.StructureTile(brickType, ReplaceOld)},
				{'q',new StructureUtils.StructureTile(brickType, ReplaceOld, BlockType.SlopeDownRight)},
				{'p',new StructureUtils.StructureTile(brickType, ReplaceOld, BlockType.SlopeDownLeft)},
				{'c',new StructureUtils.StructureTile(TileID.Containers, RequiredTile|MultiTile, 0, 44)},
				{'l',new StructureUtils.StructureTile(TileID.Lamps, RequiredTile|MultiTile, 0, 23)},
				{'_',new StructureUtils.StructureTile(0, OptionalTile|Deactivate)},
				{' ',new StructureUtils.StructureTile(0, Nothing)}
			}
			);
			StructureUtils.Structure pillar = new StructureUtils.Structure(
			new string[] {"bbbbbbbbbbbbbbbbbbbbbb"},
			new Dictionary<char, StructureUtils.StructureTile>() { {'b',new StructureUtils.StructureTile(brickType, OptionalTile)}}
			);
			int succ = 0;
			int xoff = 0;
			while(succ == 0&&xoff<x*0.75f) {
				succ = pyramid.Place(x+xoff, y);
				if(succ>0)break;
				if(xoff < 0) {
					xoff = -xoff;
				} else {
					xoff = (-xoff)-1;
				}
			}
			y += 4;
			while(succ>0) {
				succ = pillar.Place(x+xoff, ++y);
			}
			Chest chest = Main.chest[pyramid.createdChests.Dequeue()];
			if(loot.Length<=Chest.maxItems) {
				loot.CopyTo(chest.item, 0);
			} else {
				chest.item[39].SetDefaults(ModContent.ItemType<StartBag>());
				StartBag bag = chest.item[39].ModItem as StartBag;
				MethodInfo addItem = typeof(StartBag).GetMethod("AddItem", BindingFlags.Instance|BindingFlags.NonPublic);
				int i = 0;
				for(; i < 39; i++) {
					chest.item[i] = loot[i];
				}
				for(; i < loot.Length; i++) {
					addItem.Invoke(bag, new object[] {loot[i]});
				}
			}
		}
		public static WorldGenLegacyMethod Minefield(TwistConfig twistConfig) => (GenerationProgress progress, GameConfiguration configuration) => {
			if (progress is not null) progress.Message = "Placing Landmines";
			Tile tile;
			Tile tileBelow;
			float dord = twistConfig.Minefield;
			for (int y = 0; y < Main.maxTilesY - 1; y++) {
				for (int x = 0; x < Main.maxTilesX; x++) {
					tile = Main.tile[x, y];
					tileBelow = Main.tile[x, y + 1];
					if (!tile.HasTile && tileBelow.HasTile && tileBelow.Slope == 0 && !tileBelow.IsHalfBlock && Main.tileSolid[tileBelow.TileType] && (dord == 1 || WorldGen.genRand.NextFloat() < dord)) {
						tile.ResetToType(TileID.LandMine);
						//tile.type = TileID.LandMine;
						tile.TileColor = TwistExt.GetMineColor(tileBelow.TileType);
						tile.HasTile = true;
					}
				}
			}
		};
		public static WorldGenLegacyMethod Painter(TwistConfig twistConfig) => (GenerationProgress progress, GameConfiguration configuration) => {
			if (progress is not null) progress.Message = "Painting it,";
			Tile tile;
			byte color = (byte)twistConfig.Paint;
			for(int y = 0; y < Main.maxTilesY-1; y++) {
				for(int x = 0; x < Main.maxTilesX; x++) {
					tile = Main.tile[x, y];
					tile.TileColor = color;
					tile.WallColor = color;
				}
			}
		};
		public static WorldGenLegacyMethod Coater(TwistConfig twistConfig) => (GenerationProgress progress, GameConfiguration configuration) => {
			if (progress is not null) progress.Message = "Putting on a coat";
			Tile tile;
			byte color = (byte)twistConfig.PaintCoating;
			bool fullbright = twistConfig.PaintCoating.HasFlag(CoatingEnum.Glow);
			bool echo = twistConfig.PaintCoating.HasFlag(CoatingEnum.Echo);
			for(int y = 0; y < Main.maxTilesY-1; y++) {
				for(int x = 0; x < Main.maxTilesX; x++) {
					tile = Main.tile[x, y];
					if (fullbright) {
						tile.IsTileFullbright = true;
						tile.IsWallFullbright = true;
					}
					if (echo) {
						tile.IsTileInvisible = true;
						tile.IsWallInvisible = true;
					}
				}
			}
		};
		public static WorldGenLegacyMethod Switcher(TwistConfig twistConfig) => (GenerationProgress progress, GameConfiguration configuration) => {
			if (progress is not null) progress.Message = "Switcheroo";
			Tile tile;
			var watch = new Stopwatch();
			watch.Start();
			(Dictionary<int, int> tiles, Dictionary<int, int> walls) = SetupSwitch(twistConfig);
			for (int y = 0; y < Main.maxTilesY - 1; y++) {
				for (int x = 0; x < Main.maxTilesX; x++) {
					tile = Main.tile[x, y];
					Switch(tile, tiles, walls);
					//if(Switch(tile))WorldGen.SquareTileFrame(x,y);
				}
			}
			watch.Stop();
			ModContent.GetInstance<TwistWorld>().AddPairings(tiles.Where(kvp => kvp.Key > -1 && kvp.Value > -1).Select(kvp => ((ushort)kvp.Key, (ushort)kvp.Value)));
			WorldTwists.Instance.Logger.Info("Switched tiles in" + watch.Elapsed);
		};
		public static void TreeSolidifier(GenerationProgress progress, GameConfiguration configuration) {
			if (progress is not null) progress.Message = "Solidifying Trees";
			Tile tile;
			int wood;
			Item item = new();
			StructureUtils.Structure strukt;
			int fails = 0;
			UnifiedRandom rand = WorldGen.genRand;
			var watch = new Stopwatch();
			watch.Start();
			for(int y = 0; y < Main.maxTilesY-1; y++) {
				for(int x = 0; x < Main.maxTilesX; x++) {
					tile = Main.tile[x, y];
					if(!tile.HasTile||tile.TileType!=TileID.Trees)continue;
					wood = TwistExt.GetTreeWood(x,y);
					item.SetDefaults(wood);
					tile.TileType = (ushort)item.createTile;
					if(tile.IsTreeTop()) {
						strukt = TwistExt.GetTreetop(item.type, rand);
						if(strukt.Place(x-2,y-5,true)==0)
							++fails;
					}else {
						//try {
						WorldGen.SquareTileFrame(x, y);
						//} catch(IndexOutOfRangeException) { }
					}
				}
			}
			watch.Stop();

			WorldTwists.Instance.Logger.Info($"Solidified trees in {watch.Elapsed} with {fails} failures");
		}
		public static WorldGenLegacyMethod Waver(TwistConfig twistConfig) => (GenerationProgress progress, GameConfiguration configuration) => {
			if (progress is not null) progress.Message = "Waving";
			TwistExt.WaveType waveType = twistConfig.WaveType;
			double wavePeriod = twistConfig.WavePeriod;
			double waveIntensity = twistConfig.WaveIntensity;
			int diff = (int)Math.Ceiling(waveIntensity);
			TileData?[,] tiles = new TileData?[Main.tile.Width, Main.tile.Height];
			for (int y1 = 1 + diff; y1 < Main.maxTilesY; y1++) {
				for (int x1 = 0; x1 < Main.maxTilesX; x1++) {
					if (Main.tileContainer[Main.tile[x1, y1].TileType] || Main.tileContainer[Main.tile[x1, y1 - 1].TileType]) {
						tiles[x1, y1 - diff] = Main.tile[x1, y1];
					} else {
						int y = (y1 - diff) + (int)TwistExt.GetWave(waveType, x1, wavePeriod, waveIntensity);
						if (tiles[x1, y] is null) tiles[x1, y] = Main.tile[x1, y1];
					}
				}
			}
			for (int i = Main.chest.Length; i-- > 0;) {
				if (Main.chest[i] is null) continue;
				Main.chest[i].y -= diff;
			}
			for (int y2 = 0; y2 < Main.maxTilesY; y2++) {
				for (int x2 = 0; x2 < Main.maxTilesX; x2++) {
					Main.tile[x2, y2].SetTileData(tiles[x2, y2]??new());
					//if (!(tiles[x2, y2] is null)) Main.tile[x2, y2] = tiles[x2, y2];
					//else if (!(Main.tile[x2, y2] is null)) Main.tile[x2, y2].HasTile = false;
					//else Main.tile[x2, y2] = new Tile();
				}
			}
		};
		public static void BeeHell(GenerationProgress progress, GameConfiguration configuration) {
			if (progress is not null) progress.Message = "Bee Hell";
			for (int y = Main.maxTilesY - 200; y < Main.maxTilesY; y++) {
				for (int x = 0; x < Main.maxTilesX; x++) {
					if (Main.tile[x, y].TileType == TileID.Ash) {
						Main.tile[x, y].TileType = TileID.Hive;
					}
				}
			}
		}
		public static WorldGenLegacyMethod ShuffledWalls(TwistConfig twistConfig) => (GenerationProgress progress, GameConfiguration configuration) => {
			WorldTwists.Instance.Logger.Info("Shuffling Walls");
			if (progress is not null) progress.Message = "Shuffling Walls";
			//Dictionary<int,int> count
			List<ushort> types = new List<ushort>() { };
			Tile tile;
			for (int y = 0; y < Main.maxTilesY; y++) {
				for (int x = 0; x < Main.maxTilesX; x++) {
					tile = Main.tile[x, y];
					if (!types.Contains(tile.WallType)) types.Add(tile.WallType);
				}
			}
			List<ushort> shuffledTypes = types.ToList();
			//WorldTwists.Instance.Logger.Info("Randomize: Shuffled "+WorldGen.genRand.);
			bool UseWorldSeed = twistConfig.UseWorldSeed;
			bool UseComplexSeed = twistConfig.UseComplexSeed;
			WorldTwists.Instance.Logger.Info($"Shuffle: Using settings UseWorldSeed: {UseWorldSeed}; UseComplexSeed: {UseComplexSeed}");
			if (UseWorldSeed) {
				if (UseComplexSeed) {
					WorldTwists.Instance.Logger.Info("Shuffle: inext " + typeof(UnifiedRandom).GetField("inext", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(WorldGen.genRand));
					WorldTwists.Instance.Logger.Info("Shuffle: inextp " + typeof(UnifiedRandom).GetField("inextp", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(WorldGen.genRand));
					WorldTwists.Instance.Logger.Info("Shuffle: SeedArray: " + string.Join(",", (int[])typeof(UnifiedRandom).GetField("SeedArray", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(WorldGen.genRand)));
				}
				shuffledTypes.Shuffle(WorldGen.genRand);
			} else {
				UnifiedRandom rand = new UnifiedRandom(twistConfig.RandomizeSeed);
				if (UseComplexSeed) {
					typeof(UnifiedRandom).GetField("inext", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rand, twistConfig.inext);
					typeof(UnifiedRandom).GetField("inextp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rand, twistConfig.inextp);
					typeof(UnifiedRandom).GetField("SeedArray", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(rand, twistConfig.SeedArray.ToArray());
				}
				shuffledTypes.Shuffle(rand);
			}
			Dictionary<ushort, ushort>  wallPairings = new Dictionary<ushort, ushort>() { };
			string log = "";
			if (!twistConfig.ShuffledAirWalls) {
				wallPairings.Add(WallID.None, WallID.None);
				types.Remove(WallID.None);
			}
			for (int i = 0; i < types.Count; i++) {
				wallPairings.Add(types[i], shuffledTypes[i]);
				log += $"[ {types[i]} ( {TwistExt.GetTileName(types[i])}), {shuffledTypes[i]} ( {TwistExt.GetTileName(shuffledTypes[i])})] ";
			}
			WorldTwists.Instance.Logger.Info("Shuffle: Shuffled " + log);
			for (int y = 0; y < Main.maxTilesY; y++) {
				for (int x = 0; x < Main.maxTilesX; x++) {
					try {
						Main.tile[x, y].WallType = wallPairings[Main.tile[x, y].WallType];
					} catch (Exception) {
						WorldTwists.Instance.Logger.Info("Shuffle: Ran into issue randomizing " + Main.tile[x, y].WallType);
						throw;
					}
				}
			}
		};
		public static WorldGenLegacyMethod Shear(float mult) => (GenerationProgress progress, GameConfiguration configuration) => {
			if (progress is not null) progress.Message = "Shearing";
			int oobtiles = Main.offLimitBorderTiles;
			int width = Main.maxTilesX;
			int height = Main.maxTilesY;
			int x = -1;
			TileData[,] tiles = new TileData[Main.tile.Width, Main.tile.Height];
			for(int y1 = oobtiles; y1 < height-oobtiles; y1++) {
				for(int x1 = oobtiles; x1 < width-oobtiles; x1++) {
					//x = (x1 + (y1 * mult)+(width-oobtiles*3)) % (width-oobtiles*2)+oobtiles;
					x = x1 + (int)(y1 * mult);
					while(x < 0)x += (width - oobtiles * 2);
					x = x % (width - oobtiles * 2) + oobtiles;
					tiles[x, y1] = Main.tile[x1, y1];
				}
			}
			for(int y2 = 0; y2 < height; y2++) {
				for(int x2 = 0; x2 < width; x2++) {
					Main.tile[x2, y2].SetTileData(tiles[x2, y2]??new TileData());
				}
			}
			Chest c;
			Tile chestTile;
			//Tile[,] chestTiles = new Tile[3,3];
			for(int i = 0; i < Main.chest.Length; i++) {
				c = Main.chest[i];
				if(c is null) {
					continue;
				}
				c.x = (c.x + (int)(c.y * mult));
				while(c.x < 0)c.x += (width - oobtiles * 2);
				c.x = c.x % (width - oobtiles * 2) + oobtiles;
				chestTile = Main.tile[c.x, c.y + 1];
				//c.y--;
				try {
					MultiTileUtils.AggressivelyPlace(new Point(c.x, c.y), chestTile.TileType, chestTile.TileFrameX / MultiTileUtils.GetStyleWidth(chestTile.TileType));
				} catch(Exception e) {
					WorldTwists.Instance.Logger.Warn(e);
					Exception _ = e;
				}
			}
			//Point spawnPoint = new Point(Main.spawnTileX, Main.spawnTileY);
			Main.spawnTileX = (Main.spawnTileX + (int)(Main.spawnTileY * mult)) % Main.maxTilesX;
			//Point dungeonPoint = new Point(Main.spawnTileX, Main.spawnTileY);
			Main.dungeonX = (Main.dungeonX + (int)(Main.dungeonY * mult)) % Main.maxTilesX;

			int npci;
			for(npci = 0; npci < Main.npc.Length; npci++) {
				if(Main.npc[npci].type == NPCID.OldMan) {
					Main.npc[npci].position = new Vector2(Main.dungeonX, Main.dungeonY) * 16;
					break;
				}
			}
			for(npci = 0; npci < Main.npc.Length; npci++) {
				if(Main.npc[npci].type == NPCID.Guide) {
					Main.npc[npci].position = new Vector2(Main.spawnTileX, Main.spawnTileY) * 16;
					break;
				}
			}
		};
		public static WorldGenLegacyMethod Grid(TwistConfig twistConfig) => (GenerationProgress progress, GameConfiguration configuration) => {
			if (progress is not null) progress.Message = "Gridifying";
			int oobtiles = Main.offLimitBorderTiles;
			int width = Main.maxTilesX;
			int height = Main.maxTilesY;
			SkyGridSetting skyGrids = twistConfig.SkyGrids;
			for (int y = oobtiles; y < height - oobtiles; y++) {
				for (int x = oobtiles; x < width - oobtiles; x++) {
					if (!(Main.tile[x, y - 1].LiquidAmount > 0 || TileID.Sets.PreventsTileRemovalIfOnTopOfIt[Main.tile[x, y].TileType]) && !skyGrids.Combine(x, y)) {
						WorldGen.KillTile(x, y);
					}
				}
			}
		};
		public bool _smol = false;
		public static bool smol {
			get => ModContent.GetInstance<TwistWorld>()._smol;
			set => ModContent.GetInstance<TwistWorld>()._smol = value;
		}
		private MirrorDefaultDictionary<ushort> pairings = [];
		public static MirrorDefaultDictionary<ushort> Pairings {
			get => ModContent.GetInstance<TwistWorld>().pairings;
			set { if(value is not null) ModContent.GetInstance<TwistWorld>().pairings = value; }
		}
		public void AddPairings(IDictionary<ushort, ushort> newPairings) => AddPairings(newPairings.Select(kvp => (kvp.Key, kvp.Value)));
		public void AddPairings(IEnumerable<(ushort oldTile, ushort newTile)> newPairings) {
			List<(ushort oldTile, ushort newTile)> newerPairings = [];
			List<ushort> removedPairings = [];
			foreach ((ushort oldTile, ushort newTile) in newPairings) {
				newerPairings.Add((pairings[oldTile], newTile));
				removedPairings.Add(oldTile);
			}
			foreach (ushort oldTile in removedPairings) {
				pairings.Remove(oldTile);
			}
			foreach ((ushort oldTile, ushort newTile) in newPairings) {
				pairings[oldTile] = newTile;
			}
		}

		private HashSet<int> _bossKills;
		public static HashSet<int> bossKills {
			get {
				HashSet<int> value = ModContent.GetInstance<TwistWorld>()?._bossKills;
				if (value is not null) {
					return value;
				}
				return ModContent.GetInstance<TwistWorld>()._bossKills = new HashSet<int>();
			}
			set {
				if (value is not null) ModContent.GetInstance<TwistWorld>()._bossKills = value;
			}
		}
		public static (Dictionary<int, int> tiles, Dictionary<int, int> walls) SetupSwitch(TwistConfig twistConfig) {
			Dictionary<int, int> tiles = new(TileLoader.TileCount + 1);
			for (int i = -1; i < TileLoader.TileCount; i++) {
				if (i == -1 || !Main.tileFrameImportant[i] || twistConfig.JankLoot) {
					TileDefinition newTile = (i == -1) ? new TileDefinition() : new(i);
					foreach (TypeMapper<TileDefinition> current in twistConfig.TileMaps) {
						if (current.input.Contains(newTile) != current.inverted) {
							if (i == current.output.Type) break;
							tiles.Add(i, current.output.Type);
							break;
						}
					}
				}
			}
			Dictionary<int, int> walls = new(TileLoader.TileCount + 1);
			for (int i = 0; i < WallLoader.WallCount; i++) {
				WallDefinition newTile = new(i);
				foreach (TypeMapper<WallDefinition> current in twistConfig.WallMaps) {
					if (current.input.Contains(newTile) != current.inverted) {
						if (i == current.output.Type) break;
						walls.Add(i, current.output.Type);
						break;
					}
				}
			}
			return (tiles, walls);
		}
		public static bool Switch(Tile tile, Dictionary<int, int> tiles, Dictionary<int, int> walls) {
			bool reframe = false;

			int tileType = tile.HasTile ? tile.TileType : -1;
			if (tiles.TryGetValue(tileType, out int newTile)) {
				if (newTile == -1) {
					tile.HasTile = false;
				} else {
					tile.TileType = (ushort)newTile;
					reframe = true;
				}
			}

			if (walls.TryGetValue(tile.WallType, out int newWall)) tile.WallType = (ushort)newWall;
			return reframe;
		}
		public override void SaveWorldData(TagCompound tag)/* tModPorter Suggestion: Edit tag parameter instead of returning new TagCompound */ {
			try {
				tag.Add("smol", smol);
				if(pairings?.Count>0) {
					tag.Add("pairingKeys", pairings.Keys.ToList());
					tag.Add("pairingValues", pairings.Values.ToList());
				}
				if (_bossKills is not null) {
					tag.Add("bossKills", _bossKills.ToList());
				}
			} catch(Exception) {}
		}
		public override void LoadWorldData(TagCompound tag) {
			try {
				if(tag.ContainsKey("smol"))smol = tag.GetBool("smol");
				Pairings = [];
				if (tag.ContainsKey("pairingKeys") && tag.ContainsKey("pairingValues")) {
					List<ushort> keys = tag.Get<List<ushort>>("pairingKeys");
					List<ushort> values = tag.Get<List<ushort>>("pairingValues");
					AddPairings(keys.Zip(values, (k, v) => (k, v)));
				}
				if (tag.ContainsKey("bossKills")) {
					_bossKills = tag.Get<List<int>>("bossKills").ToIntSet();
				}
			} catch(Exception) {}
		}
		public override void TileCountsAvailable(ReadOnlySpan<int> tileCounts) {
			if(TwistConfig.Instance.KeepDungeon) {
				try {
					addPropertyTilePairing(nameof(SceneMetrics.DungeonTileCount), tileCounts, TileID.BlueDungeonBrick, TileID.GreenDungeonBrick, TileID.PinkDungeonBrick);
					addPropertyTilePairing(nameof(SceneMetrics.EvilTileCount), tileCounts, TileID.Ebonstone);
					addPropertyTilePairing(nameof(SceneMetrics.BloodTileCount), tileCounts, TileID.Crimstone);
					addPropertyTilePairing(nameof(SceneMetrics.JungleTileCount), tileCounts, TileID.JungleGrass, TileID.LihzahrdBrick);
					addPropertyTilePairing(nameof(SceneMetrics.MushroomTileCount), tileCounts, TileID.MushroomGrass);
					addPropertyTilePairing(nameof(SceneMetrics.SnowTileCount), tileCounts, TileID.SnowBlock, TileID.IceBlock);
					//addTilePairing(ref Main.SceneMetrics.DungeonTileCount, TileID.BlueDungeonBrick, tileCounts);
					//addTilePairing(ref Main.SceneMetrics.DungeonTileCount, TileID.GreenDungeonBrick, tileCounts);
					//addTilePairing(ref Main.SceneMetrics.DungeonTileCount, TileID.PinkDungeonBrick, tileCounts);
					//addTilePairing(ref Main.SceneMetrics.EvilTileCount, TileID.Ebonstone, tileCounts);
					//addTilePairing(ref Main.SceneMetrics.BloodTileCount, TileID.Crimstone, tileCounts);
					//addTilePairing(ref Main.SceneMetrics.JungleTileCount, TileID.JungleGrass, tileCounts);
					//addTilePairing(ref Main.SceneMetrics.JungleTileCount, TileID.LihzahrdBrick, tileCounts);
					//addTilePairing(ref Main.SceneMetrics.MushroomTileCount, TileID.MushroomGrass, tileCounts);
					//addTilePairing(ref Main.SceneMetrics.SnowTileCount, TileID.SnowBlock, tileCounts);
					//addTilePairing(ref Main.SceneMetrics.SnowTileCount, TileID.IceBlock, tileCounts);
				} catch(Exception) { }
			}
		}
		private void addPropertyTilePairing(string propertyName, ReadOnlySpan<int> tileCounts, params ushort[] ids) {
			PropertyInfo count = null;
			for (int i = 0; i < tileCounts.Length; i++) {
				ushort id = ids[i];
				if (Pairings.ContainsKey(id)) {
					count ??= typeof(SceneMetrics).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
					count.SetValue(Main.SceneMetrics, (ushort)(((int)count.GetValue(Main.SceneMetrics)) + tileCounts[Pairings[id]]));
				}
			}
		}
		private bool oldDayTime;
		public override void PostUpdateWorld() {
			if(smol&&Main.dayTime!=oldDayTime&&!NPC.downedBoss3&&NPC.CountNPCS(NPCID.OldMan)<1) {
				NPC.NewNPC(NPC.GetSource_TownSpawn(), Main.dungeonX*16,Main.dungeonY*16,NPCID.OldMan);
			}
			oldDayTime = Main.dayTime;
		}
		public override void PreWorldGen() {
			TwistExt.ApplySecretSeeds(TwistConfig.Instance.preGeneration);
		}
		public override void PostWorldGen() {
			TwistExt.ApplySecretSeeds(TwistConfig.Instance.postGeneration);
		}
	}
	public class TwistGlobalNPC : GlobalNPC {
		public override void OnKill(NPC npc) {
			if (npc.boss && Main.netMode != NetmodeID.MultiplayerClient) {
				if (RetwistConfig.Instance.TrackKillBoss) {
					if (!TwistWorld.bossKills.Contains(npc.type) && npc.type != NPCID.WallofFlesh) {
						ThreadPool.QueueUserWorkItem(BossKillCallback, 1);
						TwistWorld.bossKills.Add(npc.type);
					}
				} else {
					ThreadPool.QueueUserWorkItem(BossKillCallback, 1);
				}
			}
		}
		public static void BossKillCallback(object threadContext) {
			List<GenPass> tasks = [];
			TwistWorld.AddGenTasks(RetwistConfig.Instance.KillBoss, tasks);
			foreach (GenPass item in tasks) {
				item.Apply(null, null);
			}
		}
	}
	public class TwistPlayer : ModPlayer {
		public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource) {
			if (Main.netMode != NetmodeID.MultiplayerClient) { 
				ThreadPool.QueueUserWorkItem(PlayerDeathCallback, 1);
			}
		}
		public static void PlayerDeathCallback(object threadContext) {
			List<GenPass> tasks = [];
			TwistWorld.AddGenTasks(RetwistConfig.Instance.PlayerDeath, tasks);
			foreach (GenPass item in tasks) {
				item.Apply(null, null);
			}
		}
	}
	public static class TwistExt {
		public static void Shuffle<T>(this IList<T> list, UnifiedRandom rng = null) {
			rng ??= WorldGen.genRand;

			int n = list.Count;
			while (n > 1) {
				n--;
				int k = rng.Next(n + 1);
				(list[n], list[k]) = (list[k], list[n]);
			}
		}
		public static string NextString(this UnifiedRandom rand, params string[] options) {
			return rand.Next(options);
		}
		public static bool Solid(int type) {
			return Main.tileSolid[type] && (TwistConfig.Instance.RandomizePlatforms || !Main.tileSolidTop[type]);
		}
		public static string GetTileName(int type) {
			return type<TileID.Count ? TileID.Search.GetName(type) : TileLoader.GetTile(type).Name;
		}
		public static byte GetMineColor(ushort tileType, UnifiedRandom random = null) {
			random ??= WorldGen.genRand;
			switch(tileType) {

				case TileID.Gold:
				case TileID.HoneyBlock:
				return PaintID.DeepYellowPaint;

				case TileID.SandStoneSlab:
				case TileID.SandstoneBrick:
				case TileID.Sand:
				return PaintID.YellowPaint;

				case TileID.Palladium:
				case TileID.PalladiumColumn:
				case TileID.Copper:
				case TileID.LihzahrdBrick:
				case TileID.DynastyWood:
				case TileID.PumpkinBlock:
				case TileID.CrispyHoneyBlock:
				case TileID.Hive:
				case TileID.Sandstone:
				return PaintID.OrangePaint;

				case TileID.FleshBlock:
				case TileID.FleshIce:
				case TileID.Crimstone:
				case TileID.Adamantite:
				case TileID.AdamantiteBeam:
				case TileID.RedDynastyShingles:
				case TileID.HellstoneBrick:
				return PaintID.RedPaint;

				case TileID.Obsidian:
				case TileID.DemoniteBrick:
				case TileID.SpookyWood:
				case TileID.Ebonwood:
				case TileID.Ebonsand:
				case TileID.CorruptHardenedSand:
				case TileID.CorruptSandstone:
				case TileID.CorruptIce:
				case TileID.Ebonstone:
				return PaintID.PurplePaint;

				case TileID.PlatinumBrick:
				case TileID.Silver:
				case TileID.MarbleBlock:
				case TileID.Marble:
				case TileID.Pearlsand:
				case TileID.Cloud:
				case TileID.SnowBrick:
				case TileID.SnowBlock:
				return PaintID.WhitePaint;

				case TileID.Cobalt:
				case TileID.CobaltBrick:
				case TileID.BlueDynastyShingles:
				case TileID.EbonstoneBrick:
				case TileID.IceBrick:
				case TileID.IceBlock:
				case TileID.BreakableIce:
				return PaintID.SkyBluePaint;

				case TileID.Tin:
				case TileID.Iron:
				case TileID.LivingWood:
				case TileID.PalmWood:
				case TileID.Pearlwood:
				case TileID.BoneBlock:
				case TileID.DesertFossil:
				case TileID.FossilOre:
				return PaintID.BrownPaint;

				case TileID.PinkDungeonBrick:
				case TileID.RichMahogany:
				case TileID.LivingMahogany:
				return PaintID.PinkPaint;

				case TileID.Orichalcum:
				case TileID.BubblegumBlock:
				case TileID.HallowSandstone:
				case TileID.HallowHardenedSand:
				case TileID.HallowedIce:
				return PaintID.VioletPaint;

				case TileID.Chlorophyte:
				case TileID.ChlorophyteBrick:
				case TileID.LivingMahoganyLeaves:
				case TileID.JungleGrass:
				return PaintID.LimePaint;

				case TileID.LeafBlock:
				return PaintID.GreenPaint;

				case TileID.Tungsten:
				case TileID.Cactus:
				return PaintID.DeepSkyBluePaint;

				case TileID.Granite:
				case TileID.GraniteBlock:
				case TileID.MushroomBlock:
				case TileID.MushroomGrass:
				return PaintID.BluePaint;

				case TileID.ShroomitePlating:
				return PaintID.DeepBluePaint;

				case TileID.Titanstone:
				case TileID.Lead:
				case TileID.Asphalt:
				case TileID.ObsidianBrick:
				return PaintID.BlackPaint;

				case TileID.Mythril:
				case TileID.MythrilBrick:
				return PaintID.DeepTealPaint;

				case TileID.Crimsand:
				case TileID.CrimsonHardenedSand:
				case TileID.CrimsonSandstone:
				case TileID.CrimtaneBrick:
				case TileID.Crimtane:
				return random.NextBool()?PaintID.RedPaint:PaintID.BlackPaint;

				case TileID.CrimsonGrass:
				return random.NextBool()?PaintID.RedPaint : PaintID.GrayPaint;

				case TileID.CorruptGrass:
				return random.NextBool()?PaintID.RedPaint : PaintID.GrayPaint;

				case TileID.HallowedGrass:
				return random.NextBool()?PaintID.SkyBluePaint : PaintID.GrayPaint;

				case TileID.GreenDungeonBrick:
				return random.NextBool()?PaintID.DeepSkyBluePaint : PaintID.GrayPaint;

				case TileID.BorealWood:
				case TileID.WoodenBeam:
				case TileID.WoodBlock:
				default:
				return PaintID.GrayPaint;
			}
		}
		public static bool IsTreeTop(this Tile tile) => tile.TileFrameY >= 198 && tile.TileFrameX >= 22;
		public static int GetTreeWood(int i, int j) {
			Tile tile = Framing.GetTileSafely(i, j);
			int wood = ItemID.Wood;
			int x = i;
			int y = j;

			#region repositioning
			if(tile.TileFrameX == 66 && tile.TileFrameY <= 45) {
				x++;
			}
			if (tile.TileFrameX == 88 && tile.TileFrameY >= 66 && tile.TileFrameY <= 110) {
				x--;
			}
			if (tile.TileFrameX == 22 && tile.TileFrameY >= 132 && tile.TileFrameY <= 176) {
				x--;
			}
			if (tile.TileFrameX == 44 && tile.TileFrameY >= 132 && tile.TileFrameY <= 176) {
				x++;
			}
			if (tile.TileFrameX == 44 && tile.TileFrameY >= 198) {
				x++;
			}
			if (tile.TileFrameX == 66 && tile.TileFrameY >= 198) {
				x--;
			}
			#endregion

			for(; !Main.tile[x, y].HasTile || !Main.tileSolid[Main.tile[x, y].TileType]; y++);
			if (Main.tile[x, y].HasTile) {
				switch (Main.tile[x, y].TileType) {
					case TileID.CorruptGrass:
					wood = ItemID.Ebonwood;
					break;
					case TileID.JungleGrass:
					wood = ItemID.RichMahogany;
					break;
					case TileID.HallowedGrass:
					wood = ItemID.Pearlwood;
					break;
					case TileID.CrimsonGrass:
					wood = ItemID.Shadewood;
					break;
					case TileID.MushroomGrass:
					wood = ItemID.GlowingMushroom;
					break;
					case TileID.SnowBlock:
					wood = ItemID.BorealWood;
					break;
				}
				TileLoader.DropTreeWood(Main.tile[x, y].TileType, ref wood);
			}
			return wood;
		}
		public static StructureUtils.Structure GetTreetop(int drop, UnifiedRandom rand = null) {
			rand ??= WorldGen.genRand;

			string[] map = new string[] {
			"     ",
			" aaa ",
			"aaaaa",
			"aaaaa",
			"aalaa",
			" ala "
			};
			byte paint = PaintID.DeepGreenPaint;
			switch(drop) {
				case ItemID.Pearlwood:
				return GetHallowedTreetop((byte)rand.Next(16));
				case ItemID.RichMahogany:
				paint = 0;
				map = new string[] {
				"     ",
				"aaaaa",
				"aaaaa",
rand.NextString("aaaaa","alaaa","aaala","alala"),
				" l l ",
				" 1l2 "
				};
				break;
				case ItemID.Ebonwood:
				paint = PaintID.DeepPurplePaint;
				break;
				case ItemID.Shadewood:
				paint = PaintID.DeepRedPaint;
				map = new string[] {
				"     ",
				"aaaaa",
				"aaaaa",
				"  l  ",
				"aaaaa",
				"  l  "
				};
				break;
				case ItemID.BorealWood:
				int t = rand.Next(4);
				map = new string[] {
				"     ",
				"     ",
				"     ",
				"     ",
  new string[] {" l3  " ,"  4l " ," l l ","  l  "}[t],
  new string[] {" 2l  " ,"  l2 " ," 1l2 ","  l  "}[t]
				};
				break;
				case ItemID.GlowingMushroom:
				map = new string[] {
				"     ",
				"     ",
				"     ",
				" lll ",
				"lllll",
				"lllll"
				};
				break;
			}
			Item i = new Item(drop);
			return new StructureUtils.Structure(map,
			('a',new StructureUtils.StructureTile(TileID.LivingMahoganyLeaves, OptionalTile, slopeType: BlockType.Solid, paint:paint)),
			('l',new StructureUtils.StructureTile((ushort)i.createTile, OptionalTile)),
			('1',new StructureUtils.StructureTile((ushort)i.createTile, OptionalTile, BlockType.SlopeUpRight)),
			('2',new StructureUtils.StructureTile((ushort)i.createTile, OptionalTile, BlockType.SlopeUpLeft)),
			('3',new StructureUtils.StructureTile((ushort)i.createTile, OptionalTile, BlockType.SlopeDownRight)),
			('4',new StructureUtils.StructureTile((ushort)i.createTile, OptionalTile, BlockType.SlopeDownLeft)),
			(' ',new StructureUtils.StructureTile(0, Nothing))
			);
		}
		public static StructureUtils.Structure GetHallowedTreetop(byte type) {
			string[] map;
			switch(type%4) {
				case 0:
				map = new string[] {
				"  a  ",
				" aaa ",
				" aaa ",
				"aaaaa",
				"aaaaa",
				"aalaa"
				};
				break;
				case 1:
				map = new string[] {
				"     ",
				"  a  ",
				" aaa ",
				" aaa ",
				"aaaaa",
				"aalaa"
				};
				break;
				case 2:
				map = new string[] {
				"  a  ",
				" aaa ",
				" aaa ",
				" aaa ",
				" aaa ",
				" ala "
				};
				break;
				default:
				map = new string[] {
				"     ",
				"     ",
				"  a  ",
				" aaa ",
				"aaaaa",
				"aalaa"
				};
				break;
			}
			byte paint;
			switch(type/4) {
				case 0:
				paint = PaintID.DeepPinkPaint;
				break;
				case 1:
				paint = PaintID.DeepVioletPaint;
				break;
				case 2:
				paint = PaintID.DeepSkyBluePaint;
				break;
				default:
				paint = PaintID.DeepYellowPaint;
				break;
			}
			return new StructureUtils.Structure(map,
			('a',new StructureUtils.StructureTile(TileID.LivingMahoganyLeaves, OptionalTile, paint:paint)),
			('l',new StructureUtils.StructureTile(TileID.Pearlwood, OptionalTile)),
			(' ',new StructureUtils.StructureTile(0, Nothing))
			);
		}
		public static GenPass GetDuplicatePassForMoreGen(GenPass pass, int dupeCount) {
			switch(pass.Name) {
				case "Dungeon":
				return new PassLegacy(dupeCount>0?$"Dungeon {dupeCount+2}":"Dungeon 2: Electric Boogaloo", (GenerationProgress progress, GameConfiguration configuration) => {
					int XPos = 0;
					double approach = 0.1 * dupeCount;
					if (GenVars.dungeonX > Main.maxTilesX/2) {
						XPos = WorldGen.genRand.Next((int)(Main.maxTilesX * (0.05+approach)), (int)(Main.maxTilesX * (0.2+approach)));
					} else {
						XPos = WorldGen.genRand.Next((int)(Main.maxTilesX * (0.8-approach)), (int)(Main.maxTilesX * (0.95-approach)));
					}
					int y8 = (int)((Main.worldSurface + Main.rockLayer) / 2.0) + WorldGen.genRand.Next(-200, 200);
					WorldGen.MakeDungeon(XPos, y8);
				});
				case "Tunnels":
				case "Mount Caves":
				case "Small Holes":
				case "Dirt Layer Caves":
				case "Rock Layer Caves":
				case "Surface Caves":
				case "Jungle":
				case "Marble":
				case "Granite":
				case "Full Desert":
				case "Floating Islands":
				case "Mushroom Patches":
				case "Shinies":
				case "Lakes":
				case "Gems":
				case "Pyramids":
				case "Living Trees":
				case "Altars":
				case "Hives":
				case "Jungle Chests":
				case "Ice":
				case "Traps":
				case "Life Crystals":
				case "Statues":
				case "Buried Chests":
				case "Surface Chests":
				case "Water Chests":
				case "Spider Caves":
				case "Gem Caves":
				case "Jungle Trees":
				case "Pots":
				case "Hellforge":
				case "Moss":
				case "Sunflowers":
				case "Planting Trees":
				case "Herbs":
				case "Dye Plants":
				case "Weeds":
				case "Jungle Plants":
				case "Vines":
				case "Flowers":
				case "Mushrooms":
				case "Stalac":
				case "Gems In Ice Biome":
				case "Random Gems":
				case "Larva":
				case "Micro Biomes":
				case "Jungle Temple":
				case "Temple":
				return pass;
			}
			return null;
		}
		public enum WaveType {
			Square,
			Sawtooth,
			Triangle,
			Sine
		}
		public static double GetWave(WaveType type, double position, double length, double multiplier) {
			switch(type) {
				case WaveType.Square:
				return (position%(length*2))>length?0:multiplier;
				case WaveType.Sawtooth:
				length *= 2;
				return ((position%length)/length)*multiplier;
				case WaveType.Triangle:
				length *= 2;
				return (1-2*Math.Abs(Math.Round(position/length)-(position/length)))*multiplier;
				case WaveType.Sine:
				return (Math.Sin(position * Math.PI / length) + 1)*multiplier/2;
			}
			return 0;
		}
		public static HashSet<int> ToIntSet(this List<int> list) {
			HashSet<int> output = new HashSet<int>();
			for (int i = list.Count - 1; i >= 0; --i) {
				output.Add(list[i]);
			}
			return output;
		}
		public static void ApplySecretSeeds(SecretSeedConfig config) {
			if (config is null) return;
			switch (config.DrunkWorld) {
				case OnOffNeutralEnum.ON:
				WorldGen.drunkWorldGen = Main.drunkWorld = true;
				break;
				case OnOffNeutralEnum.OFF:
				WorldGen.drunkWorldGen = Main.drunkWorld = false;
				break;
			}
			switch (config.BeeWorld) {
				case OnOffNeutralEnum.ON:
				WorldGen.notTheBees = Main.notTheBeesWorld = true;
				break;
				case OnOffNeutralEnum.OFF:
				WorldGen.notTheBees = Main.notTheBeesWorld = false;
				break;
			}
			switch (config.GitGudWorld) {
				case OnOffNeutralEnum.ON:
				WorldGen.getGoodWorldGen = Main.getGoodWorld = true;
				break;
				case OnOffNeutralEnum.OFF:
				WorldGen.getGoodWorldGen = Main.getGoodWorld = false;
				break;
			}
			switch (config.AniversaryWorld) {
				case OnOffNeutralEnum.ON:
				WorldGen.tenthAnniversaryWorldGen = Main.tenthAnniversaryWorld = true;
				break;
				case OnOffNeutralEnum.OFF:
				WorldGen.tenthAnniversaryWorldGen = Main.tenthAnniversaryWorld = false;
				break;
			}
			switch (config.DrunkWorld) {
				case OnOffNeutralEnum.ON:
				WorldGen.dontStarveWorldGen = Main.dontStarveWorld = true;
				break;
				case OnOffNeutralEnum.OFF:
				WorldGen.dontStarveWorldGen = Main.dontStarveWorld = false;
				break;
			}
		}
	}
	public enum PaintEnum {
		None = 0,
		Red = 1,
		Orange = 2,
		Yellow = 3,
		Lime = 4,
		Green = 5,
		Teal = 6,
		Cyan = 7,
		SkyBlue = 8,
		Blue = 9,
		Purple = 10,
		Violet = 11,
		Pink = 12,
		DeepRed = 13,
		DeepOrange = 14,
		DeepYellow = 15,
		DeepLime = 16,
		DeepGreen = 17,
		DeepTeal = 18,
		DeepCyan = 19,
		DeepSkyBlue = 20,
		DeepBlue = 21,
		DeepPurple = 22,
		DeepViolet = 23,
		DeepPink = 24,
		Black = 25,
		White = 26,
		Gray = 27,
		Brown = 28,
		Shadow = 29,
		Negative = 30,
	}
	public enum CoatingEnum {
		None = PaintCoatingID.None,
		Glow = PaintCoatingID.Glow,
		Echo = PaintCoatingID.Echo,
		Both = PaintCoatingID.Glow | PaintCoatingID.Echo,
	}
	public enum OnOffNeutralEnum : sbyte {
		OFF = -1,
		NO_CHANGE = 0,
		ON = 1
	}
	public class SkyGridSetting {
		public int x;
		public int y;
		public SkyGridModeEnum coordCombineMode;
		public SkyGridModeEnum parentCombineMode;
		public SkyGridSetting child;
		public void CombineIn(int x, int y, ref bool value) {
			if (this.x == 0 || this.y == 0) {
				return;
			}
			bool current = false;
			switch (coordCombineMode) {
				case SkyGridModeEnum.OR:
				current = (x % this.x == 0) || (y % this.y == 0);
				break;
				case SkyGridModeEnum.AND:
				current = (x % this.x == 0) && (y % this.y == 0);
				break;
				case SkyGridModeEnum.XOR:
				current = (x % this.x == 0) ^ (y % this.y == 0);
				break;
				case SkyGridModeEnum.XNOR:
				current = !((x % this.x == 0) ^ (y % this.y == 0));
				break;
			}
			switch (parentCombineMode) {
				case SkyGridModeEnum.OR:
				value |= current;
				break;
				case SkyGridModeEnum.AND:
				value &= current;
				break;
				case SkyGridModeEnum.XOR:
				value ^= current;
				break;
				case SkyGridModeEnum.XNOR:
				value ^= !current;
				break;
			}
		}
		public bool Combine(int x, int y) {
			bool value = true;
			SkyGridSetting current = this;
			while (current is not null) {
				current.CombineIn(x, y, ref value);
				current = current.child;
			}
			return value;
		}
	}
	public enum SkyGridModeEnum : sbyte {
		OR = 0,
		AND = 1,
		XOR = 2,
		XNOR = 3
	}
	public class SkyGridSettingConverter : JsonConverter {
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			if (!reader.Read()) {
				return null;
			}
			SkyGridSetting value = new SkyGridSetting();
			while (reader.TokenType == JsonToken.PropertyName) {
				string name = reader.Value.ToString().ToLower();

				switch (name) {
					case "x":
					if (int.TryParse(reader.ReadAsString(), out int x)) value.x = x;
					break;
					case "y":
					if (int.TryParse(reader.ReadAsString(), out int y)) value.y = y;
					break;
					case "coordcombinemode":
					if(Enum.TryParse(reader.ReadAsString(), out SkyGridModeEnum coordCombineMode))value.coordCombineMode = coordCombineMode;
					break;
					case "parentcombinemode":
					if (Enum.TryParse(reader.ReadAsString(), out SkyGridModeEnum parentCombineMode)) value.parentCombineMode = parentCombineMode;
					break;
					case "child":
					reader.Read();
					if (reader.TokenType == JsonToken.StartObject) {
						value.child = (SkyGridSetting)ReadJson(reader, objectType, existingValue, serializer);
					}
					break;
					default:
					reader.Skip();
					break;
				}
				reader.Read();
			}
			return value;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			SkyGridSetting val = (SkyGridSetting)value;
			writer.WriteStartObject();
			writer.WritePropertyName("x");
			writer.WriteValue(val.x);
			writer.WritePropertyName("y");
			writer.WriteValue(val.y);
			writer.WritePropertyName("coordCombineMode");
			writer.WriteValue(Enum.GetName(val.coordCombineMode));
			writer.WritePropertyName("parentCombineMode");
			writer.WriteValue(Enum.GetName(val.parentCombineMode));
			if (val.child is SkyGridSetting child) {
				writer.WritePropertyName("child");
				WriteJson(writer, child, serializer);
			}
			writer.WriteEndObject();
		}
		public override bool CanConvert(Type objectType) {
			return objectType == typeof(SkyGridSetting);
		}
	}
	[BackgroundColor(99, 111, 153)]
	public class TypeMapper<T>(List<T> input, T output, bool inverted) where T : EntityDefinition, new() {
		public List<T> input = input;
		public T output = output;
		public bool inverted = inverted;
		public TypeMapper() : this([], new T(), false) { }

		public override bool Equals(object obj) {
			if (obj is TypeMapper<T> other)
				return inverted == other.inverted && output == other.output && input.ToHashSet().SetEquals(other.input);
			return base.Equals(obj);
		}
		public override int GetHashCode() {
			return new { input = ((IStructuralEquatable)input).GetHashCode(EqualityComparer<T>.Default), output, inverted }.GetHashCode();
		}
		public override string ToString() {
			return $"{(inverted ? "not " : "")} {string.Join(",", input)}->{output}";
		}
	}
	[Serializable]
	public class InvalidFormatException : Exception {
		public InvalidFormatException() { }
		public InvalidFormatException(string message) : base(message) { }
		public InvalidFormatException(string message, Exception inner) : base(message, inner) { }
		protected InvalidFormatException(
			SerializationInfo info,
			StreamingContext context) : base(info, context) { }
	}
	public class Debugging_Item : ModItem {
		public override string Texture => "Terraria/Images/NPC_420";
		public override void SetDefaults() {
			Item.useStyle = 5;
			Item.useAnimation = 10;
			Item.useTime = 10;
		}
		public override bool? UseItem(Player player) {
			//TwistExt.GetTreetop(9).Place((int)(Main.MouseWorld.X / 16), (int)(Main.MouseWorld.Y / 16));
			TileData datum = Main.tile[Player.tileTargetX, Player.tileTargetY];
			TileData[] data = new TileData[] { datum };
			switch (data[0].TileWallWireStateData.Slope) {
				case SlopeType.SlopeDownLeft:
				data[0].TileWallWireStateData.Slope = SlopeType.SlopeUpLeft;
				break;
				case SlopeType.SlopeUpLeft:
				data[0].TileWallWireStateData.Slope = SlopeType.SlopeDownLeft;
				break;
				case SlopeType.SlopeDownRight:
				data[0].TileWallWireStateData.Slope = SlopeType.SlopeUpRight;
				break;
				case SlopeType.SlopeUpRight:
				data[0].TileWallWireStateData.Slope = SlopeType.SlopeDownRight;
				break;
			}
			Main.tile[Player.tileTargetX, Player.tileTargetY].SetTileData(data[0]);
			return true;
		}
	}
	[TypeConverter(typeof(ToFromStringConverter<WallDefinition>))]
	public class WallDefinition : EntityDefinition {
		public static readonly Func<TagCompound, WallDefinition> DESERIALIZER = Load;

		// Tile ids start from 0 for some reason
		public override bool IsUnloaded
			=> Type < 0 && !(Mod == "Terraria" && Name == "None" || Mod == "" && Name == "");

		// TODO: doesn't handle tile styles, should implement when NPCs get negative id support
		public override int Type => WallID.Search.TryGetId(Mod != "Terraria" ? $"{Mod}/{Name}" : Name, out int id) ? id : -1;

		public WallDefinition() : base() { }
		/// <summary><b>Note: </b>As ModConfig loads before other content, make sure to only use <see cref="WallDefinition(string, string)"/> for modded content in ModConfig classes. </summary>
		public WallDefinition(int type) : base(WallID.Search.GetName(type)) { }
		public WallDefinition(string key) : base(key) { }
		public WallDefinition(string mod, string name) : base(mod, name) { }

		public static WallDefinition FromString(string s) => new(s);

		public static WallDefinition Load(TagCompound tag) => new(tag.GetString("mod"), tag.GetString("name"));

		public override string DisplayName => IsUnloaded || Type == -1 ? Language.GetTextValue("Mods.ModLoader.Unloaded") : Name;
	}
	public class MirrorDefaultDictionary<T> : IDictionary<T, T> {
		readonly Dictionary<T, T> dict = [];
		public T this[T key] {
			get => dict.TryGetValue(key, out T value) ? value : key;
			set => dict[key] = value;
		}
		public ICollection<T> Keys => dict.Keys;
		public ICollection<T> Values => dict.Values;
		public int Count => dict.Count;
		public bool IsReadOnly => false;
		public void Add(T key, T value) => dict.Add(key, value);
		public void Add(KeyValuePair<T, T> item) => ((ICollection<KeyValuePair<T, T>>)dict).Add(item);
		public void Clear() => ((ICollection<KeyValuePair<T, T>>)dict).Clear();
		public bool Contains(KeyValuePair<T, T> item) => ((ICollection<KeyValuePair<T, T>>)dict).Contains(item);
		public bool ContainsKey(T key) => ((IDictionary<T, T>)dict).ContainsKey(key);
		public void CopyTo(KeyValuePair<T, T>[] array, int arrayIndex) => ((ICollection<KeyValuePair<T, T>>)dict).CopyTo(array, arrayIndex);
		public IEnumerator<KeyValuePair<T, T>> GetEnumerator() => ((IEnumerable<KeyValuePair<T, T>>)dict).GetEnumerator();
		public bool Remove(T key) => ((IDictionary<T, T>)dict).Remove(key);
		public bool Remove(KeyValuePair<T, T> item) => ((ICollection<KeyValuePair<T, T>>)dict).Remove(item);
		public bool TryGetValue(T key, [MaybeNullWhen(false)] out T value) {
			if (dict.TryGetValue(key, out value)) return true;
			value = key;
			return false;
		}
		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)dict).GetEnumerator();
	}
	[CustomModConfigItem(typeof(LiquidDefinitionConfigElement))]
	[TypeConverter(typeof(ToFromStringConverter<TileDefinition>))]
	public class LiquidDefinition : EntityDefinition {
		public static readonly Func<TagCompound, TileDefinition> DESERIALIZER = Load;
		public override bool IsUnloaded
			=> Type < 0 && !(Mod == "Terraria" && Name == "None" || Mod == "" && Name == "");
		public override int Type => Name switch {
			nameof(LiquidID.Water) => LiquidID.Water,
			nameof(LiquidID.Lava) => LiquidID.Lava,
			nameof(LiquidID.Honey) => LiquidID.Honey,
			nameof(LiquidID.Shimmer) => LiquidID.Shimmer,
			_ => -1,
		};
		public LiquidDefinition() : base() { }
		public LiquidDefinition(int type) : base(type switch {
			LiquidID.Water => nameof(LiquidID.Water),
			LiquidID.Lava => nameof(LiquidID.Lava),
			LiquidID.Honey => nameof(LiquidID.Honey),
			LiquidID.Shimmer => nameof(LiquidID.Shimmer),
			_ => throw new NotImplementedException(),
		}) { }
		public LiquidDefinition(string key) : base(key) { }
		public LiquidDefinition(string mod, string name) : base(mod, name) { }
		public static TileDefinition Load(TagCompound tag)
			=> new(tag.GetString("mod"), tag.GetString("name"));
	}
	public class LiquidDefinitionConfigElement : ConfigElement<LiquidDefinition> {
		protected bool pendingChanges = false;
		public override void OnBind() {
			base.OnBind();
			base.TextDisplayFunction = TextDisplayOverride ?? base.TextDisplayFunction;
			pendingChanges = true;
			SetupList();
		}
		public Func<string> TextDisplayOverride { get; set; }
		protected void SetupList() {
			RemoveAllChildren();
			Main.UIScaleMatrix.Decompose(out Vector3 scale, out _, out _);
			float left = 26 + 4;
			float top = 4;
			float width = 408f * scale.X;
			for (int i = 3; i >= 0; i--) {
				LiquidDefinitionElement element = new(i) {
					Left = new StyleDimension(-left, 1),
					Top = new StyleDimension(top, 0)
				};
				element.OnLeftClick += (_, _) => {
					Value = new(element.type);
				};
				element.getSelectedLiquid = () => Value.Type;
				element.Initialize();
				Append(element);
				left += 26 + 4;
			}
			Height.Pixels += 6;
			Recalculate();
		}
	}
	public class LiquidDefinitionElement(int type) : UIElement() {
		internal Func<int> getSelectedLiquid;
		readonly Asset<Texture2D> texture = ModContent.Request<Texture2D>($"{nameof(WorldTwists)}/Textures/Liquid_{type}");
		public readonly int type = type;
		public override void OnInitialize() {
			Width.Set(26, 0);
			Height.Set(26, 0);
		}
		public override void Draw(SpriteBatch spriteBatch) {
			float inventoryScale = Main.inventoryScale;
			Rectangle dimensions = this.GetDimensions().ToRectangle();
			Color backColor = new(200, 200, 200);
			if (!PlayerInput.IgnoreMouseInterface && dimensions.Contains(Main.mouseX, Main.mouseY)) {
				backColor = Color.White;
			}
			bool selectedBack = getSelectedLiquid is not null && getSelectedLiquid() == type;
			spriteBatch.Draw(
				selectedBack ? TextureAssets.InventoryBack14.Value : TextureAssets.InventoryBack.Value,
				dimensions,
				backColor
			);
			dimensions.Inflate(-5, -5);
			spriteBatch.Draw(
				texture.Value,
				dimensions,
				backColor
			);
		}
	}
	
}