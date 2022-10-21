﻿using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSAnimStudio
{
    public static class ParamManager
    {
        private static Dictionary<SoulsAssetPipeline.SoulsGames, IBinder> ParamBNDs 
            = new Dictionary<SoulsAssetPipeline.SoulsGames, IBinder>();

        private static Dictionary<SoulsAssetPipeline.SoulsGames, Dictionary<string, PARAM_Hack>> LoadedParams 
            = new Dictionary<SoulsAssetPipeline.SoulsGames, Dictionary<string, PARAM_Hack>>();

        public static Dictionary<long, ParamData.BehaviorParam> BehaviorParam_PC 
            = new Dictionary<long, ParamData.BehaviorParam>();

        public static Dictionary<long, ParamData.BehaviorParam> BehaviorParam 
            = new Dictionary<long, ParamData.BehaviorParam>();

        public static Dictionary<long, ParamData.AtkParam> AtkParam_Pc 
            = new Dictionary<long, ParamData.AtkParam>();

        public static Dictionary<long, ParamData.AtkParam> AtkParam_Npc 
            = new Dictionary<long, ParamData.AtkParam>();

        public static Dictionary<long, ParamData.NpcParam> NpcParam 
            = new Dictionary<long, ParamData.NpcParam>();

        public static Dictionary<long, ParamData.SpEffectParam> SpEffectParam
            = new Dictionary<long, ParamData.SpEffectParam>();

        public static Dictionary<long, ParamData.EquipParamWeapon> EquipParamWeapon 
            = new Dictionary<long, ParamData.EquipParamWeapon>();

        public static Dictionary<long, ParamData.EquipParamProtector> EquipParamProtector
            = new Dictionary<long, ParamData.EquipParamProtector>();

        public static Dictionary<long, ParamData.WepAbsorpPosParam> WepAbsorpPosParam
           = new Dictionary<long, ParamData.WepAbsorpPosParam>();

        public static Dictionary<long, ParamDataDS2.WeaponParam> DS2WeaponParam
            = new Dictionary<long, ParamDataDS2.WeaponParam>();

        public static Dictionary<long, ParamDataDS2.ArmorParam> DS2ArmorParam
             = new Dictionary<long, ParamDataDS2.ArmorParam>();



        public static List<long> HitMtrlParamEntries = new List<long>();

        private static SoulsAssetPipeline.SoulsGames GameTypeCurrentLoadedParamsAreFrom = SoulsAssetPipeline.SoulsGames.None;

        public static PARAM_Hack GetParam(string paramName)
        {
            if (!ParamBNDs.ContainsKey(GameDataManager.GameType))
                throw new InvalidOperationException("ParamBND not loaded :tremblecat:");

            if (!LoadedParams.ContainsKey(GameDataManager.GameType))
                LoadedParams.Add(GameDataManager.GameType, new Dictionary<string, PARAM_Hack>());

            if (LoadedParams[GameDataManager.GameType].ContainsKey(paramName))
            {
                return LoadedParams[GameDataManager.GameType][paramName];
            }
            else
            {
                foreach (var f in ParamBNDs[GameDataManager.GameType].Files)
                {
                    if (f.Name.ToUpper().Contains(paramName.ToUpper()))
                    {
                        var p = PARAM_Hack.Read(f.Bytes);
                        LoadedParams[GameDataManager.GameType].Add(paramName, p);
                        return p;
                    }
                }
            }
            throw new InvalidOperationException($"Param '{paramName}' not found :tremblecat:");
        }

        private static bool CheckNpcParamForCurrentGameType(int chrId, ParamData.NpcParam r, bool isFirst, bool matchCXXX0)
        {
            long checkId = r.ID;

            if (matchCXXX0)
            {
                chrId /= 10;
                checkId /= 10;
            }

            if (GameDataManager.GameType != SoulsAssetPipeline.SoulsGames.SDT)
            {
                if ((checkId / 100) == chrId)
                {
                    return true;
                }
                else if (isFirst && GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.BB)
                {
                    return ((checkId % 1_0000_00) / 100 == chrId);
                }
                else
                {
                    return false;
                }
            }
            else if (GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.SDT)
            {
                return (checkId / 1_0000) == chrId;
            }
            else
            {
                throw new NotImplementedException(
                    $"ParamManager.CheckNpcParamForCurrentGameType not implemented for game type {GameDataManager.GameType}");
            }
        }

        public static List<ParamData.NpcParam> FindNpcParams(string modelName, bool matchCXXX0 = false)
        {
            int chrId = int.Parse(modelName.Substring(1));

            var npcParams = new List<ParamData.NpcParam>();
            foreach (var kvp in NpcParam.Where(r 
                => CheckNpcParamForCurrentGameType(chrId, r.Value, npcParams.Count == 0, matchCXXX0)))
            {
                if (!npcParams.Contains(kvp.Value))
                npcParams.Add(kvp.Value);
            }
            npcParams = npcParams.OrderBy(x => x.ID).ToList();
            return npcParams;
        }
        
        private static void LoadStuffFromParamBND(bool isDS2)
        {
            void AddParam<T>(Dictionary<long, T> paramDict, string paramName)
                where T : ParamData, new()
            {
                paramDict.Clear();
                var param = GetParam(paramName);
                foreach (var row in param.Rows)
                {
                    var rowData = new T();
                    rowData.ID = row.ID;
                    rowData.Name = row.Name;
                    try
                    {
                        rowData.Read(param.GetRowReader(row));
                        if (!paramDict.ContainsKey(row.ID))
                            paramDict.Add(row.ID, rowData);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to read row {row.ID} ({row.Name ?? "<No Name>"}) of param '{paramName}': {ex.ToString()}");
                    }
                    
                }
            }

            BehaviorParam.Clear();
            BehaviorParam_PC.Clear();
            AtkParam_Pc.Clear();
            AtkParam_Npc.Clear();
            NpcParam.Clear();
            EquipParamWeapon.Clear();
            EquipParamProtector.Clear();
            WepAbsorpPosParam.Clear();
            SpEffectParam.Clear();

            HitMtrlParamEntries.Clear();

            DS2WeaponParam.Clear();
            DS2ArmorParam.Clear();

            if (isDS2)
            {
                AddParam(DS2WeaponParam, "WeaponParam");
                AddParam(DS2ArmorParam, "ArmorParam");
            }
            else
            {
                AddParam(BehaviorParam, "BehaviorParam");
                if (GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.DES)
                    BehaviorParam_PC = BehaviorParam.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                else
                    AddParam(BehaviorParam_PC, "BehaviorParam_PC");
                AddParam(AtkParam_Pc, "AtkParam_Pc");
                AddParam(AtkParam_Npc, "AtkParam_Npc");
                AddParam(NpcParam, "NpcParam");
                AddParam(EquipParamWeapon, "EquipParamWeapon");
                AddParam(EquipParamProtector, "EquipParamProtector");
                if (GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.DS3)
                    AddParam(WepAbsorpPosParam, "WepAbsorpPosParam");
                AddParam(SpEffectParam, "SpEffectParam");
                if (GameDataManager.GameType != SoulsAssetPipeline.SoulsGames.DES)
                {
                    var hitMtrlParam = GetParam("HitMtrlParam");
                    foreach (var row in hitMtrlParam.Rows)
                    {
                        if (!HitMtrlParamEntries.Contains(row.ID))
                            HitMtrlParamEntries.Add(row.ID);
                    }
                    HitMtrlParamEntries = HitMtrlParamEntries.OrderBy(x => x).ToList();
                }
            }

            GameTypeCurrentLoadedParamsAreFrom = GameDataManager.GameType;
        }

        public static ParamData.AtkParam GetPlayerCommonAttack(int absoluteBehaviorID)
        {
            if (!BehaviorParam_PC.ContainsKey(absoluteBehaviorID))
                return null;

            var behaviorParamEntry = BehaviorParam_PC[absoluteBehaviorID];

            if (behaviorParamEntry.RefType != 0)
                return null;

            if (!AtkParam_Pc.ContainsKey(behaviorParamEntry.RefID))
                return null;

            return AtkParam_Pc[behaviorParamEntry.RefID];
        }

        public static ParamData.AtkParam GetPlayerBasicAtkParam(ParamData.EquipParamWeapon wpn, int behaviorJudgeID, bool isLeftHand)
        {
            if (wpn == null)
                return null;

            // Format: 10VVVVJJJ
            // V = BehaviorVariationID
            // J = BehaviorJudgeID
            long behaviorParamID = 10_0000_000 + (wpn.BehaviorVariationID * 1_000) + behaviorJudgeID;

            // If behavior 10VVVVJJJ doesn't exist, check for fallback behavior 10VV00JJJ.
            if (!BehaviorParam_PC.ContainsKey(behaviorParamID))
            {
                long baseBehaviorVariationID = (wpn.BehaviorVariationID / 100) * 100;

                if (baseBehaviorVariationID == wpn.BehaviorVariationID)
                {
                    // Fallback is just the same thing, which we already know doesn't exist.
                    return null;
                }

                long baseBehaviorParamID = 10_0000_000 + (baseBehaviorVariationID * 1_000) + behaviorJudgeID;

                if (BehaviorParam_PC.ContainsKey(baseBehaviorParamID))
                {
                    behaviorParamID = baseBehaviorParamID;
                }
                else
                {
                    return null;
                }
            }

            ParamData.BehaviorParam behaviorParamEntry = BehaviorParam_PC[behaviorParamID];

            // Make sure behavior is an attack behavior.
            if (behaviorParamEntry.RefType != 0)
                return null;

            // Make sure referenced attack exists.
            if (!AtkParam_Pc.ContainsKey(behaviorParamEntry.RefID))
                return null;

            return AtkParam_Pc[behaviorParamEntry.RefID];
        }

        public static ParamData.AtkParam GetNpcBasicAtkParam(ParamData.NpcParam npcParam, int behaviorJudgeID)
        {
            if (npcParam == null)
                return null;

            // Format: 2VVVVVJJJ
            // V = BehaviorVariationID
            // J = BehaviorJudgeID
            long behaviorParamID = 2_00000_000 + (npcParam.BehaviorVariationID * 1_000) + behaviorJudgeID;

            if (!BehaviorParam.ContainsKey(behaviorParamID))
                return null;

            ParamData.BehaviorParam behaviorParamEntry = BehaviorParam[behaviorParamID];

            // Make sure behavior is an attack behavior.
            if (behaviorParamEntry.RefType != 0)
                return null;

            // Make sure referenced attack exists.
            if (!AtkParam_Npc.ContainsKey(behaviorParamEntry.RefID))
                return null;

            return AtkParam_Npc[behaviorParamEntry.RefID];
        }

        public static bool LoadParamBND(bool forceReload)
        {
            string interroot = GameDataManager.InterrootPath;

            bool justNowLoadedParamBND = false;

            if (forceReload)
            {
                ParamBNDs.Clear();
                LoadedParams.Clear();

                BehaviorParam?.Clear();
                BehaviorParam_PC?.Clear();
                AtkParam_Pc?.Clear();
                AtkParam_Npc?.Clear();
                NpcParam?.Clear();
                EquipParamWeapon?.Clear();
                EquipParamProtector?.Clear();
                WepAbsorpPosParam?.Clear();
            }

            if (forceReload || !ParamBNDs.ContainsKey(GameDataManager.GameType))
            {
                ParamBNDs.Add(GameDataManager.GameType, null);

                if (GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.DS1)
                {
                    bool r1 = false;
                    foreach (var ext in new[] { "", ".dcx" })
                    {
                        var p1 = $"{interroot}\\param\\GameParam\\GameParam.parambnd" + ext;
                        if (r1 = File.Exists(p1))
                        {
                            ParamBNDs[GameDataManager.GameType] = BND3.Read(p1);
                            break;
                        }
                    }
                    if (!r1) return false;
                }
                else if (GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.DS1R)
                {
                    if (Directory.Exists($"{interroot}\\param\\GameParam\\") && File.Exists($"{interroot}\\param\\GameParam\\GameParam.parambnd.dcx"))
                        ParamBNDs[GameDataManager.GameType] = BND3.Read($"{interroot}\\param\\GameParam\\GameParam.parambnd.dcx");
                    else
                        return false;
                }
                else if (GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.DES)
                {
                    if (Directory.Exists($"{interroot}\\param\\GameParam\\") && File.Exists($"{interroot}\\param\\GameParam\\GameParamNA.parambnd.dcx"))
                        ParamBNDs[GameDataManager.GameType] = BND3.Read($"{interroot}\\param\\GameParam\\GameParamNA.parambnd.dcx");
                    else if (Directory.Exists($"{interroot}\\param\\GameParam\\") && File.Exists($"{interroot}\\param\\GameParam\\GameParam.parambnd.dcx"))
                        ParamBNDs[GameDataManager.GameType] = BND3.Read($"{interroot}\\param\\GameParam\\GameParam.parambnd.dcx");
                    else
                        return false;
                }
                else if (GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.BB || 
                    GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.SDT)
                {
                    if (Directory.Exists($"{interroot}\\param\\GameParam\\") && File.Exists($"{interroot}\\param\\GameParam\\GameParam.parambnd.dcx"))
                        ParamBNDs[GameDataManager.GameType] = BND4.Read($"{interroot}\\param\\GameParam\\GameParam.parambnd.dcx");
                    else if (Directory.Exists($"{interroot}\\..\\param\\GameParam\\") && File.Exists($"{interroot}\\..\\param\\GameParam\\GameParam.parambnd.dcx"))
                        ParamBNDs[GameDataManager.GameType] = BND4.Read($"{interroot}\\..\\param\\GameParam\\GameParam.parambnd.dcx");
                    else
                        return false;
                }
                else if (GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.DS3)
                {
                    if (Directory.Exists($"{interroot}\\param\\GameParam\\") &&
                        File.Exists($"{interroot}\\param\\GameParam\\GameParam_dlc2.parambnd.dcx"))
                    {
                        ParamBNDs[GameDataManager.GameType] = BND4.Read($"{interroot}\\param\\GameParam\\GameParam_dlc2.parambnd.dcx");
                    }
                    else if (File.Exists($"{interroot}\\Data0.bdt"))
                    {
                        ParamBNDs[GameDataManager.GameType] = SFUtil.DecryptDS3Regulation($"{interroot}\\Data0.bdt");
                    }
                    else if (Directory.Exists($"{interroot}\\..\\param\\GameParam\\") &&
                        File.Exists($"{interroot}\\..\\param\\GameParam\\GameParam_dlc2.parambnd.dcx"))
                    {
                        ParamBNDs[GameDataManager.GameType] = BND4.Read($"{interroot}\\..\\param\\GameParam\\GameParam_dlc2.parambnd.dcx");
                    }
                    else if (File.Exists($"{interroot}\\..\\Data0.bdt"))
                    {
                        ParamBNDs[GameDataManager.GameType] = SFUtil.DecryptDS3Regulation($"{interroot}\\..\\Data0.bdt");
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.DS2SOTFS)
                {
                    if (File.Exists($"{interroot}\\enc_regulation.bnd.dcx"))
                        ParamBNDs[GameDataManager.GameType] = DarkSouls2.DS2GameParamUtil.DecryptDS2Regulation($"{interroot}\\enc_regulation.bnd.dcx");
                    else
                        return false;
                    //System.Windows.Forms.MessageBox.Show("DS2 Params not supported yet.");
                    //LoadStuffFromParamBND(isClearAll: true);
                    //return true;
                }
                else
                {
                    throw new NotImplementedException();
                }

                justNowLoadedParamBND = true;
            }

            if (justNowLoadedParamBND || forceReload || GameTypeCurrentLoadedParamsAreFrom != GameDataManager.GameType)
            {
                LoadStuffFromParamBND(isDS2: GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.DS2SOTFS);
            }

            return true;
        }
    }
}
