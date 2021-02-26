using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using Terraria.World.Generation;
using Tyfyter.Utils;
using Tyfyter.Utils.ID;
using static Tyfyter.Utils.StructureUtils.StructureTilePlacementType;

namespace WorldTwists {
	public class WorldTwists : Mod {
        internal static WorldTwists Instance;
        public override void Load() {
            if(Instance!=null) Logger.Info("WorldTwists Instance already loaded at Load()");
            Instance = this;
        }

        public override void Unload() {
            TwistWorld.pairings = null;
            if(Instance==null) Logger.Info("WorldTwists Instance already unloaded at Unload()");
            Instance = null;
        }
    }
    [Label("Settings")]
    public class TwistConfig : ModConfig {
        public static TwistConfig Instance;
        public override ConfigScope Mode => ConfigScope.ServerSide;
        [Header("Shuffled")]

        [Label("Shuffled Blocks")]
        [DefaultValue(true)]
        public bool Shuffled = true;

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
        /*[DefaultValue(new int[56]{
            0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0,0,0,0,0,
            0,0,0,0,0,0})]*/

        [Header("Rarity Invert")]

        [Label("Invert Blocks")]
        [DefaultValue(false)]
        public bool Inverted = false;

        [Header("Other Changes")]
        [Label("Liquid Cycling")]
        [DefaultValue(0)]
        [Tooltip("-1 = Water->Honey->Lava->Water, 1 = Water->Lava->Honey->Water")]
        [Range(-1, 1)]
        public int LiquidCycleInt {
            get => LiquidCycle;
            set { LiquidCycle = (sbyte)value; }
        }
        internal sbyte LiquidCycle = 0;
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
        [Label("Flipped World")]
        [DefaultValue(false)]
        public bool Flipped { get; set; } = false;
        [Label("Hardmode Start")]
        [DefaultValue(false)]
        public bool AlreadyHM { get; set; } = false;
        [Label("Hardmode Start Spawns WoF loot")]
        [DefaultValue(false)]
        public bool HMPyramid { get; set; } = false;

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

        [Header("Randomize/Shuffle")]
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
    }
    public class TwistWorld : ModWorld {
        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight) {

            if(TwistConfig.Instance.Shuffled) tasks.Add(new PassLegacy("Shuffle", ShuffledBlocks));
            else if(TwistConfig.Instance.Inverted) tasks.Add(new PassLegacy("Rarity Invert", Invert));
            else if(TwistConfig.Instance.Randomize) tasks.Add(new PassLegacy("Randomize", RandomizedBlocks));
            if(TwistConfig.Instance.LiquidCycle!=0) {
                int genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Settle Liquids Again"));
                tasks.Insert(genIndex, new PassLegacy("Cycle Liquids", LiquidCycle));
            }
            if(TwistConfig.Instance.Flipped) {
                tasks.Add(new PassLegacy("Flipping World", Flipper));
                tasks.Add(tasks[tasks.FindIndex(genpass => genpass.Name.Equals("Settle Liquids Again"))]);
            }
            if(TwistConfig.Instance.GreatEnsmallening) {
                tasks.Add(new PassLegacy("Mini Worlds", GreatEnsmallener));
            }
            if(TwistConfig.Instance.AlreadyHM) {
                if(!TwistConfig.Instance.HMPyramid)tasks.Add(new PassLegacy("Starting Hardmode", (p)=>WorldGen.StartHardmode()));
                else tasks.Add(new PassLegacy("Starting Hardmode and Placing loot", HMLooter));
            }
        }
        public static void LiquidCycle(GenerationProgress progress) {
            Tile tile;
            for(int y = 0; y < Main.maxTilesY; y++) {
                for(int x = 0; x < Main.maxTilesX; x++) {
                    tile = Main.tile[x, y];
                    tile.liquidType((tile.liquidType()+TwistConfig.Instance.LiquidCycle+3)%3);
                }
            }
        }
        public static void ShuffledBlocks(GenerationProgress progress) {
            WorldTwists.Instance.Logger.Info("Shuffling Blocks");
            progress.Message = "Shuffling Blocks";
            //Dictionary<int,int> count
            List<ushort> types = new List<ushort>() { };
            List<ushort> invalidTypes = new List<ushort>() { };
            if(!TwistConfig.Instance.RandomizeBoulders) {
                invalidTypes.Add(TileID.Boulder);
            }
            if(!TwistConfig.Instance.Sandomize) {
                for(ushort i = 0; i < TileID.Sets.Falling.Length; i++) {
                    if(TileID.Sets.Falling[i]) invalidTypes.Add(i);
                }
            }
            if(!TwistConfig.Instance.JankLoot) {
                invalidTypes.Add(TileID.ClosedDoor);
            }
            Tile tile;
            for(int y = 0; y < Main.maxTilesY; y++) {
                for(int x = 0; x < Main.maxTilesX; x++) {
                    tile = Main.tile[x, y];
                    if(!types.Contains(tile.type)) types.Add(tile.type);
                    if(!TwistExt.solid(tile.type)&&!invalidTypes.Contains(tile.type)) invalidTypes.Add(tile.type);
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
            bool UseWorldSeed = TwistConfig.Instance.UseWorldSeed;
            bool UseComplexSeed = TwistConfig.Instance.UseComplexSeed;
            WorldTwists.Instance.Logger.Info($"Shuffle: Using settings UseWorldSeed:{UseWorldSeed}; UseComplexSeed:{UseComplexSeed}");
            if(UseWorldSeed) {
                if(UseComplexSeed) {
                    WorldTwists.Instance.Logger.Info("Shuffle: inext "+typeof(UnifiedRandom).GetField("inext", BindingFlags.NonPublic|BindingFlags.Instance).GetValue(WorldGen.genRand));
                    WorldTwists.Instance.Logger.Info("Shuffle: inextp "+typeof(UnifiedRandom).GetField("inextp", BindingFlags.NonPublic|BindingFlags.Instance).GetValue(WorldGen.genRand));
                    WorldTwists.Instance.Logger.Info("Shuffle: SeedArray: "+string.Join(",", (int[])typeof(UnifiedRandom).GetField("SeedArray", BindingFlags.NonPublic|BindingFlags.Instance).GetValue(WorldGen.genRand)));
                }
                shuffledTypes.Shuffle(WorldGen.genRand);
            } else {
                UnifiedRandom rand = new UnifiedRandom(TwistConfig.Instance.RandomizeSeed);
                if(UseComplexSeed) {
                    typeof(UnifiedRandom).GetField("inext", BindingFlags.NonPublic|BindingFlags.Instance).SetValue(rand, TwistConfig.Instance.inext);
                    typeof(UnifiedRandom).GetField("inextp", BindingFlags.NonPublic|BindingFlags.Instance).SetValue(rand, TwistConfig.Instance.inextp);
                    typeof(UnifiedRandom).GetField("SeedArray", BindingFlags.NonPublic|BindingFlags.Instance).SetValue(rand, TwistConfig.Instance.SeedArray.ToArray());
                }
                shuffledTypes.Shuffle(rand);
            }
            pairings = new Dictionary<ushort, ushort>() { };
            string log = "";
            for(int i = 0; i < types.Count; i++) {
                pairings.Add(types[i], shuffledTypes[i]);
                log+=$"[{types[i]} ({getTileName(types[i])}),{shuffledTypes[i]} ({getTileName(shuffledTypes[i])})] ";
            }
            //TileID.Sets.Falling
            for(int i = 0; i < invalidTypes.Count; i++) {
                pairings.Add(invalidTypes[i], invalidTypes[i]);
            }
            WorldTwists.Instance.Logger.Info("Shuffle: Shuffled "+log);
            for(int y = 0; y < Main.maxTilesY; y++) {
                for(int x = 0; x < Main.maxTilesX; x++) {
                    try {
                        if(Main.tile[x, y].active()&&TwistExt.solid(Main.tile[x, y].type))
                            Main.tile[x, y].type = pairings[Main.tile[x, y].type];
                        if(Main.tileCut[Main.tile[x, y].type]&&Main.tile[x, y].type!=TileID.Pots&&Main.tile[x, y].type!=TileID.JunglePlants&&Main.tile[x, y].type!=TileID.Larva)
                            Main.tile[x, y].active(false);
                    } catch(Exception e) {
                        WorldTwists.Instance.Logger.Info("Shuffle: Ran into issue randomizing "+Main.tile[x, y].type);
                        throw e;
                    }
                }
            }
        }
        public static void RandomizedBlocks(GenerationProgress progress) {
            WorldTwists.Instance.Logger.Info("Randomizing Blocks");
            progress.Message = "Randomizing Blocks";
            //Dictionary<int,int> count
            List<ushort> types = new List<ushort>() { };
            List<ushort> invalidTypes = new List<ushort>() { };
            if(!TwistConfig.Instance.RandomizeBoulders) {
                invalidTypes.Add(TileID.Boulder);
            }
            if(!TwistConfig.Instance.Sandomize) {
                for(ushort i = 0; i < TileID.Sets.Falling.Length; i++) {
                    if(TileID.Sets.Falling[i]) invalidTypes.Add(i);
                }
            }
            if(!TwistConfig.Instance.JankLoot) {
                invalidTypes.Add(TileID.ClosedDoor);
            }
            Tile tile;
            for(int y = 0; y < Main.maxTilesY; y++) {
                for(int x = 0; x < Main.maxTilesX; x++) {
                    tile = Main.tile[x, y];
                    if(!types.Contains(tile.type)) types.Add(tile.type);
                    if(!TwistExt.solid(tile.type)&&!invalidTypes.Contains(tile.type)) invalidTypes.Add(tile.type);
                }
            }
            for(int i = 0; i < types.Count; i++) {
                if(invalidTypes.Contains(types[i])) {
                    types.RemoveAt(i);
                    i--;
                }
            }
            //WorldTwists.Instance.Logger.Info("Randomize: Shuffled "+WorldGen.genRand.);
            bool UseWorldSeed = TwistConfig.Instance.UseWorldSeed;
            bool UseComplexSeed = TwistConfig.Instance.UseComplexSeed;
            WorldTwists.Instance.Logger.Info($"Randomize: Using settings UseWorldSeed:{UseWorldSeed}; UseComplexSeed:{UseComplexSeed}");
            UnifiedRandom rand;
            if(UseWorldSeed) {
                rand = WorldGen.genRand;
                if(UseComplexSeed) {
                    WorldTwists.Instance.Logger.Info("Randomize: inext "+typeof(UnifiedRandom).GetField("inext", BindingFlags.NonPublic|BindingFlags.Instance).GetValue(WorldGen.genRand));
                    WorldTwists.Instance.Logger.Info("Randomize: inextp "+typeof(UnifiedRandom).GetField("inextp", BindingFlags.NonPublic|BindingFlags.Instance).GetValue(WorldGen.genRand));
                    WorldTwists.Instance.Logger.Info("Randomize: SeedArray: "+string.Join(",", (int[])typeof(UnifiedRandom).GetField("SeedArray", BindingFlags.NonPublic|BindingFlags.Instance).GetValue(WorldGen.genRand)));
                }
            } else {
                rand = new UnifiedRandom(TwistConfig.Instance.RandomizeSeed);
                if(UseComplexSeed) {
                    typeof(UnifiedRandom).GetField("inext", BindingFlags.NonPublic|BindingFlags.Instance).SetValue(rand, TwistConfig.Instance.inext);
                    typeof(UnifiedRandom).GetField("inextp", BindingFlags.NonPublic|BindingFlags.Instance).SetValue(rand, TwistConfig.Instance.inextp);
                    typeof(UnifiedRandom).GetField("SeedArray", BindingFlags.NonPublic|BindingFlags.Instance).SetValue(rand, TwistConfig.Instance.SeedArray.ToArray());
                }
            }
            for(int y = 0; y < Main.maxTilesY; y++) {
                for(int x = 0; x < Main.maxTilesX; x++) {
                    try {
                        tile = Main.tile[x, y];
                        if(Main.tile[x, y].active()&&types.Contains(tile.type))
                            tile.type = rand.Next(types);
                        if(Main.tileCut[Main.tile[x, y].type]&&Main.tile[x, y].type!=TileID.Pots&&Main.tile[x, y].type!=TileID.JunglePlants&&Main.tile[x, y].type!=TileID.Larva)
                            Main.tile[x, y].active(false);
                    } catch(Exception e) {
                        WorldTwists.Instance.Logger.Info("Randomize: Ran into issue randomizing "+Main.tile[x, y].type);
                        throw e;
                    }
                }
            }
        }
        static string getTileName(int type) {
            return type<TileID.Count ? TileID.Search.GetName(type) : TileLoader.GetTile(type).Name;
        }
        public static void Invert(GenerationProgress progress) {
            WorldTwists.Instance.Logger.Info("Inverting Blocks");
            progress.Message = "Inverting Blocks";
            //Dictionary<int,int> count
            Dictionary<ushort, int> types = new Dictionary<ushort, int>() { };
            List<ushort> invalidTypes = new List<ushort>() { };
            if(!TwistConfig.Instance.RandomizeBoulders) {
                invalidTypes.Add(TileID.Boulder);
                for(ushort i = 0; i < TileLoader.TileCount; i++) {
                    if(Main.tileFrameImportant[i])invalidTypes.Add(i);
                }
            }
            if(!TwistConfig.Instance.JankLoot) {
                invalidTypes.Add(TileID.ClosedDoor);
            }
            Tile tile;
            for(int y = 0; y < Main.maxTilesY; y++) {
                for(int x = 0; x < Main.maxTilesX; x++) {
                    tile = Main.tile[x, y];
                    if(!TwistExt.solid(tile.type)) {
                        if(!invalidTypes.Contains(tile.type)) invalidTypes.Add(tile.type);
                        continue;
                    }
                    if(invalidTypes.Contains(tile.type)) continue;
                    if(types.ContainsKey(tile.type)) {
                        types[tile.type]++;
                    } else {
                        types.Add(tile.type, 1);
                    }
                }
            }
            for(int i = 0; i < invalidTypes.Count; i++) {
                if(types.ContainsKey(invalidTypes[i])) {
                    types.Remove(invalidTypes[i]);
                    i--;
                }
            }
            List<KeyValuePair<ushort, int>> listTypes = types.ToList();
            listTypes.Sort((p1, p2) => p1.Value-p2.Value);
            pairings = new Dictionary<ushort, ushort>() { };
            string log = "";
            for(int i = 0; i < types.Count; i++) {
                pairings.Add(listTypes[i].Key, listTypes[types.Count-i-1].Key);
                log+=$"({listTypes[i]},{listTypes[types.Count-i-1]}) ";
            }
            for(int i = 0; i < invalidTypes.Count; i++) {
                pairings.Add(invalidTypes[i], invalidTypes[i]);
            }
            WorldTwists.Instance.Logger.Info("Invert: Inverted "+log);
            for(int y = 0; y < Main.maxTilesY; y++) {
                for(int x = 0; x < Main.maxTilesX; x++) {
                    try {
                        if(Main.tile[x, y].active()&&TwistExt.solid(Main.tile[x, y].type))
                            Main.tile[x, y].type = pairings[Main.tile[x, y].type];
                        if(Main.tileCut[Main.tile[x, y].type])
                            Main.tile[x, y].active(false);
                    } catch(Exception e) {
                        WorldTwists.Instance.Logger.Info("Randomize: Ran into issue randomizing "+Main.tile[x, y].type);
                        throw e;
                    }
                }
            }
        }
        public static void GreatEnsmallener(GenerationProgress progress) {
            progress.Message = "Ensmallening";
            int width = Main.maxTilesX;
            int height = Main.maxTilesY;
            int halfX = width/2;
            int halfY = height/2;
            int x, y;
            Tile[,] tiles = new Tile[Main.tile.GetLength(0),Main.tile.GetLength(1)];
            for(int y1 = 0; y1 < height; y1++) {
                for(int x1 = 0; x1 < width; x1++) {
                    x = x1/2+((x1%2)*halfX);
                    y = y1/2+((y1%2)*halfY);
                    tiles[x,y] = Main.tile[x1, y1];
                }
            }
            for(int y2 = 0; y2 < height; y2++) {
                for(int x2 = 0; x2 < width; x2++) {
                    Main.tile[x2, y2] = tiles[x2, y2];
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
                c.x = c.x/2+((c.x%2)*halfX);
                c.y = c.y/2+((c.y%2)*halfY);
                chestTile = Main.tile[c.x, c.y];
                c.y--;
                /*chestTiles = new Tile[3, 3];
                for(int cy = -1; cy < 2; cy++) {
                    for(int cx = -1; cx < 2; cx++) {
                        chestTiles[cx + 1, cy + 1] = Main.tile[c.x+cx, c.y+cy];
                    }
                }*/
                try {
                    MultiTileUtils.AggressivelyPlace(new Point(c.x, c.y), chestTile.type, chestTile.frameX / MultiTileUtils.GetStyleWidth(chestTile.type));
                } catch(Exception e) {
                    WorldTwists.Instance.Logger.Warn(e);
                    Exception _ = e;
                }
            }
            Main.spawnTileX = (Main.spawnTileX / 2) + WorldGen.genRand.Next(2) * halfX;
            Main.spawnTileY = (Main.spawnTileY / 2) + (WorldGen.genRand.Next(2) + TwistConfig.Instance.HipsterSpawnSkew>0 ? halfY : 0);
            Main.dungeonX = (Main.dungeonX / 2) + WorldGen.genRand.Next(2) * halfX;
            Main.dungeonY = (Main.dungeonY / 2);
            int npci;
            for(npci = 0;npci<Main.npc.Length;npci++){
	            if(Main.npc[npci].type==NPCID.OldMan){
		            break;
	            }
            }
            if(npci<201) {
                Main.npc[npci].position = new Vector2(Main.dungeonX,Main.dungeonY)*16;
            }
            for(npci = 0;npci<Main.npc.Length;npci++){
	            if(Main.npc[npci].type==NPCID.Guide){
		            break;
	            }
            }
            if(npci<201) {
                Main.npc[npci].position.X = Main.npc[npci].position.X + (WorldGen.genRand.Next(2) * halfX*16);
                Main.npc[npci].position.Y = Main.spawnTileY;
            }
            smol = true;
        }
        public static void Flipper(GenerationProgress progress) {
            progress.Message = "Flipping";
            int width = Main.maxTilesX;
            int height = Main.maxTilesY;
            int halfX = width/2;
            int halfY = height/2;
            int y;
            int debX = -1;
            Tile[,] tiles = new Tile[Main.tile.GetLength(0),Main.tile.GetLength(1)];
            for(int y1 = 0; y1 < height; y1++) {
                for(int x = 0; x < width; x++) {
                    y = Main.maxTilesY - y1;
                    tiles[x,y - 1] = Main.tile[x, y1];
                    if(x>debX)debX = x;
                }
                y = y1;
            }
            debX = -1;
            for(int y2 = 0; y2 < height; y2++) {
                for(int x2 = 0; x2 < width; x2++) {
                    Main.tile[x2, y2] = tiles[x2, y2];
                    if(x2>debX)debX = x2;
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
                    MultiTileUtils.AggressivelyPlace(new Point(c.x, c.y), chestTile.type, chestTile.frameX / MultiTileUtils.GetStyleWidth(chestTile.type));
                } catch(Exception e) {
                    WorldTwists.Instance.Logger.Warn(e);
                    Exception _ = e;
                }
            }
            int found = 0;
            for(y = 0; y < height; y++) {
                Main.spawnTileY = y - 1;
                if(Main.tileSolid[Main.tile[Main.spawnTileX, y].type]) {
                    if(found>2)break;
                    found = 0;
                } else {
                    found++;
                }
            }
            //Main.spawnTileX = (Main.spawnTileX / 2) + WorldGen.genRand.Next(2) * halfX;
            //Main.spawnTileY = Main.maxTilesY - Main.spawnTileY;//(Main.spawnTileY / 2) + (WorldGen.genRand.Next(2) + TwistConfig.Instance.HipsterSpawnSkew>0 ? halfY : 0);
            //Main.dungeonX = (Main.dungeonX / 2) + WorldGen.genRand.Next(2) * halfX;
            //Main.dungeonY = (Main.dungeonY / 2);
            int npci;
            for(npci = 0;npci<Main.npc.Length;npci++){
	            if(Main.npc[npci].type==NPCID.OldMan){
		            break;
	            }
            }
            if(npci<201) {
                Main.npc[npci].position = new Vector2(Main.dungeonX,Main.dungeonY)*16;
            }
            for(npci = 0;npci<Main.npc.Length;npci++){
	            if(Main.npc[npci].type==NPCID.Guide){
		            break;
	            }
            }
            if(npci<201) {
                Main.npc[npci].position.Y = Main.spawnTileY;
            }
        }
        public static void HMLooter(GenerationProgress progress) {
            progress.Message = "Starting Hardmode";
            Item[] items = Main.item;
            Main.item = new Item[Main.maxItems+1].Select((v)=>new Item()).ToArray();

            NPC npc = Main.npc[NPC.NewNPC(16*16,16*16,NPCID.WallofFlesh)];
            npc.NPCLoot();
            npc.active = false;

            Item[] loot = Main.item.Where((i)=>!i.IsAir).ToArray();
            Main.item = items;

			int centerI = (int)(npc.position.X + (npc.width / 2)) / 16;
			int centerJ = (int)(npc.position.Y + (npc.height / 2)) / 16;
			int size = npc.width / 2 / 16 + 1;
			for (int i = centerI - size; i <= centerI + size; i++) {
				for (int j = centerJ - size; j <= centerJ + size; j++) {
					if ((i == centerI - size || i == centerI + size || j == centerJ - size || j == centerJ + size) && (Main.tile[i, j].type == 347 || Main.tile[i, j].type == 140)) {
						Main.tile[i, j].active(active: false);
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
            new string[]{
            "   _____l_c__l_____   ",
            "  ____qbbbbbbbbp____  ",
            " ___qbbbbbbbbbbbbp___ ",
            "__qbbbbbbbbbbbbbbbbp__",
            "qbbbbbbbbbbbbbbbbbbbbp"
            },
            new Dictionary<char, StructureUtils.StructureTile>(){
            {'b',new StructureUtils.StructureTile(brickType, ReplaceOld)},
            {'q',new StructureUtils.StructureTile(brickType, ReplaceOld, SlopeID.BottomRight)},
            {'p',new StructureUtils.StructureTile(brickType, ReplaceOld, SlopeID.BottomLeft)},
            {'c',new StructureUtils.StructureTile(TileID.Containers, RequiredTile|MultiTile, 0, 44)},
            {'l',new StructureUtils.StructureTile(TileID.Lamps, RequiredTile|MultiTile, 0, 23)},
            {'_',new StructureUtils.StructureTile(0, OptionalTile|Deactivate)},
            {' ',new StructureUtils.StructureTile(0, Nothing)}
            }
            );
            StructureUtils.Structure pillar = new StructureUtils.Structure(
            new string[]{"bbbbbbbbbbbbbbbbbbbbbb"},
            new Dictionary<char, StructureUtils.StructureTile>(){{'b',new StructureUtils.StructureTile(brickType, OptionalTile)}}
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
                StartBag bag = chest.item[39].modItem as StartBag;
                MethodInfo addItem = typeof(StartBag).GetMethod("AddItem", BindingFlags.Instance|BindingFlags.NonPublic);
                int i = 0;
                for(; i < 39; i++) {
                    chest.item[i] = loot[i];
                }
                for(; i < loot.Length; i++) {
                    addItem.Invoke(bag, new object[]{loot[i]});
                }
            }
        }
        public bool _smol = false;
        public static bool smol {
            get => ModContent.GetInstance<TwistWorld>()._smol;
            set => ModContent.GetInstance<TwistWorld>()._smol = value;
        }
        private Dictionary<ushort, ushort> _pairings;
        public static Dictionary<ushort, ushort> pairings {
            get => ModContent.GetInstance<TwistWorld>()._pairings;
            set => ModContent.GetInstance<TwistWorld>()._pairings = value;
        }
        public override TagCompound Save() {
            TagCompound tag = new TagCompound();
            try {
                tag.Add("smol", smol);
                if(pairings.Count>0) {
                    tag.Add("pairingKeys", pairings.Keys.ToList());
                    tag.Add("pairingValues", pairings.Values.ToList());
                }
            } catch(Exception) {}
            return tag;
        }
        public override void Load(TagCompound tag) {
            try {
                if(tag.ContainsKey("smol"))smol = tag.GetBool("smol");
                pairings = new Dictionary<ushort, ushort>() { };
                if(!tag.ContainsKey("pairingKeys")||!tag.ContainsKey("pairingValues"))return;
                List<ushort> keys = tag.Get<List<ushort>>("pairingKeys");
                List<ushort> values = tag.Get<List<ushort>>("pairingValues");
                pairings = keys.Zip(values, (k, v) => new { Key = k, Value = v }).ToDictionary(x => x.Key, x => x.Value);
            } catch(Exception) {}
        }
        public override void TileCountsAvailable(int[] tileCounts) {
            if(TwistConfig.Instance.KeepDungeon) {
                try {
                    addTilePairing(ref Main.dungeonTiles, TileID.BlueDungeonBrick, tileCounts);
                    addTilePairing(ref Main.dungeonTiles, TileID.GreenDungeonBrick, tileCounts);
                    addTilePairing(ref Main.dungeonTiles, TileID.PinkDungeonBrick, tileCounts);
                    addTilePairing(ref Main.evilTiles, TileID.Ebonstone, tileCounts);
                    addTilePairing(ref Main.bloodTiles, TileID.Crimstone, tileCounts);
                    addTilePairing(ref Main.jungleTiles, TileID.JungleGrass, tileCounts);
                    addTilePairing(ref Main.shroomTiles, TileID.MushroomGrass, tileCounts);
                    addTilePairing(ref Main.snowTiles, TileID.IceBlock, tileCounts);
                } catch(Exception) { }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void addTilePairing(ref int count, ushort id, int[] tileCounts) {
            if(pairings.ContainsKey(id)) count+=tileCounts[pairings[id]];
        }
        private bool oldDayTime;
        public override void PostUpdate() {
            if(smol&&Main.dayTime!=oldDayTime&&!NPC.downedBoss3&&NPC.CountNPCS(NPCID.OldMan)<1) {
                NPC.NewNPC(Main.dungeonX*16,Main.dungeonY*16,NPCID.OldMan);
            }
            oldDayTime = Main.dayTime;
        }
    }
    public static class TwistExt {
        public static void Shuffle<T>(this IList<T> list, UnifiedRandom rng) {
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        public static bool solid(int type) {
            return Main.tileSolid[type]&&(TwistConfig.Instance.RandomizePlatforms||!Main.tileSolidTop[type]);
        }
    }
}