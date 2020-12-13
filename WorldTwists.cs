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
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using Terraria.World.Generation;

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

        [Label("Complex seed SeedArray")]
        public List<int> SeedArrayList {
            get => SeedArray;
            set {
                if(value.Count>56) {
                    value.RemoveRange(56, value.Count-56);
                } else for(; value.Count<56; value.Add(0)) ;
                SeedArray = value;
            }
        }
        internal List<int> SeedArray = new List<int>(56);
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

        [Header("Minor Changes")]
        [Label("Liquid Cycling")]
        [DefaultValue(0)]
        [Tooltip("-1 = Water->Honey->Lava->Water, 1 = Water->Lava->Honey->Water")]
        [Range(-1, 1)]
        public int LiquidCycleInt {
            get => LiquidCycle;
            set { LiquidCycle = (sbyte)value; }
        }
        internal sbyte LiquidCycle = 0;

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
    }
    public class TwistWorld : ModWorld {
        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight) {

            if(TwistConfig.Instance.Shuffled) tasks.Add(new PassLegacy("Randomize", ShuffledBlocks));
            else if(TwistConfig.Instance.Inverted) tasks.Add(new PassLegacy("Rarity Invert", Invert));
            else if(TwistConfig.Instance.Inverted) tasks.Add(new PassLegacy("Rarity Invert", Invert));
            if(TwistConfig.Instance.LiquidCycle!=0) {
                //int genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Settle Liquids"));
                int genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Settle Liquids Again"));
                tasks.Insert(genIndex, new PassLegacy("Cycle Liquids", LiquidCycle));
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
        private Dictionary<ushort, ushort> _pairings;
        public static Dictionary<ushort, ushort> pairings {
            get => ModContent.GetInstance<TwistWorld>()._pairings;
            set => ModContent.GetInstance<TwistWorld>()._pairings = value;
        }
        public override TagCompound Save() {
            TagCompound tag = new TagCompound();
            try {
                if(pairings.Count>0) {
                    tag.Add("pairingKeys", pairings.Keys.ToList());
                    tag.Add("pairingValues", pairings.Values.ToList());
                }
            } catch(Exception) {}
            return tag;
        }
        public override void Load(TagCompound tag) {
            try {
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