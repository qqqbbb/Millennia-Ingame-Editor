using BepInEx;
using CPrompt;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Ingame_Editor
{
    internal class Editor : MonoBehaviour
    //CreateCardFlyoff
    {
        private const int buttonFontSize = 16;
        private const int labelFontSize = 20;
        public static bool showingWindow;
        public static bool mouseCursorOnEditorWindow;
        public APlayer playerToEdit;
        public ADomainManager domainManager;
        public AGameData gameData;
        private Rect windowRect = new Rect(0, Screen.height - 675, 750f, 600f);
        private int topMenuIndex;
        private string[] topMenuButtonNames = new string[5] { "Player", "City", "Units", "Map tile", "Select player" };

        enum TopMenu
        {
            Player,
            City,
            Units,
            Map,
            SelectPlayer
        }

        private string[] playerNames;
        static public ALocation selectedLoc;
        static public AEntityTile selectedTile;
        public static ACity selectedCity;
        private static bool selectedLocValidForLandmarks;
        public static List<AEntityCharacter> selectedUnits;
        private float scale = 1.0f;
        private bool setupDone;
        Dictionary<string, string> terrains = new Dictionary<string, string> { { "TT_GRASSLAND_SNOW", "Frozen Grassland" }, { "TT_SCRUBLAND_SNOW", "Frozen Scrubland" }, { "TT_HILLS_SNOW", "Frozen Hills" }, { "TT_DESERT_SNOW", "Frozen Desert" }, { "TT_HILLS_WASTELAND", "Wasteland Hills" } };
        static Dictionary<string, string> goods = new Dictionary<string, string>();
        int windowID = Main.PLUGIN_GUID.GetHashCode();
        Color selectColor = new Color(.5f, .5f, 1f);
        int selectedTerrainIndex;
        int availableReources;
        Texture2D selectedTexture;
        GUIStyle buttonStyle;
        GUIStyle labelStyle;
        GUIStyle selectedButtonStyle;
        List<AEntityInfo> unitList;
        static Dictionary<string, string> landmarks;

        //readonly string[] militaryLandUnitTags = { "TypeLine", "TypeMobile", "TypeRanged", "TypeSiege", "TypeCommando" };
        //readonly string[] civilLandUnitTags = { "NonCombatant", "TypeSettler" };                     
        readonly string[] militaryNavalUnitTags = { "CombatType:CT_Warship", "CombatType:CT_Attackship", "CombatType:CT_Capship" };
        readonly string[] civilNavalUnitTags = { "UtilityShip", "TypeSettler" };
        static bool terrainChoice;
        static bool goodsChoice;
        static int numValidResources;
        static bool unitChoice;
        static Vector2 scrollPosition;
        static bool landMarkChoice;

        private void Setup()
        {
            if (AGame.Instance.GetGameType() != AGameType.AGT_SinglePlayer)
            {
                Destroy(this);
                return;
            }
            ADevConfig.EnableConsole = Config.enableConsole.Value;
            selectedTexture = Util.MakeTexture(2, 2, selectColor);
            List<AEntityInfo> resources = AEntityInfoManager.Instance.GetAllWithTag(AEntityTile.cBonusTile);
            foreach (var info in resources)
            {
                //string type = info.ID;
                //StringBuilder sb = new StringBuilder();
                //foreach (var s in info.Tags)
                //    sb.Append(s + ' ');
                //Main.logger.LogInfo("" + type + " " + sb.ToString());
                if (info.ID.Contains("BASE") == false)
                {
                    string name = AGame.Instance.GetEntityTypeDisplayName(info.ID);
                    goods[info.ID] = name;
                }
            }
            foreach (string id in AMapController.Instance.TerrainTypes.Keys)
            {
                //Main.logger.LogInfo("TerrainTypes " + id);
                if (terrains.ContainsKey(id))
                    continue;

                string name = AGame.Instance.GetTerrainTypeDisplayName(id);
                terrains[id] = name;
                //terrainTypeIDs.Add(key);
                //terrainTypeNames.Add(name);
            }
            setupDone = true;
        }

        private void Update()
        {
            if (!AGame.Instance || !AGame.Instance.CurrPlayer)
                return;

            if (setupDone == false)
                Setup();

            if (Input.GetKeyDown(Config.hotkey.Value))
            {
                if (showingWindow)
                    CloseWindow();
                else
                    OpenWindow();
            }
            if (showingWindow)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                    CloseWindow();
            }
        }

        private void OpenWindow()
        {
            playerToEdit = AGame.Instance.CurrPlayer;
            gameData = playerToEdit.GetComponent<AGameData>();
            domainManager = playerToEdit.GetComponent<ADomainManager>();
            showingWindow = true;
        }

        private void CloseWindow()
        {
            mouseCursorOnEditorWindow = false;
            CloseSubmenus();
            showingWindow = false;
        }

        public static void SelectLocation(ALocation loc)
        {
            Main.logger.LogInfo("SelectLocation " + loc);
            CloseSubmenus();
            selectedLoc = loc;
            selectedUnits = null;
            selectedCity = null;
            selectedLocValidForLandmarks = ALandmarkManager.Instance.IsLocationValidForLandmarks(selectedLoc, false);
            selectedUnits = selectedLoc.GetUnits(AGame.cUnitLayerNormal);
            numValidResources = GetNumValidGoods(selectedLoc);
            selectedTile = selectedLoc.GetTile();
            if (selectedTile)
                Main.logger.LogDebug("SelectLocation " + selectedTile.TypeID);

            if (selectedTile && selectedTile.IsCity(false))
                selectedCity = selectedTile.GetCity();
        }

        public static void ClearSelection()
        {
            if (unitChoice)
                return;

            Main.logger.LogInfo("ClearSelection ");
            selectedCity = null;
            selectedUnits = null;
            selectedLoc = null;
            selectedTile = null;
            CloseSubmenus();
        }

        private static void CloseSubmenus()
        {
            terrainChoice = false;
            goodsChoice = false;
            unitChoice = false;
            landMarkChoice = false;
            scrollPosition = Vector2.zero;
        }

        private void OnGUI()
        {
            if (showingWindow == false)
                return;

            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button);
                selectedButtonStyle = new GUIStyle(buttonStyle);
                //buttonStyle.fontSize = Mathf.RoundToInt(buttonFontSize * scale);
                //selectedButtonStyle.fontSize = Mathf.RoundToInt(buttonFontSize * 2 * scale);
                //selectedGridStyle.fontSize = Mathf.RoundToInt(16 * scale);
                buttonStyle.normal.textColor = Color.white;
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.normal.textColor = Color.black;
                labelStyle.fontSize = Mathf.RoundToInt(labelFontSize * scale);
                selectedButtonStyle.normal.textColor = Color.white;
                selectedButtonStyle.normal.background = selectedTexture;
                selectedButtonStyle.hover.background = selectedTexture;
                selectedButtonStyle.fontSize = Mathf.RoundToInt(buttonFontSize * scale);

            }
            scale = Config.editorUIscale.Value;
            float scaledWidth = 750f * scale;
            float scaledHeight = 600f * scale;
            //windowRect = new Rect(0, Screen.height - 675, 750f, 600f);
            //windowRect = new Rect(0, Screen.height - (675 * scale), 750f * scale, 600f * scale);
            windowRect = new Rect(0, Screen.height - scaledHeight, scaledWidth, scaledHeight);
            windowRect = GUI.Window(windowID, windowRect, DrawEditorWindow, "");
            mouseCursorOnEditorWindow = windowRect.Contains(Event.current.mousePosition);
            if (mouseCursorOnEditorWindow)
            {
                //ATooltipController.Instance.ClearAllTooltips();
            }
        }

        public void DrawEditorWindow(int winId)
        {
            //GUI.skin.label.fontStyle.
            //GUI.contentColor = Color.black;
            labelStyle.fontSize = Mathf.RoundToInt(labelFontSize * scale);
            GUI.skin.label = labelStyle;
            float scaledWidth = 750f * scale;
            //GUI.backgroundColor
            GUI.skin.button.fontSize = Mathf.RoundToInt(buttonFontSize * scale);
            buttonStyle.fontSize = Mathf.RoundToInt(buttonFontSize * scale);
            selectedButtonStyle.fontSize = Mathf.RoundToInt(buttonFontSize * scale);
            GUILayout.BeginArea(new Rect(10f * scale, 20f * scale, 720f * scale, 570f * scale));
            DrawTopMenu();
            if (topMenuIndex == 0)
            {
                DrawPlayerMenu();
            }
            else if (topMenuIndex == 1 && selectedCity)
            {
                DrawCityMenu();
            }
            else if (topMenuIndex == 2)
            {
                DrawUnitMenu();
            }
            else if (topMenuIndex == 3)
            {
                DrawTileMenu();
            }
            else if (topMenuIndex == 4)
            {
                DrawPlayerChoiceMenu();
            }
            if (GUI.changed)
                AGame.Instance.SetIsCheating();

            GUILayout.EndArea();
        }

        private void DrawTopMenu()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < topMenuButtonNames.Length; i++)
            {
                string buttonName = topMenuButtonNames[i];
                if (selectedCity == null && buttonName == "City")
                {
                    continue;
                }
                else if ((selectedUnits == null || selectedUnits.Count == 0) && buttonName == "Units")
                {
                    continue;
                }
                else if (selectedLoc == null && buttonName == "Map tile")
                {
                    continue;
                }
                GUIStyle style = buttonStyle;
                if (i == topMenuIndex)
                {
                    style = selectedButtonStyle;
                    GUI.backgroundColor = selectColor;
                }
                else
                    GUI.backgroundColor = Color.black;

                //buttonStyle.fontSize = Mathf.RoundToInt(buttonFontSize * scale);
                if (GUILayout.Button(buttonName, style))
                {
                    topMenuIndex = i;
                    CloseSubmenus();
                }
            }
            GUILayout.EndHorizontal();

        }

        private void DrawPlayerMenu()
        {
            DrawLabelRightSide("Player to edit: " + playerToEdit.GetNationName());
            GUILayout.Label("");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Wealth +1000"))
            {
                gameData.AdjustBaseValueCapped(APlayer.cResCoin, 1000f, false);
            }
            if (GUILayout.Button("Culture +100"))
            {
                gameData.AdjustBaseValueCapped(APlayer.cResCulture, 100f, false);
                //gameData.GetFloatValue(APlayer.cCultureNeeded);
            }
            //if (GUILayout.Button("Knowledge +100"))
            //{
            //    gameData.AdjustBaseValueCapped(APlayer.cResKnowledge, 100f, false);
            //    playerToEdit.ApplyKnowledge();
            //}
            if (GUILayout.Button("Improvement points +100"))
            {
                gameData.AdjustBaseValueCapped(APlayer.cResImprovementPoints, 100f, false);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Specialists +100"))
            {
                gameData.AdjustBaseValueCapped(APlayer.cResSpecialists, 100f, false);
            }
            if (GUILayout.Button("Innovation per turn +100"))
            {
                gameData.AdjustBaseValueCapped(APlayer.cResInnovation + AGame.cPerTurnSuffix, 100f, false);
            }
            if (GUILayout.Button("Chaos per turn -100"))
            {
                gameData.AdjustBaseValueCapped(APlayer.cResChaos + AGame.cPerTurnSuffix, -100f, false);
            }
            if (GUILayout.Button("Innovation +100"))
            {
                gameData.AdjustBaseValueCapped(APlayer.cResInnovation, 100f, false);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Chaos -100"))
            {
                gameData.AdjustBaseValueCapped(APlayer.cResChaos, -100f, false);
            }
            if (GUILayout.Button("Chaos +100"))
            {
                gameData.AdjustBaseValueCapped(APlayer.cResChaos, 100f, false);
            }
            if (GUILayout.Button("Government xp +100"))
            {
                gameData.AdjustBaseValue(ADomainManager.cResDomainXPGovernment, 100f, false);
            }
            if (GUILayout.Button("Exploration xp +100"))
            {
                //AGame.cGameDataResourcePrefix + ADomainManager.cDomainExploration
                gameData.AdjustBaseValue(domainManager.GetDomainXPKey(ADomainManager.cDomainExploration), 100f, false);
                //data.SetBaseValueAsBool("DomainUnlock-DomainExploration", true);
                gameData.SetBaseValueAsBool(domainManager.GetDomainUnlockKey(ADomainManager.cDomainExploration), true);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Warfare xp +100"))
            {
                gameData.AdjustBaseValue(domainManager.GetDomainXPKey(ADomainManager.cDomainWarfare), 100f, false);
                //gameData.SetBaseValueAsBool(ADomainManager.cDomainUnlock + ADomainManager.cDomainWarfare, true);
                gameData.SetBaseValueAsBool(domainManager.GetDomainUnlockKey(ADomainManager.cDomainWarfare), true);
            }
            if (GUILayout.Button("Engineering xp +100"))
            {
                gameData.AdjustBaseValue(domainManager.GetDomainXPKey(ADomainManager.cDomainEngineering), 100f, false);
                //gameData.SetBaseValueAsBool(ADomainManager.cDomainUnlock + ADomainManager.cDomainEngineering, true);
                gameData.SetBaseValueAsBool(domainManager.GetDomainUnlockKey(ADomainManager.cDomainEngineering), true);
            }
            if (GUILayout.Button("Diplomacy xp +100"))
            {
                gameData.AdjustBaseValue(domainManager.GetDomainXPKey(ADomainManager.cDomainDiplomacy), 100f, false);
                //gameData.AdjustBaseValue(AGame.cGameDataResourcePrefix + ADomainManager.cDomainDiplomacy, 100f, false);
                //gameData.SetBaseValueAsBool(ADomainManager.cDomainUnlock + ADomainManager.cDomainDiplomacy, true);
                gameData.SetBaseValueAsBool(domainManager.GetDomainUnlockKey(ADomainManager.cDomainDiplomacy), true);
            }
            if (GUILayout.Button("Arts xp +100"))
            {
                //gameData.AdjustBaseValue(AGame.cGameDataResourcePrefix + ADomainManager.cDomainArts, 100f, false);
                gameData.AdjustBaseValue(domainManager.GetDomainXPKey(ADomainManager.cDomainArts), 100f, false);
                //this.gameData.SetBaseValueAsBool(ADomainManager.cDomainUnlock + ADomainManager.cDomainArts, true);
                gameData.SetBaseValueAsBool(domainManager.GetDomainUnlockKey(ADomainManager.cDomainArts), true);
            }
            GUILayout.EndHorizontal();
            ACard currentTech = playerToEdit.GetCurrentTech();
            if (currentTech != null)
            {
                GUILayout.Label("");
                if (GUILayout.Button("Finish researching " + currentTech.GetCardTitle()))
                    playerToEdit.SetResearched(currentTech.ID);
            }
            //float cultureMeterMax = playerToEdit.GetCultureMeterMax();
            //if (GUILayout.Button("cultureMeterMax " + cultureMeterMax))
            //    playerToEdit.SetResearched(currentTech.ID);

            GUILayout.Label("");
            if (GUILayout.Button("Reveal all landmarks"))
            {
                ACard card = ACard.FindFromText("EXPLORERS-LANDMARKS");
                card.Play(null, null, playerToEdit);
            }
            if (GUILayout.Button("Reveal all tribal camps"))
            {
                ACard card = ACard.FindFromText("EXPLORERS-LANDMARKS");
                card.Choices[0].Effects[0].Target = "ENTTAG,ALLPLAYERS-" + AEntityTile.cTagRewardCamp;
                card.Play(null, null, playerToEdit);
            }
            if (GUILayout.Button("Reveal entire map"))
            {
                AMapController.Instance.RevealFog(true);
            }
            if (GUI.changed)
            {
                AUIManager.Instance.RefreshAllPanels(UIRefreshType.cUIRefreshAll);
            }
        }

        private void DrawCityMenu()
        {
            GUILayout.Label("");
            DrawLabelRightSide(selectedCity.GetDisplayName());
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1 " + AStringTable.Instance.GetString("UI-CityFrame-PopulationLabel")))
                selectedCity.AddPopulation(1);

            if (!selectedCity.PrimaryProductionQueue.IsEmpty() && GUILayout.Button("Finish production"))
            {
                selectedCity.PrimaryProductionQueue.UseProductionPoints(99999f, selectedCity.GetComponent<AGameData>(), out float _);
            }
            if (selectedCity.IsVassal() && GUILayout.Button("Integrate vassal"))
            {
                selectedCity.IntegrateVassal();
                //AGameData gameData = selectedCity.GetComponent<AGameData>();
                //gameData.SetBaseValue(ACity.cStatVassalIntegration, gameData.GetFloatValue(ACity.cStatVassalIntegrationNeeded, 0), true);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawUnitMenu()
        {
            if (selectedLoc == null)
                return;

            GUILayout.Label("");
            foreach (var unit in selectedUnits)
            {
                DrawLabelRightSide(unit.GetDisplayName());
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Restore HP"))
                {
                    unit.HealStatFrac(AEntityCharacter.cStatHealth, 1f);
                    unit.HealStatFrac(AEntityCharacter.cStatCommand, 1f);
                }
                if (GUILayout.Button("Restore movement points"))
                {
                    unit.HealStatFrac(AEntityCharacter.cStatMovement, 1f);
                }
                if (GUILayout.Button("Delete unit"))
                    unit.DestroyEntity(false, false, false, false);

                GUILayout.EndHorizontal();
            }
        }

        private void DrawTileMenu()
        {
            if (selectedLoc == null)
                return;

            DrawLabelRightSide("Player to edit: " + playerToEdit.GetNationName());
            GUILayout.Label("");
            //int rows = Mathf.CeilToInt((float)gridSize / columns);
            if (terrainChoice)
            {
                DrawTerrainChoiceMenu();
                //Func<string, bool> changeTerrain = (terrainKey) =>
                //{
                //    bool b = selectedLoc.ChangeTerrain(terrainKey);
                //    SelectLocation(AInputHandler.Instance.SelectedLocation);
                //    return b;
                //};
                //DrawChoiceMenu(4, terrains, changeTerrain);
                return;
            }
            else if (goodsChoice)
            {
                DrawGoodsChoiceMenu();
                return;
            }
            else if (landMarkChoice)
            {
                DrawLandMarkChoiceMenu();
                return;
            }
            else if (unitChoice)
            {
                DrawUnitChoiceMenu();
                return;
            }
            DrawLabelRightSide("Terrain: " + terrains[selectedLoc.TerrainType]);
            if (GUILayout.Button("Change terrain"))
            {
                terrainChoice = true;
                return;
            }
            if (selectedCity == null)
            {
                string goodsName = "";
                if (selectedTile && selectedTile.HasTag(AEntityTile.cBonusTile))
                {
                    goodsName = "Goods: " + AGame.Instance.GetEntityTypeDisplayName(selectedTile.TypeID);
                    GUILayout.Label("");
                }
                if (numValidResources > 0)
                {
                    DrawLabelRightSide(goodsName);
                    if (GUILayout.Button("Change goods"))
                    {
                        goodsChoice = true;
                        return;
                    }
                }
                if (selectedLocValidForLandmarks)
                {
                    GUILayout.Label("");
                    if (GUILayout.Button("Create landmark"))
                    {
                        landmarks = GetLandmarks(selectedLoc.TileCoord);
                        landMarkChoice = true;
                        return;
                    }
                }
            }
            if (selectedLoc.HasSpaceFor(1, playerToEdit.PlayerNum, AGame.cUnitLayerNormal) == false)
                return;

            GUILayout.Label("");
            if (Util.IsWaterTile(selectedLoc))
            {
                if (GUILayout.Button("Create civilian unit"))
                {
                    unitList = Util.GetEntityInfosWithTag(AEntityCharacter.cTerrainTagWaterMovement);
                    //foreach (var unit in unitList)
                    //{
                    //    int att = unit.GetStartingDataValueAsInt(ACombatManager.cStatAttack);
                    //    Main.logger.LogMessage("" + unit.ID + " att " + att);
                    //}
                    unitList = Util.GetEntityInfosWithAnyTag(unitList, civilNavalUnitTags);
                    unitChoice = true;
                }
                if (GUILayout.Button("Create military unit"))
                {
                    //< Tag > CombatType:CT_Ranged
                    //CombatType: CT_Line
                    //< Tag > CombatType:CT_Mobile
                    //unitList = Util.GetEntityInfosWithTag(AEntityCharacter.cTerrainTagWaterMovement);
                    //    unitList = Util.RemoveEntityInfosFromList(unitList, "UtilityShip");
                    //    unitList = Util.RemoveEntityInfosFromList(unitList, "TypeSettler");
                    unitList = Util.GetEntityInfosWithAnyTag(militaryNavalUnitTags);
                    unitChoice = true;
                    //Main.logger.LogInfo("unitList ");
                    //foreach (var unit in unitList)
                    //    Main.logger.LogInfo(" " + unit.ID);
                }
            }
            else if (GUILayout.Button("Create civilian unit"))
            {
                unitList = Util.GetEntityInfosWithTag(ACombatManager.cTagNonCombatant);
                List<AEntityInfo> settlers = Util.GetEntityInfosWithTag("TypeSettler");
                settlers = Util.RemoveEntityInfosFromList(settlers, AEntityCharacter.cTerrainTagWaterMovement);
                unitList.AddRange(settlers);
                unitChoice = true;
            }
            else if (GUILayout.Button("Create line unit"))
            {
                unitList = Util.GetEntityInfosWithTag("TypeLine");
                //unitList = GetEntityInfosWithTag(AEntityCharacter.cTagCombatant);
                unitChoice = true;
            }
            else if (GUILayout.Button("Create mobile unit"))
            {
                unitList = Util.GetEntityInfosWithTag("TypeMobile");
                unitChoice = true;
            }
            else if (GUILayout.Button("Create ranged unit"))
            {
                unitList = Util.GetEntityInfosWithTag("TypeRanged");
                List<AEntityInfo> commandos = Util.GetEntityInfosWithTag("TypeCommando");
                commandos = Util.RemoveEntityInfosFromList(commandos, AEntityCharacter.cTerrainTagWaterMovement);
                unitList.AddRange(commandos);
                unitChoice = true;
            }
            else if (GUILayout.Button("Create siege unit"))
            {
                unitList = Util.GetEntityInfosWithTag("TypeSiege");
                unitChoice = true;
            }
            //if (GUILayout.Button("Create unit for another player", gridStyle))
            //{
            //    playerChoice = true;
            //}
        }

        private void DrawPlayerChoiceMenu()
        {
            int columns = 4;
            GUILayout.Label("");
            GUI.backgroundColor = Color.black;
            GUILayout.BeginHorizontal();
            int count = 0;
            foreach (APlayer player in AGame.Instance.Players)
            {
                if (player.IsEliminated)
                    continue;

                if (count % columns == 0 && count != 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                string name = player.GetNationName();
                if (GUILayout.Button(name))
                {
                    topMenuIndex = 0;
                    playerToEdit = player;
                }
                count++;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawLabelRightSide(string s)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(s);
            GUILayout.EndHorizontal();
        }

        private void DrawLandMarkChoiceMenu()
        {
            int columns = 4;
            GUILayout.Label("");
            GUILayout.BeginHorizontal();
            int count = 0;
            foreach (var kv in landmarks)
            {
                if (count % columns == 0 && count != 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                if (GUILayout.Button(kv.Value))
                {
                    AEntityTile tile = ALandmarkManager.Instance.CreateLandmark(selectedLoc.TileCoord, kv.Key);
                    //SelectLocation(AInputHandler.Instance.SelectedLocation);
                    CloseSubmenus();
                }
                count++;
            }
            GUILayout.EndHorizontal();
        }

        public bool ShouldNotSpawnLandmark(string id)
        {
            return id == "LM_WASTELAND_RUINS_A" || id == "LM_WASTELAND_RUINS_B" || id == "LM_WASTELAND_RUINS_C" || id == "LM_WASTELAND_RUINS_D" || id == "LM_WASTELAND_RUINS_REPEATABLE2" || id.StartsWith("SHARED_LANDMARK_RULES") || id.StartsWith("LM_QUEST");
        }

        public Dictionary<string, string> GetLandmarks(ATileCoord coord)
        {
            landmarks = new Dictionary<string, string>();
            Main.logger.LogInfo("GetLandmarks");
            foreach (string id in ALandmarkManager.Instance.LandmarkInfo.Keys)
            {
                if (ShouldNotSpawnLandmark(id))
                    continue;

                string name = GetLandmarkName(id);
                Main.logger.LogMessage(" " + id + " " + name);
                if (Config.landmarkCheck.Value)
                {
                    ALandmark landmark = ALandmarkManager.Instance.LandmarkInfo[id];
                    if (landmark != null && landmark.IsValid(coord))
                        landmarks[id] = name;
                }
                else
                    landmarks[id] = name;
            }
            return landmarks;
        }

        private string GetLandmarkName(string id)
        {
            ALandmark landmarkInfo = ALandmarkManager.Instance.GetLandmarkInfo(id);
            string key = string.Format("Landmark-{0}-DisplayName", landmarkInfo.ID);
            return AStringTable.Instance.GetString(key);
        }

        private void DrawUnitChoiceMenu()
        {
            int columns = 4;
            float scaledHeight = 600f * Config.editorUIscale.Value;
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(scaledHeight * .85f)); // line unit list is big
            GUILayout.Label("");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < unitList.Count; i++)
            {
                if (i % columns == 0 && i != 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                string id = unitList[i].ID;
                string name = AEntityInfoManager.Instance.GetEntityInfoDisplayName(id);
                if (id == "UNIT_DEBUGSCOUT")
                    name = "Debug scout";

                if (GUILayout.Button(name))
                {
                    AEntityCharacter unit = AMapController.Instance.CreateUnit(id, selectedLoc.TileCoord, playerToEdit.PlayerNum);
                    ASelectable selectable = unit.GetComponent<ASelectable>();
                    AInputHandler.Instance.SetSelection(selectable, false, false, false);
                    CloseSubmenus();
                    //SelectTile(selectedLoc);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        static int GetNumValidGoods(ALocation loc)
        {
            int count = 0;
            foreach (var kv in goods)
            {
                if (Config.tileGoodsCheck.Value && loc.GetTerrainType().IsResourceTileTypeValid(kv.Key, AGame.Instance.CurrPlayer.PlayerNum) == false)
                    continue;

                count++;
            }
            return count;
        }

        private void DrawGoodsChoiceMenu()
        {
            int columns = 6;
            int count = 0;
            GUILayout.Label("");
            GUILayout.BeginHorizontal();
            foreach (var kv in goods)
            {
                if (Config.tileGoodsCheck.Value && selectedLoc.GetTerrainType().IsResourceTileTypeValid(kv.Key, playerToEdit.PlayerNum) == false)
                    continue;

                if (selectedTile && selectedTile.TypeID == kv.Value)
                    continue;

                if (count % columns == 0 && count != 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                if (GUILayout.Button(kv.Value))
                {
                    selectedTile = AMapController.Instance.SpawnTile(kv.Key, selectedLoc, selectedLoc.PlayerNum);
                    CloseSubmenus();
                }
                count++;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawTerrainChoiceMenu()
        {
            int columns = 6;
            GUILayout.Label("");
            GUILayout.BeginHorizontal();
            int count = 0;
            foreach (var kv in terrains)
            {
                if (count % columns == 0 && count != 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                if (kv.Key == selectedLoc.TerrainType)
                    continue;

                if (GUILayout.Button(kv.Value))
                {
                    selectedLoc.ChangeTerrain(kv.Key);
                    SelectLocation(AInputHandler.Instance.SelectedLocation);
                    CloseSubmenus();
                }
                count++;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawChoiceMenu(int columns, Dictionary<string, string> dic, Func<string, bool> func)
        {
            GUILayout.Label("");
            GUILayout.BeginHorizontal();
            int count = 0;
            foreach (var kv in dic)
            {
                if (count % columns == 0 && count != 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                if (kv.Key == selectedLoc.TerrainType)
                    continue;

                if (GUILayout.Button(kv.Value))
                {
                    func(kv.Key);
                    SelectLocation(AInputHandler.Instance.SelectedLocation);
                    CloseSubmenus();
                }
                count++;
            }
            GUILayout.EndHorizontal();
        }

    }
}
