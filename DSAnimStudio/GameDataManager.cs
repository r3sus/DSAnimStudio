﻿using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSAnimStudio
{
    public static class GameDataManager
    {
        public enum GameTypes
        {
            None,
            DES,
            DS1,
            DS1R,
            DS2,
            DS2SOTFS,
            DS3,
            BB,
            SDT,
        }

        public static readonly Dictionary<GameTypes, string> GameTypeNames =
            new Dictionary<GameTypes, string>
        {
            { GameTypes.None, "<NONE>" },
            { GameTypes.DES, "Demon's Souls" },
            { GameTypes.DS1, "Dark Souls: Prepare to Die Edition" },
            { GameTypes.DS1R, "Dark Souls Remastered" },
            { GameTypes.DS2, "Dark Souls II" },
            { GameTypes.DS2SOTFS, "Dark Souls II: Scholar of the First Sin" },
            { GameTypes.DS3, "Dark Souls III" },
            { GameTypes.BB, "Bloodborne" },
            { GameTypes.SDT, "Sekiro: Shadows Die Twice" },
        };

        public static bool CheckGameTypeParamIDCompatibility(GameTypes a, GameTypes b)
        {
            if (a == GameTypes.DS1 && b == GameTypes.DS1R)
                return true;
            else if (a == GameTypes.DS1R && b == GameTypes.DS1)
                return true;
            // TODO: Check if these DS2 ones would be a good idea with 
            // Forlorn set only being in sotfs etc.
            //if (a == GameTypes.DS2 && b == GameTypes.DS2SOTFS)
            //    return true;
            //if (a == GameTypes.DS2SOTFS && b == GameTypes.DS2)
            //    return true;
            else
                return a == b;
        }

        private static GameTypes lastGameType = GameTypes.None;
        public static GameTypes GameType { get; private set; } = GameTypes.None;

        public static bool GameTypeHasLongAnimIDs =>
            !(GameType == GameTypes.DS1 || GameType == GameTypes.DS1R || GameType == GameTypes.DES);

        public static HKX.HKXVariation GetCurrentLegacyHKXType()
        {
            if (GameType == GameTypes.DES)
                return HKX.HKXVariation.HKXDeS;
            else if (GameType == GameTypes.DS1)
                return HKX.HKXVariation.HKXDS1;
            else if (GameType == GameTypes.DS3)
                return HKX.HKXVariation.HKXDS3;
            else if (GameType == GameTypes.BB)
                return HKX.HKXVariation.HKXBloodBorne;

            // TODO MAKE LESS SHIT
            return HKX.HKXVariation.HKXDS3;
        }

        public static string InterrootPath { get; private set; } = null;

        public static void Init(GameTypes gameType, string interroot)
        {
            GameType = gameType;
            InterrootPath = interroot;
            if (gameType != lastGameType)
            {
                ParamManager.LoadParamBND(forceReload: false);
                FmgManager.LoadAllFMG(forceReload: false);
                LoadSystex();
            }
            lastGameType = GameType;
        }

        public static void ReloadAllData()
        {
            ReloadParams();
            ReloadFmgs();
        }

        public static void ReloadParams()
        {
            ParamManager.LoadParamBND(forceReload: true);
        }

        public static void ReloadFmgs()
        {
            FmgManager.LoadAllFMG(forceReload: true);
        }

        public static void LoadSystex()
        {
            LoadingTaskMan.DoLoadingTask("LoadSystex", "Loading SYSTEX textures...", progress =>
            {
                if (GameType == GameTypes.DS1)
                {
                    TexturePool.AddTpfsFromPaths(new List<string>
                    {
                        $@"{InterrootPath}\other\SYSTEX_TEX.tpf",
                        $@"{InterrootPath}\other\envlight.tpf",
                        $@"{InterrootPath}\other\lensflare.tpf",
                    }, progress);
                }
                else if (GameType == GameTypes.DS1R)
                {
                    TexturePool.AddTpfsFromPaths(new List<string>
                    {
                        $@"{InterrootPath}\other\SYSTEX_TEX.tpf.dcx",
                        $@"{InterrootPath}\other\envlight.tpf.dcx",
                        $@"{InterrootPath}\other\lensflare.tpf.dcx",
                    }, progress);
                }
                else if (GameType == GameTypes.DS3)
                {
                    TexturePool.AddTpfsFromPaths(new List<string>
                    {
                        $@"{InterrootPath}\other\systex.tpf.dcx",
                        $@"{InterrootPath}\other\bloodtex.tpf.dcx",
                        $@"{InterrootPath}\other\decaltex.tpf.dcx",
                        $@"{InterrootPath}\other\sysenvtex.tpf.dcx",
                    }, progress);
                }
                else if (GameType == GameTypes.BB)
                {
                    // TODO: completely confirm these because I just
                    // copied them from a BB network test file list.
                    TexturePool.AddTpfsFromPaths(new List<string>
                    {
                        $@"{InterrootPath}\other\SYSTEX.tpf.dcx",
                        $@"{InterrootPath}\other\decalTex.tpf.dcx",
                        $@"{InterrootPath}\other\bloodTex.tpf.dcx",
                    }, progress);
                }
                else if (GameType == GameTypes.SDT)
                {
                    TexturePool.AddTpfsFromPaths(new List<string>
                    {
                        $@"{InterrootPath}\other\systex.tpf.dcx",
                        $@"{InterrootPath}\other\maptex.tpf.dcx",
                        $@"{InterrootPath}\other\decaltex.tpf.dcx",
                    }, progress);
                }
            });

           
        }

        public static Model LoadObject(string id)
        {
            Model obj = null;

            LoadingTaskMan.DoLoadingTaskSynchronous($"LOAD_OBJ_{id}", $"Loading object {id}...", progress =>
            {
                if (GameType == GameTypes.DS3)
                {
                    var chrbnd = BND4.Read($@"{InterrootPath}\obj\{id}.objbnd.dcx");

                    obj = new Model(progress, id, chrbnd, 0, null, null);
                }
                else if (GameType == GameTypes.DS1)
                {
                    var chrbnd = BND3.Read($@"{InterrootPath}\obj\{id}.objbnd");

                    obj = new Model(progress, id, chrbnd, 0, null, null);
                }
                else if (GameType == GameTypes.DS1R)
                {
                    var chrbnd = BND4.Read($@"{InterrootPath}\obj\{id}.objbnd.dcx");

                    obj = new Model(progress, id, chrbnd, 0, null, null);
                }
                else if (GameType == GameTypes.BB)
                {
                    var chrbnd = BND4.Read($@"{InterrootPath}\obj\{id}.objbnd.dcx");

                    obj = new Model(progress, id, chrbnd, 0, null, null);
                }

                Scene.AddModel(obj);

                var texturesToLoad = obj.MainMesh.GetAllTexNamesToLoad();

                LoadingTaskMan.DoLoadingTask($"LOAD_OBJ_{id}_TEX",
                    "Loading additional object textures...", innerProgress =>
                {
                    if (GameType == GameTypes.DS1)
                    {
                        foreach (var tex in texturesToLoad)
                        {
                            TexturePool.AddTpfFromPath($@"{InterrootPath}\map\tx\{tex}.tpf");
                        }
                    }
                    else if (GameType == GameTypes.DS3)
                    {
                        int objGroup = int.Parse(id.Substring(1)) / 1_0000;
                        var tpfBnds = System.IO.Directory.GetFiles($@"{InterrootPath}\map\m{objGroup:D2}", "*.tpfbhd");
                        foreach (var t in tpfBnds)
                            TexturePool.AddSpecificTexturesFromBXF4(t, texturesToLoad);
                    }
                    obj.MainMesh.TextureReloadQueued = true;
                });
            });

            return obj;

        }

        public static Model LoadCharacter(string id)
        {
            Model chr = null;

            LoadingTaskMan.DoLoadingTaskSynchronous($"LOAD_CHR_{id}", $"Loading character {id}...", progress =>
            {
                if (GameType == GameTypes.DS3)
                {
                    var chrbnd = BND4.Read($@"{InterrootPath}\chr\{id}.chrbnd.dcx");
                    var texbnd = BND4.Read($@"{InterrootPath}\chr\{id}.texbnd.dcx");
                    var anibnd = BND4.Read($@"{InterrootPath}\chr\{id}.anibnd.dcx");

                    chr = new Model(progress, id, chrbnd, 0, anibnd, texbnd, ignoreStaticTransforms: true);
                }
                else if (GameType == GameTypes.DS1)
                {
                    var chrbnd = BND3.Read($@"{InterrootPath}\chr\{id}.chrbnd");
                    var anibnd = BND3.Read($@"{InterrootPath}\chr\{id}.anibnd");

                    chr = new Model(progress, id, chrbnd, 0, anibnd, texbnd: null,
                        possibleLooseDdsFolder: $@"{InterrootPath}\chr\{id}\", ignoreStaticTransforms: true);
                }
                else if (GameType == GameTypes.DS1R)
                {
                    var chrbnd = BND3.Read($@"{InterrootPath}\chr\{id}.chrbnd.dcx");
                    var anibnd = BND3.Read($@"{InterrootPath}\chr\{id}.anibnd.dcx");

                    chr = new Model(progress, id, chrbnd, 0, anibnd, texbnd: null,
                        possibleLooseDdsFolder: $@"{InterrootPath}\chr\{id}\", ignoreStaticTransforms: true);
                }
                else if (GameType == GameTypes.BB)
                {
                    var chrbnd = BND4.Read($@"{InterrootPath}\chr\{id}.chrbnd.dcx");
                    var anibnd = BND4.Read($@"{InterrootPath}\chr\{id}.anibnd.dcx");

                    chr = new Model(progress, id, chrbnd, 0, anibnd, texbnd: null,
                        additionalTpfNames: new List<string> { $@"{InterrootPath}\chr\{id}_2.tpf.dcx" },
                        possibleLooseDdsFolder: $@"{InterrootPath}\chr\{id}\", ignoreStaticTransforms: true);
                }

                Scene.AddModel(chr);
            });

            if (id == "c0000")
            {
                chr.IS_PLAYER = true;

                LoadingTaskMan.DoLoadingTask("c0000_ANIBNDs", 
                    "Loading additional player ANIBNDs...", progress =>
                {
                    string[] anibnds = System.IO.Directory.GetFiles($@"{InterrootPath}\chr", 
                        GameType == GameTypes.DS1 ? "c0000_*.anibnd" : "c0000_*.anibnd.dcx")
                    .OrderBy(fn =>
                    {
                        var fnCheck = fn.ToLower();
                        if (fnCheck.Contains("lo"))
                            return 0;
                        else if (fnCheck.Contains("md"))
                            return 1;
                        else if (fnCheck.Contains("hi"))
                            return 2;
                        else
                            return 3;
                    }).ToArray();

                    for (int i = 0; i < anibnds.Length; i++)
                    {
                        IBinder anibnd = null;
                        if (BND3.Is(anibnds[i]))
                            anibnd = BND3.Read(anibnds[i]);
                        else
                            anibnd = BND4.Read(anibnds[i]);

                        chr.AnimContainer.LoadAdditionalANIBND(anibnd, null);

                        progress.Report(1.0 * i / anibnds.Length);
                    }

                    progress.Report(1);
                });
            }

            return chr;
            
        }
    }
}
