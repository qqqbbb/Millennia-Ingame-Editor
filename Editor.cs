using BepInEx;
using CPrompt;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;


namespace Ingame_Editor
{
    internal class Editor : MonoBehaviour
    {
        private const int buttonFontSize = 16;
        private const int labelFontSize = 20;
        public static bool showingEditorWindow;
        public static bool mouseCursorOnEditorWindow;
        static APlayer playerToEdit;
        static APlayer currentPlayer;
        public ADomainManager domainManager;
        public AGameData gameData;
        private Rect windowRect = new Rect(0, Screen.height - 675, 750f, 600f);
        enum TopMenu { Player, City, Units, Map, CreateUnit }
        enum SubMenu { None, Unit, Terrain, Goods, Landmark, SelectPlayer, nextAge }
        static TopMenu openMenu;
        static SubMenu openSubMenu;
        static public ALocation selectedLoc;
        static public AEntityTile selectedTile;
        public static ACity selectedCity;
        private static bool selectedLocValidForLandmark;
        public static List<AEntityCharacter> selectedUnits;
        private float scale = 1f;
        private bool setupDone;
        Dictionary<string, string> terrains = new Dictionary<string, string> { { "TT_GRASSLAND_SNOW", "Frozen Grassland" }, { "TT_SCRUBLAND_SNOW", "Frozen Scrubland" }, { "TT_HILLS_SNOW", "Frozen Hills" }, { "TT_DESERT_SNOW", "Frozen Desert" }, { "TT_HILLS_WASTELAND", "Wasteland Hills" } };
        static Dictionary<string, string> goods = new Dictionary<string, string>();
        int windowID = Main.PLUGIN_GUID.GetHashCode();
        Color selectColor = new Color(.5f, .5f, 1f);
        int availableReources;
        Texture2D selectedTexture;
        GUIStyle buttonStyle;
        GUIStyle labelStyle;
        GUIStyle selectedButtonStyle;
        List<AEntityInfo> unitList;
        static Dictionary<string, string> landmarks = new Dictionary<string, string>();
        static List<string> validLandmarks = new List<string>();
        readonly string[] militaryNavalUnitTags = { "CombatType:CT_Warship", "CombatType:CT_Attackship", "CombatType:CT_Capship", "TypeCarrier" };
        readonly string[] civilNavalUnitTags = { "UtilityShip", "TypeSettler" };
        static Vector2 scrollPosition;
        static AGameData cityData;
        private static bool neutralTownSelected;
        private GUIStyle defaultLabelStyle;
        private GUIStyle defaultButtonStyle;
        private int defaultButtonFontSize;
        private Color defaultBackgroundColor;
        static bool goodsPlayerCheck = true;
        private bool goodsTerrainCheck = true;
        private Dictionary<string, string> nameOverrides = new Dictionary<string, string> { { "UNIT_DEBUGSCOUT", "Debug scout" } };
        private bool landMarkCheck;
        private static APlayer selectedUnitOwner;
        private bool landmarksRevealed;
        private bool rewardCampsRevealed;
        List<ACard> advanceAges;

        private void Setup()
        {
            if (AGame.Instance.GetGameType() != AGameType.AGT_SinglePlayer)
            {
                Destroy(this);
                return;
            }
            currentPlayer = AGame.Instance.CurrPlayer;
            //ADevConfig.EnableConsole = Config.enableConsole.Value;
            selectedTexture = Util.MakeTexture(2, 2, selectColor);
            List<AEntityInfo> goodsInfos = AEntityInfoManager.Instance.GetAllWithTag(AEntityTile.cBonusTile);
            foreach (var info in goodsInfos)
            {
                if (info.ID.Contains("BASE") == false)
                {
                    string name = AGame.Instance.GetEntityTypeDisplayName(info.ID);
                    goods[info.ID] = name;
                }
            }
            foreach (string id in AMapController.Instance.TerrainTypes.Keys)
            {
                if (terrains.ContainsKey(id))
                    continue;

                string name = AGame.Instance.GetTerrainTypeDisplayName(id);
                terrains[id] = name;
            }
            setupDone = true;
        }

        private void Update()
        {
            if (!AGame.Instance || !AGame.Instance.CurrPlayer)
                return;

            if (setupDone == false)
                Setup();

            if (Input.GetKeyDown(Config.editorHotkey.Value))
            {
                if (showingEditorWindow)
                    CloseEditorWindow();
                else
                    OpenEditorWindow();
            }
            if (showingEditorWindow)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (IsSubmenuOpen())
                        CloseSubmenu();
                    else
                        CloseEditorWindow();
                }
            }
        }

        static void OpenMenu(TopMenu topMenu)
        {
            openMenu = topMenu;
            CloseSubmenu();
        }

        private void OpenSubMenu(SubMenu subMenu)
        {
            scrollPosition = Vector2.zero;
            openSubMenu = subMenu;
        }

        private void OpenEditorWindow()
        {
            OpenMenu(TopMenu.Player);
            playerToEdit = AGame.Instance.CurrPlayer;
            gameData = playerToEdit.GetComponent<AGameData>();
            domainManager = playerToEdit.GetComponent<ADomainManager>();
            GetAdvanceAges();
            showingEditorWindow = true;
        }

        private void GetAdvanceAges()
        {
            int ageNumber = playerToEdit.GetAgeNumber();
            ACard baseTech = ATechManager.Instance.GetBaseTechFromChosenAge(ageNumber);
            advanceAges = ATechManager.Instance.GetPossibleAgesFrom(baseTech);
        }

        public static void CloseEditorWindow()
        {
            CloseSubmenu();
            mouseCursorOnEditorWindow = false;
            showingEditorWindow = false;
        }

        public static void SelectLocation(ALocation loc)
        {
            //Main.logger.LogInfo("SelectLocation " + loc);
            CloseSubmenu();
            selectedLoc = loc;
            selectedUnits = null;
            selectedCity = null;
            neutralTownSelected = false;
            selectedLocValidForLandmark = ALandmarkManager.Instance.IsLocationValidForLandmarks(selectedLoc, false);
            //selectedLocValidForLandmark = Util.IsLocationValidForLandmarks(selectedLoc, false);
            selectedUnits = selectedLoc.GetUnits(AGame.cUnitLayerAll);
            selectedUnitOwner = null;
            if (selectedUnits.Count == 0)
            {
                if (openMenu == TopMenu.Units)
                    OpenMenu(TopMenu.Map);
            }
            else
            {
                selectedUnitOwner = selectedUnits[0].GetPlayer();
                if (openMenu == TopMenu.CreateUnit && CanCreateUnit() == false)
                    OpenMenu(TopMenu.Units);
            }
            selectedTile = selectedLoc.GetTile();
            if (selectedTile == null)
                return;

            //Main.logger.LogDebug("SelectLocation tile ID " + selectedTile.TypeID);
            //Main.logger.LogDebug("SelectLocation tile Tags ");
            //foreach (var tag in selectedTile.TagDict.Tags)
            //    Main.logger.LogDebug(tag);

            neutralTownSelected = selectedTile.HasTag(AEntityTile.cTagNeutralTown);
            if (selectedTile.IsCity(false))
            {
                selectedCity = selectedTile.GetCity();
                cityData = selectedCity.GetComponent<AGameData>();
            }
        }

        public static void ClearSelection()
        {
            if (openSubMenu == SubMenu.Unit)
                return;

            selectedCity = null;
            selectedUnits = null;
            selectedUnitOwner = null;
            selectedLoc = null;
            selectedTile = null;
            CloseSubmenu();
        }

        private static void CloseSubmenu()
        {
            openSubMenu = SubMenu.None;
            scrollPosition = Vector2.zero;
        }

        private static bool IsSubmenuOpen()
        {
            return openSubMenu != SubMenu.None;
        }

        private void OnGUI()
        {
            if (showingEditorWindow == false)
                return;

            if (selectedButtonStyle == null)
                SetStyles();

            scale = Config.editorUIscale.Value;
            float scaledWidth = 750f * scale;
            float scaledHeight = 600f * scale;
            windowRect = new Rect(0, Screen.height - scaledHeight, scaledWidth, scaledHeight);
            windowRect = GUI.Window(windowID, windowRect, DrawEditorWindow, "");
            mouseCursorOnEditorWindow = windowRect.Contains(Event.current.mousePosition);
            GUI.backgroundColor = defaultBackgroundColor;
            //if (mouseCursorOnEditorWindow)
            //ATooltipController.Instance.ClearAllTooltips();
        }

        private void SetStyles()
        {
            defaultLabelStyle = GUI.skin.label;
            defaultButtonFontSize = GUI.skin.button.fontSize;
            defaultBackgroundColor = GUI.backgroundColor;
            selectedButtonStyle = new GUIStyle(GUI.skin.button);
            labelStyle = new GUIStyle(GUI.skin.label);
            buttonStyle = new GUIStyle(GUI.skin.button);
            labelStyle.normal.textColor = Color.black;
            selectedButtonStyle.normal.textColor = Color.white;
            selectedButtonStyle.normal.background = selectedTexture;
            selectedButtonStyle.hover.background = selectedTexture;
        }

        public void DrawEditorWindow(int windowId)
        {
            labelStyle.fontSize = Mathf.RoundToInt(labelFontSize * scale);
            int fontSize = Mathf.RoundToInt(buttonFontSize * scale);
            buttonStyle.fontSize = fontSize;
            selectedButtonStyle.fontSize = fontSize;
            GUILayout.BeginArea(new Rect(10f * scale, 20f * scale, 720f * scale, 570f * scale));
            DrawTopMenu();
            if (openMenu == TopMenu.Player)
                DrawPlayerMenu();
            else if (openMenu == TopMenu.City)
            {
                if (selectedCity)
                    DrawCityMenu();
                else if (neutralTownSelected)
                    DrawNeutralTownMenu();
            }
            else if (openMenu == TopMenu.Units)
                DrawUnitMenu();
            else if (openMenu == TopMenu.Map)
                DrawTileMenu();
            else if (openMenu == TopMenu.CreateUnit)
                DrawCreateUnitMenu();

            if (GUI.changed)
                AGame.Instance.SetIsCheating();

            GUILayout.EndArea();
        }

        private void DrawTopMenu()
        {
            GUILayout.BeginHorizontal();
            foreach (TopMenu topMenu in Enum.GetValues(typeof(TopMenu)))
            {
                if (selectedLoc == null)
                {
                    if (topMenu == TopMenu.City || topMenu == TopMenu.Map || topMenu == TopMenu.CreateUnit)
                        continue;
                }
                if (topMenu == TopMenu.City && selectedCity == null && neutralTownSelected == false)
                {
                    if (openMenu == TopMenu.City)
                        OpenMenu(TopMenu.Player);

                    continue;
                }
                else if (topMenu == TopMenu.Units && (selectedUnits == null || selectedUnits.Count == 0))
                    continue;
                else if (topMenu == TopMenu.CreateUnit)
                {
                    if (CanCreateUnit() == false)
                    {
                        if (openMenu == TopMenu.CreateUnit)
                            OpenMenu(TopMenu.Map);

                        continue;
                    }
                }
                string buttonName = topMenu.ToString();
                if (topMenu == TopMenu.CreateUnit)
                    buttonName = "Create unit";
                else if (topMenu == TopMenu.Map)
                    buttonName = "Map tile";

                GUI.backgroundColor = Color.black;
                if (topMenu == openMenu)
                {
                    if (DrawSelectedButton(buttonName))
                        OpenMenu(topMenu);
                }
                else if (GUILayout.Button(buttonName, buttonStyle))
                    OpenMenu(topMenu);
            }
            GUILayout.EndHorizontal();
        }

        private bool DrawSelectedButton(string text)
        {
            GUI.backgroundColor = selectColor;
            bool clicked = GUILayout.Button(text, selectedButtonStyle);
            GUI.backgroundColor = Color.black;
            return clicked;
        }

        private bool DrawSelectedButtonRightSide(string text)
        {
            GUI.backgroundColor = selectColor;
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            float scaledWidth = 365f * Config.editorUIscale.Value;
            bool clicked = GUILayout.Button(text, selectedButtonStyle, GUILayout.Width(scaledWidth));
            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.black;
            return clicked;
        }

        private static bool CanCreateUnit()
        {
            if (selectedLoc.GetIsRevealed(currentPlayer.PlayerNum) == false)
                return false;

            int playerToCheck = GetPlayerIDforCreateUnitMenu();
            return selectedLoc.HasSpaceFor(1, playerToCheck, AGame.cUnitLayerNormal) || selectedLoc.HasSpaceFor(1, playerToCheck, AGame.cUnitLayerAir);
        }

        private void DrawPlayerMenu()
        {
            GUILayout.Label("");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Player to edit: " + playerToEdit.GetNationName(), labelStyle);
            if (GUILayout.Button("Select another player", buttonStyle))
                OpenSubMenu(SubMenu.SelectPlayer);

            GUILayout.EndHorizontal();
            GUILayout.Label("");
            if (openSubMenu == SubMenu.SelectPlayer)
            {
                ShowPlayerChoiceMenu();
                return;
            }
            else if (openSubMenu == SubMenu.nextAge)
            {
                ShowAdvanceAgeMenu();
                return;
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Wealth +1000", buttonStyle))
                gameData.AdjustBaseValueCapped(APlayer.cResCoin, 1000f, false);
            else if (GUILayout.Button("Improvement points +100", buttonStyle))
                gameData.AdjustBaseValueCapped(APlayer.cResImprovementPoints, 100f, false);
            else if (GUILayout.Button("Specialists +100", buttonStyle))
                gameData.AdjustBaseValueCapped(APlayer.cResSpecialists, 100f, false);

            GUILayout.EndHorizontal();
            GUILayout.Label("");
            GUILayout.BeginHorizontal();
            //if (GUILayout.Button("Innovation per turn +100"))
            //    gameData.AdjustBaseValueCapped(APlayer.cResInnovation + AGame.cPerTurnSuffix, 100f, false);
            //if (GUILayout.Button("Chaos per turn -100"))
            //    gameData.AdjustBaseValueCapped(APlayer.cResChaos + AGame.cPerTurnSuffix, -100f, false);
            if (GUILayout.Button("Innovation +100", buttonStyle))
                gameData.AdjustBaseValueCapped(APlayer.cResInnovation, 100f, false);
            else if (GUILayout.Button("Chaos -100", buttonStyle))
                gameData.AdjustBaseValueCapped(APlayer.cResChaos, -100f, false);
            else if (GUILayout.Button("Chaos +100", buttonStyle))
                gameData.AdjustBaseValueCapped(APlayer.cResChaos, 100f, false);

            GUILayout.EndHorizontal();
            GUILayout.Label("");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Government xp +100", buttonStyle))
                gameData.AdjustBaseValue(ADomainManager.cResDomainXPGovernment, 100f, false);
            else if (GUILayout.Button("Exploration xp +100", buttonStyle))
            {
                gameData.AdjustBaseValue(domainManager.GetDomainXPKey(ADomainManager.cDomainExploration), 100f, false);
                gameData.SetBaseValueAsBool(domainManager.GetDomainUnlockKey(ADomainManager.cDomainExploration), true);
            }
            else if (GUILayout.Button("Warfare xp +100", buttonStyle))
            {
                gameData.AdjustBaseValue(domainManager.GetDomainXPKey(ADomainManager.cDomainWarfare), 100f, false);
                gameData.SetBaseValueAsBool(domainManager.GetDomainUnlockKey(ADomainManager.cDomainWarfare), true);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Engineering xp +100", buttonStyle))
            {
                gameData.AdjustBaseValue(domainManager.GetDomainXPKey(ADomainManager.cDomainEngineering), 100f, false);
                gameData.SetBaseValueAsBool(domainManager.GetDomainUnlockKey(ADomainManager.cDomainEngineering), true);
            }
            else if (GUILayout.Button("Diplomacy xp +100", buttonStyle))
            {
                gameData.AdjustBaseValue(domainManager.GetDomainXPKey(ADomainManager.cDomainDiplomacy), 100f, false);
                gameData.SetBaseValueAsBool(domainManager.GetDomainUnlockKey(ADomainManager.cDomainDiplomacy), true);
            }
            else if (GUILayout.Button("Arts xp +100", buttonStyle))
            {
                gameData.AdjustBaseValue(domainManager.GetDomainXPKey(ADomainManager.cDomainArts), 100f, false);
                gameData.SetBaseValueAsBool(domainManager.GetDomainUnlockKey(ADomainManager.cDomainArts), true);
            }
            GUILayout.EndHorizontal();
            ACard currentTech = playerToEdit.GetCurrentTech();
            if (playerToEdit.GetCultureMeterFrac() < 1)
            {
                GUILayout.Label("");
                if (playerToEdit.GetCultureMeterFrac() < 1 && DrawButtonlRightSide("Fill culture meter"))
                    gameData.SetBaseValue(APlayer.cResCulture, playerToEdit.GetCultureMeterMax());
            }
            if (currentTech != null)
            {
                float meterFraction = playerToEdit.GetTechProgress(currentTech, out int numTurns, out float techPerTurn, out float projFrac);
                if (projFrac < 1)
                {
                    GUILayout.Label("");
                    if (DrawButtonlRightSide("Finish researching " + currentTech.GetCardTitle()))
                        playerToEdit.SetResearched(currentTech.ID);
                }
            }
            if (advanceAges.Count > 0)
            {
                GUILayout.Label("");
                if (advanceAges.Count == 1)
                {
                    ACard nextAge = advanceAges[0];
                    if (DrawButtonlRightSide("Advance to " + Util.GetAgeName(nextAge)))
                        AdvanceToAge(nextAge);
                }
                else if (DrawButtonlRightSide("Advance to next age"))
                    OpenSubMenu(SubMenu.nextAge);
            }
            //UnrestButtons();
            if (GUI.changed)
                AUIManager.Instance.RefreshAllPanels(UIRefreshType.cUIRefreshAll);
        }



        private void ShowAdvanceAgeMenu()
        {
            GUILayout.Label("");
            foreach (var age in advanceAges)
            {
                if (DrawButtonlRightSide(Util.GetAgeName(age)))
                    AdvanceToAge(age);
            }
        }

        private void AdvanceToAge(ACard age)
        {
            Main.logger.LogMessage("AdvanceToAge " + age.ID);
            CloseEditorWindow();
            ACard baseTechFromAge = ATechManager.Instance.GetBaseTechFromAge(playerToEdit.GetAge());
            List<ACard> ageTechs = ATechManager.Instance.GetAllTechsFromAge(baseTechFromAge, filterSubtype: CardSubtype.CST_Tech);
            foreach (var tech in ageTechs)
            {
                bool valid = false;
                if (tech.CardTags.HasTag(ATechManager.cCardTagAgeAdvance))
                {
                    if (tech == age)
                        valid = true;
                }
                else
                    valid = true;

                if (valid)
                {
                    Main.logger.LogMessage("SetResearched " + tech.ID);
                    playerToEdit.SetResearched(tech.ID);
                }
            }
            GetAdvanceAges();
        }

        private void UnrestButtons()
        {
            bool unrestDisabled = gameData.GetFloatValueAsBool(ACity.cDisableUnrest);
            if (unrestDisabled)
            {
                if (GUILayout.Button("Enable population unrest"))
                    gameData.SetBaseValueAsBool(ACity.cDisableUnrest, false);
            }
            else
            {
                if (GUILayout.Button("Disable population unrest"))
                    gameData.SetBaseValueAsBool(ACity.cDisableUnrest, true);
            }
        }

        private void DrawNeutralTownMenu()
        {
            GUILayout.Label("");
            DrawLabelRightSide(selectedTile.GetDisplayName());

            if (openSubMenu == SubMenu.SelectPlayer)
            {
                ShowPlayerChoiceMenu();
                return;
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Give ownership to " + playerToEdit.GetNationName(), buttonStyle))
            {
                AMapController.Instance.ExecuteIntegration(selectedTile, playerToEdit.PlayerNum, true, true, AVassalConversionType.VCT_Envoy);
                SelectLocation(selectedLoc);
            }
            if (GUILayout.Button("Select another player", buttonStyle))
                OpenSubMenu(SubMenu.SelectPlayer);

            GUILayout.EndHorizontal();
            GUILayout.Label("");
            if (GUILayout.Button("Destroy the city", buttonStyle))
            {
                selectedTile.DestroyEntity(false);
                OpenMenu(TopMenu.Map);
            }
        }

        private void DrawCityMenu()
        {
            GUILayout.Label("");
            DrawLabelRightSide(selectedCity.GetDisplayName());
            GUILayout.BeginHorizontal();
            string pop = AStringTable.Instance.GetString("UI-CityFrame-PopulationLabel");
            if (selectedCity.GetPopulation(false) > selectedCity.GetMinimumPopulation())
            {
                if (GUILayout.Button("-1 " + pop, buttonStyle))
                    selectedCity.KillPopulation(1, false);
            }
            if (GUILayout.Button("+1 " + pop, buttonStyle))
                selectedCity.AddPopulation(1);

            GUILayout.EndHorizontal();
            if (selectedCity.PrimaryProductionQueue.IsEmpty() == false)
            {
                AProductionQueue prodQueue = selectedCity.PrimaryProductionQueue;
                AEntityInfo entityInfo = prodQueue.GetEntityInfoForQueueItem(0);
                bool project = entityInfo.HasTag(ACity.cTagCityProject);
                if (project == false)
                {
                    GUILayout.Label("");
                    if (DrawButtonlRightSide("Finish " + entityInfo.GetDisplayName(selectedCity.GetPlayer())))
                    {
                        prodQueue.GetProductionProgress(out float prodPts, out float prodPtsNeeded, out _, out _);
                        prodQueue.UseProductionPoints(prodPtsNeeded - prodPts, cityData, out _);
                    }
                }
            }
            if (selectedCity.IsVassal() && DrawButtonlRightSide("Integrate vassal"))
            {
                selectedCity.IntegrateVassal();
            }
            float unrest = cityData.GetFloatValue(ACity.cStatUnrest);
            if (unrest > 0 && DrawButtonlRightSide("Remove unrest"))
            {
                cityData.SetBaseValue(ACity.cStatUnrest, 0);
            }
            if (openSubMenu == SubMenu.SelectPlayer)
            {
                ShowPlayerChoiceMenu();
                return;
            }
            GUILayout.Label("");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Give ownership to " + playerToEdit.GetNationName(), buttonStyle))
            {
                selectedCity.ConvertRegion(playerToEdit.PlayerNum, true, AVassalConversionType.VCT_Settler);
                SelectLocation(selectedLoc);
            }
            if (GUILayout.Button("Select another player", buttonStyle))
                OpenSubMenu(SubMenu.SelectPlayer);

            GUILayout.EndHorizontal();
            GUILayout.Label("");
            if (DrawButtonlRightSide("Destroy the city"))
            {
                selectedTile.DestroyEntity(false);
                OpenMenu(TopMenu.Map);
            }
        }

        private void DrawUnitMenu()
        {
            if (selectedLoc == null)
                return;

            GUILayout.Label("");
            bool deletedUnit = false;
            float scaledHeight = 600f * Config.editorUIscale.Value;
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(scaledHeight * .85f));
            foreach (var unit in selectedUnits)
            {
                string name = unit.GetDisplayName();
                if (nameOverrides.ContainsKey(unit.TypeID))
                    name = nameOverrides[unit.TypeID];

                DrawLabelRightSide(name);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Delete unit", buttonStyle))
                {
                    unit.DestroyEntity(false, false, false, false);
                    deletedUnit = true;
                }
                if (unit.GetHealthFrac() < 1 && GUILayout.Button("Restore health", buttonStyle))
                {
                    HealStat(unit, AEntityCharacter.cStatHealth);
                }
                if (unit.GetMoraleFrac() < 1 && GUILayout.Button("Restore morale", buttonStyle))
                {
                    HealStat(unit, AEntityCharacter.cStatCommand);
                }
                if (unit.HasTag(AEntityCharacter.cTagAirUnit))
                {
                    AGameData data = unit.GetComponent<AGameData>();
                    if (data && data.GetFloatValueAsBool(AEntityCharacter.cMovedThisTurn) && GUILayout.Button("Restore movement points", buttonStyle))
                    {
                        data.SetBaseValueAsBool(AEntityCharacter.cMovedThisTurn, false);
                        SelectUnit(unit);
                    }
                }
                else if (unit.GetStatFrac(AEntityCharacter.cStatMovement) < 1 && GUILayout.Button("Restore movement points", buttonStyle))
                {
                    HealStat(unit, AEntityCharacter.cStatMovement);
                }
                float nextXP;
                int promotionLevel = unit.GetPromotionLevel(out nextXP);
                if (nextXP > 0 && GUILayout.Button("+1 experience level", buttonStyle))
                {
                    unit.AddCombatXP(nextXP);
                    ASelectable selectable = unit.GetComponent<ASelectable>();
                    AInputHandler.Instance.SetSelection(selectable, false, false, false);
                }

                GUILayout.EndHorizontal();
                GUILayout.Label("");
            }
            if (deletedUnit)
            {
                selectedUnits = selectedLoc.GetUnits(AGame.cUnitLayerNormal);
                if (selectedUnits.Count == 0)
                {
                    OpenMenu(TopMenu.Map);
                    selectedUnitOwner = null;
                }
                deletedUnit = false;
            }
            GUILayout.EndScrollView();
        }

        private static void SelectUnit(AEntityCharacter unit)
        {
            if (unit.GetPlayer() != currentPlayer)
                return;

            ASelectable selectable = unit.GetComponent<ASelectable>();
            if (selectable)
                AInputHandler.Instance.SetSelection(selectable, false, false, false);
        }

        private void DrawTileMenu()
        {
            if (selectedLoc == null)
                return;

            GUILayout.Label("");
            //int rows = Mathf.CeilToInt((float)gridSize / columns);
            if (openSubMenu == SubMenu.Terrain)
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
            else if (openSubMenu == SubMenu.Goods)
            {
                DrawGoodsChoiceMenu();
                return;
            }
            else if (openSubMenu == SubMenu.Landmark)
            {
                DrawLandMarkChoiceMenu();
                return;
            }
            else if (openSubMenu == SubMenu.SelectPlayer)
            {
                ShowPlayerChoiceMenu(true, SubMenu.Goods);
                return;
            }
            if (selectedLoc.GetIsRevealed(currentPlayer.PlayerNum))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Terrain: " + terrains[selectedLoc.TerrainType], labelStyle);
                if (DrawButtonlRightSide("Change terrain"))
                {
                    openSubMenu = SubMenu.Terrain;
                    return;
                }
                GUILayout.EndHorizontal();
                if (selectedCity)
                    return;

                GUILayout.Label("");
                GUILayout.BeginHorizontal();
                string goodsName = null;
                if (selectedTile && selectedTile.HasTag(AEntityTile.cBonusTile))
                    goodsName = "Goods: " + AGame.Instance.GetEntityTypeDisplayName(selectedTile.TypeID);

                if (goodsName != null)
                    GUILayout.Label(goodsName, labelStyle);

                if (DrawButtonlRightSide("Change goods"))
                {
                    openSubMenu = SubMenu.Goods;
                    return;
                }
                GUILayout.EndHorizontal();
                if (selectedLocValidForLandmark)
                {
                    GUILayout.Label("");
                    if (DrawButtonlRightSide("Create landmark"))
                    {
                        GetLandmarks(selectedLoc.TileCoord);
                        OpenSubMenu(SubMenu.Landmark);
                        return;
                    }
                }
                if (selectedTile)
                {
                    string name = null;
                    if (selectedTile.HasTag(AEntityTile.cTagBarbarianCamp))
                        name = "barbarian camp";
                    else if (selectedTile.HasTag(ANeutralAI.cTagNeutralCampSpawnInhibitor))
                        name = "megafauna den";
                    else if (selectedTile.HasTag(AEntityTile.cBonusTile))
                        name = "goods";
                    else if (selectedTile.IsLandmark())
                        name = "landmark";

                    if (name != null)
                    {
                        GUILayout.Label("");
                        if (DrawButtonlRightSide("Remove " + name))
                        {
                            selectedTile.DestroyEntity(false, false, true, false, true, false);
                            SelectLocation(selectedLoc);
                        }
                    }
                }
            }
            else
            {
                if (DrawButtonlRightSide("Reveal selected tile"))
                {
                    selectedLoc.RevealArea(0, currentPlayer.PlayerNum);
                }
                GUILayout.Label("");
                if (DrawButtonlRightSide("Reveal selected tile and neighbouring tiles"))
                {
                    selectedLoc.RevealArea(1, currentPlayer.PlayerNum);
                }
                GUILayout.Label("");
                if (DrawButtonlRightSide("Reveal entire map"))
                {
                    AMapController.Instance.RevealFog(true);
                }
            }
            if (landmarksRevealed == false)
            {
                GUILayout.Label("");
                if (DrawButtonlRightSide("Reveal all landmarks"))
                {
                    ACard card = ACard.FindFromText("EXPLORERS-LANDMARKS");
                    card.Play(null, null, playerToEdit);
                    landmarksRevealed = true;
                }
            }
            if (rewardCampsRevealed == false)
            {
                GUILayout.Label("");
                if (DrawButtonlRightSide("Reveal all tribal camps"))
                {
                    ACard card = ACard.FindFromText("EXPLORERS-LANDMARKS");
                    card.Choices[0].Effects[0].Target = "ENTTAG,ALLPLAYERS-" + AEntityTile.cTagRewardCamp;
                    card.Play(null, null, playerToEdit);
                    rewardCampsRevealed = true;
                }
            }
        }

        private static void HealStat(AEntityCharacter unit, string stat)
        {
            unit.HealStatFrac(stat, 1f);
            SelectUnit(unit);
        }

        private void DrawCreateUnitMenu()
        {
            GUILayout.Label("");
            if (selectedUnitOwner == null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Created unit will belong to " + playerToEdit.GetNationName(), labelStyle);
                if (GUILayout.Button("Select another player", buttonStyle))
                    OpenSubMenu(SubMenu.SelectPlayer);

                GUILayout.EndHorizontal();
            }
            else
                DrawLabelRightSide("Created unit will belong to " + selectedUnitOwner.GetNationName());

            GUILayout.Label("");
            if (openSubMenu == SubMenu.Unit)
            {
                DrawUnitChoiceMenu();
                return;
            }
            if (openSubMenu == SubMenu.SelectPlayer)
            {
                ShowPlayerChoiceMenu(false);
                return;
            }
            int playerToCheck = GetPlayerIDforCreateUnitMenu();
            if (selectedLoc.HasSpaceFor(1, playerToCheck, AGame.cUnitLayerAir))
            {
                if (DrawButtonlRightSide("Create air unit"))
                {
                    unitList = Util.GetEntityInfosWithTag(AEntityCharacter.cTagAirUnit);
                    OpenSubMenu(SubMenu.Unit);
                }
            }
            if (selectedLoc.HasSpaceFor(1, playerToCheck, AGame.cUnitLayerNormal) == false)
                return;

            if (Util.IsWaterTile(selectedLoc))
            {
                if (playerToCheck > 0 && DrawButtonlRightSide("Create civilian unit"))
                {
                    unitList = Util.GetEntityInfosWithTag(AEntityCharacter.cTerrainTagWaterMovement);
                    unitList = Util.GetEntityInfosWithAnyTag(unitList, civilNavalUnitTags);
                    OpenSubMenu(SubMenu.Unit);
                }
                if (DrawButtonlRightSide("Create military unit"))
                {
                    unitList = Util.GetEntityInfosWithAnyTag(militaryNavalUnitTags);
                    OpenSubMenu(SubMenu.Unit);
                }
            }
            else if (playerToCheck > 0 && DrawButtonlRightSide("Create civilian unit"))
            {
                unitList = Util.GetEntityInfosWithTag(ACombatManager.cTagNonCombatant);
                List<AEntityInfo> settlers = Util.GetEntityInfosWithTag("TypeSettler");
                settlers = Util.RemoveEntityInfosFromList(settlers, AEntityCharacter.cTerrainTagWaterMovement);
                unitList.AddRange(settlers);
                OpenSubMenu(SubMenu.Unit);
            }
            else if (DrawButtonlRightSide("Create leader unit"))
            {
                unitList = Util.GetEntityInfosWithTag("Leader");
                OpenSubMenu(SubMenu.Unit);
                //PrintUnits(unitList);
            }
            else if (DrawButtonlRightSide("Create line unit"))
            {
                unitList = Util.GetEntityInfosWithTag("TypeLine");
                unitList = Util.RemoveEntityInfosFromList(unitList, "Leader");
                //unitList = Util.GetEntityInfosWithTag(AEntityCharacter.cTagCombatant);
                OpenSubMenu(SubMenu.Unit);
            }
            else if (DrawButtonlRightSide("Create mobile unit"))
            {
                unitList = Util.GetEntityInfosWithTag("TypeMobile");
                OpenSubMenu(SubMenu.Unit);
            }
            else if (DrawButtonlRightSide("Create ranged unit"))
            {
                unitList = Util.GetEntityInfosWithTag("TypeRanged");
                List<AEntityInfo> commandos = Util.GetEntityInfosWithTag("TypeCommando");
                commandos = Util.RemoveEntityInfosFromList(commandos, AEntityCharacter.cTerrainTagWaterMovement);
                unitList.AddRange(commandos);
                OpenSubMenu(SubMenu.Unit);
            }
            else if (DrawButtonlRightSide("Create siege unit"))
            {
                unitList = Util.GetEntityInfosWithTag("TypeSiege");
                OpenSubMenu(SubMenu.Unit);
            }
        }

        private static int GetPlayerIDforCreateUnitMenu()
        {
            if (selectedUnitOwner != null)
                return selectedUnitOwner.PlayerNum;

            return playerToEdit.PlayerNum;
        }

        private void ShowPlayerChoiceMenu(bool onlyRealPlayer = true, SubMenu subMenuToOpen = SubMenu.None)
        {
            int columns = 4;
            GUILayout.Label("");
            GUILayout.BeginHorizontal();
            int count = 0;
            foreach (APlayer player in AGame.Instance.Players)
            {
                if (player.IsEliminated || player == playerToEdit)
                    continue;

                if (onlyRealPlayer && player.IsReal() == false)
                    continue;

                if (count % columns == 0 && count != 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                string name = player.GetNationName();
                if (GUILayout.Button(name, buttonStyle))
                {
                    if (subMenuToOpen == SubMenu.None)
                        CloseSubmenu();
                    else
                        OpenSubMenu(subMenuToOpen);

                    playerToEdit = player;
                    GetAdvanceAges();
                }
                count++;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawLabelRightSide(string s)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(s, labelStyle);
            GUILayout.EndHorizontal();
        }

        private bool DrawButtonlRightSide(string s)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            float scaledWidth = 365f * Config.editorUIscale.Value;
            bool clicked = GUILayout.Button(s, buttonStyle, GUILayout.Width(scaledWidth));
            GUILayout.EndHorizontal();
            return clicked;
        }

        private void DrawLandMarkChoiceMenu()
        {
            int columns = 4;
            string landMarkCheckText = "Check if landMark is valid for map";
            bool clicked = false;
            if (landMarkCheck)
            {
                clicked = DrawSelectedButton(landMarkCheckText);
            }
            else
                clicked = GUILayout.Button(landMarkCheckText, buttonStyle);

            if (clicked)
            {
                landMarkCheck = !landMarkCheck;
            }
            GUILayout.BeginHorizontal();
            int count = 0;
            foreach (var kv in landmarks)
            {
                if (landMarkCheck && validLandmarks.Contains(kv.Key) == false)
                    continue;

                if (count % columns == 0 && count != 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                if (GUILayout.Button(kv.Value, buttonStyle))
                {
                    AEntityTile tile = ALandmarkManager.Instance.CreateLandmark(selectedLoc.TileCoord, kv.Key);
                    SelectLocation(selectedLoc);
                }
                count++;
            }
            GUILayout.EndHorizontal();
        }

        public bool ShouldNotSpawnLandmark(string id)
        {
            return id == "LM_WASTELAND_RUINS_A" || id == "LM_WASTELAND_RUINS_B" || id == "LM_WASTELAND_RUINS_C" || id == "LM_WASTELAND_RUINS_D" || id == "LM_WASTELAND_RUINS_REPEATABLE2" || id.StartsWith("SHARED_LANDMARK_RULES") || id.StartsWith("LM_QUEST");
        }

        public void GetLandmarks(ATileCoord coord)
        {
            landmarks.Clear();
            validLandmarks.Clear();
            foreach (string id in ALandmarkManager.Instance.LandmarkInfo.Keys)
            {
                if (ShouldNotSpawnLandmark(id))
                    continue;

                string name = GetLandmarkName(id);
                landmarks[id] = name;
                ALandmark landmark = ALandmarkManager.Instance.LandmarkInfo[id];
                if (landmark != null && landmark.IsValid(coord))
                    validLandmarks.Add(id);
            }
        }

        private string GetLandmarkName(string id)
        {
            ALandmark landmark = ALandmarkManager.Instance.GetLandmarkInfo(id);
            string key = string.Format("Landmark-{0}-DisplayName", landmark.ID);
            return AStringTable.Instance.GetString(key);
        }

        private void DrawUnitChoiceMenu()
        {
            int columns = 4;
            float scaledHeight = 600f * Config.editorUIscale.Value;
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(scaledHeight * .75f));
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
                if (nameOverrides.ContainsKey(id))
                    name = nameOverrides[id];

                if (GUILayout.Button(name, buttonStyle))
                {
                    AEntityCharacter unit = AMapController.Instance.CreateUnit(id, selectedLoc.TileCoord, playerToEdit.PlayerNum);
                    if (CanCreateUnit() == false)
                        OpenMenu(TopMenu.Units);

                    SelectLocation(selectedLoc);
                    SelectUnit(unit);
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
                if (goodsPlayerCheck && loc.GetTerrainType().IsResourceTileTypeValid(kv.Key, AGame.Instance.CurrPlayer.PlayerNum) == false)
                    continue;

                count++;
            }
            return count;
        }

        private void DrawGoodsChoiceMenu()
        {
            int columns = 6;
            int count = 0;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Select another player", buttonStyle))
                OpenSubMenu(SubMenu.SelectPlayer);

            string goodsPlayerCheckText = "Check if goods are valid for " + playerToEdit.GetNationName();
            bool clickedGoodsPlayerCheck = false;
            if (goodsPlayerCheck)
                clickedGoodsPlayerCheck = DrawSelectedButton(goodsPlayerCheckText);
            else
                clickedGoodsPlayerCheck = GUILayout.Button(goodsPlayerCheckText, buttonStyle);

            if (clickedGoodsPlayerCheck)
                goodsPlayerCheck = !goodsPlayerCheck;
            GUILayout.EndHorizontal();
            string goodsTerrainCheckText = "Check if goods are valid for terrain";
            bool clickedGoodsTerrainCheck = false;
            if (goodsTerrainCheck)
                clickedGoodsTerrainCheck = DrawSelectedButtonRightSide(goodsTerrainCheckText);
            else
                clickedGoodsTerrainCheck = DrawButtonlRightSide(goodsTerrainCheckText);

            if (clickedGoodsTerrainCheck)
            {
                goodsTerrainCheck = !goodsTerrainCheck;
            }
            GUILayout.Label("");
            GUILayout.BeginHorizontal();
            foreach (var kv in goods)
            {
                if (goodsPlayerCheck && Util.IsGoodsTypeValidForPlayer(kv.Key, playerToEdit.PlayerNum) == false)
                    continue;

                if (goodsTerrainCheck && Util.IsGoodsTypeValidForTerrain(selectedLoc.GetTerrainType(), kv.Key) == false)
                    continue;

                if (selectedTile && selectedTile.TypeID == kv.Value)
                    continue;

                if (count % columns == 0 && count != 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                if (GUILayout.Button(kv.Value, buttonStyle))
                {
                    selectedTile = AMapController.Instance.SpawnTile(kv.Key, selectedLoc, selectedLoc.PlayerNum);
                    CloseSubmenu();
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

                if (GUILayout.Button(kv.Value, buttonStyle))
                {
                    selectedLoc.ChangeTerrain(kv.Key);
                    SelectLocation(AInputHandler.Instance.SelectedLocation);
                    CloseSubmenu();
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

                if (GUILayout.Button(kv.Value, buttonStyle))
                {
                    func(kv.Key);
                    SelectLocation(AInputHandler.Instance.SelectedLocation);
                    CloseSubmenu();
                }
                count++;
            }
            GUILayout.EndHorizontal();
        }

    }
}
