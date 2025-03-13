using CPrompt;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace Ingame_Editor
{
    internal class Util
    {
        static public Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; ++i)
                pixels[i] = color;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pixels);
            result.Apply();
            return result;
        }

        static public List<AEntityInfo> GetEntityInfosFromList(List<AEntityInfo> list, string tag)
        {
            List<AEntityInfo> newList = new List<AEntityInfo>();
            foreach (var ei in list)
            {
                if (ei.Tags.Contains(tag))
                    newList.Add(ei);
            }
            return newList;
        }

        static public List<AEntityInfo> RemoveEntityInfosFromList(List<AEntityInfo> list, string tag)
        {
            List<AEntityInfo> newList = new List<AEntityInfo>();
            foreach (var ei in list)
            {
                if (ei.Tags.Contains(tag) == false)
                    newList.Add(ei);
            }
            return newList;
        }

        static public List<AEntityInfo> GetEntityInfosWithAnyTag(params string[] tags)
        {
            List<AEntityInfo> units = new List<AEntityInfo>();
            foreach (string key in AEntityInfoManager.Instance.EntityInfo.Keys)
            {
                AEntityInfo entityInfo = AEntityInfoManager.Instance.EntityInfo[key];
                if (ShouldNotSpawnUnit(entityInfo))
                    continue;

                foreach (var tag in tags)
                {
                    if (entityInfo.Tags.Contains(tag))
                    {
                        units.Add(entityInfo);
                        break;
                    }
                }
            }
            return units;
        }

        static public List<AEntityInfo> GetEntityInfosWithAnyTag(List<AEntityInfo> input, params string[] tags)
        {
            List<AEntityInfo> units = new List<AEntityInfo>();
            foreach (AEntityInfo entityInfo in input)
            {
                foreach (var tag in tags)
                {
                    if (entityInfo.Tags.Contains(tag))
                    {
                        units.Add(entityInfo);
                        break;
                    }
                }
            }
            return units;
        }

        static public List<AEntityInfo> GetEntityInfosWithEveryTag(params string[] tags)
        {
            List<AEntityInfo> units = new List<AEntityInfo>();
            foreach (string key in AEntityInfoManager.Instance.EntityInfo.Keys)
            {
                AEntityInfo entityInfo = AEntityInfoManager.Instance.EntityInfo[key];
                if (ShouldNotSpawnUnit(entityInfo))
                    continue;

                bool skip = false;
                foreach (var tag in tags)
                {
                    if (entityInfo.Tags.Contains(tag) == false)
                    {
                        skip = true;
                        break;
                    }
                }
                if (skip == false)
                    units.Add(entityInfo);
            }
            return units;
        }

        static public List<AEntityInfo> GetEntityInfosWithTag(string tag)
        {
            List<AEntityInfo> units = new List<AEntityInfo>();
            foreach (string key in AEntityInfoManager.Instance.EntityInfo.Keys)
            {
                AEntityInfo entityInfo = AEntityInfoManager.Instance.EntityInfo[key];
                if (ShouldNotSpawnUnit(entityInfo))
                    continue;

                if (entityInfo.Tags.Contains(tag))
                {
                    units.Add(entityInfo);
                }
            }
            return units;
        }

        private static bool ShouldNotSpawnUnit(AEntityInfo entityInfo)
        {
            return entityInfo.ID == "UNIT_LEADER" || entityInfo.ID == "UNIT_DRONECARRIER" || entityInfo.ID.StartsWith("UNIT_COMBAT") || entityInfo.ID == "UNIT_SETTLER_REGION" || entityInfo.ID == "UNIT_REVOLUTIONARY" || entityInfo.ID.Contains("_BASE") || entityInfo.ID.Contains("MILITIA") || entityInfo.ID.EndsWith("DEFENDER") || entityInfo.ID.Contains("TRANSPORT");
        }

        static public bool IsWaterTile(ALocation location)
        {
            return location.GetTerrainType().HasTag(AEntityCharacter.cTerrainTagWaterMovement);
        }

        static public void DumpStrings()
        {
            Main.logger.LogInfo("StringTable dump ");
            foreach (var kv in AStringTable.Instance.StringTable)
            {
                Main.logger.LogInfo(kv.Key + ":::" + kv.Value + "...");
                Main.logger.LogInfo("---------------------");
            }
        }

        static public bool IsGoodsTypeValidForTerrain(ATerrainType terrainType, string tileID)
        {
            //Main.logger.LogInfo("IsGoodsTypeValidForTerrain terrainType " + terrainType + " tileID " + tileID);
            AEntityInfo ei = AEntityInfoManager.Instance.Get(tileID);
            if (ei == null)
            {
                //Main.logger.LogInfo("IsGoodsTypeValidForTerrain " + tileID + " ei == null ");
                return false;
            }
            string tagName = ATerrainType.cValidTileResourceTerrainRoot + terrainType.ID;
            //Main.logger.LogInfo("IsGoodsTypeValidForTerrain " + ei.ID + " tagName " + tagName + " " + ei.HasTag(tagName));
            return ei.HasTag(tagName);
        }

        static public bool IsGoodsTypeValidForPlayer(string terrainType, int forPlayer)
        {
            APlayer player = null;
            AEntityInfo ei = AEntityInfoManager.Instance.Get(terrainType);
            if (forPlayer > -1)
                player = AGame.Instance.GetPlayer(forPlayer);

            if (player != null && player.IsTileTypeHidden(ei.ID))
                return false;

            return true;
        }

        private void PrintUnits(List<AEntityInfo> units)
        {
            Main.logger.LogInfo("unitList ");
            foreach (var unit in units)
                Main.logger.LogInfo(" " + unit.ID);
        }

        static public string GetAgeName(ACard age)
        {
            string name = age.GetCardTitle();
            if (age.ID.EndsWith("CRISISWASTELAND_ADVANCE"))
                name = AStringTable.Instance.GetString("TECHAGE9_CRISISWASTELAND-AgeTitle");

            return name;
        }


    }
}
