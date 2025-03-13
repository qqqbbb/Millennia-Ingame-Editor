using CPrompt;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace Ingame_Editor
{
    internal class Patches
    {
        [HarmonyPatch(typeof(AInputHandler))]
        class AInputHandler_Patch
        {
            [HarmonyPrefix, HarmonyPatch("DoNormalCursorWorldInteraction")]
            public static bool DoNormalCursorWorldInteractionPrefix(AInputHandler __instance, ref bool __result)
            { // dont allow selecting world entity when editor open
                return !Editor.mouseCursorOnEditorWindow;
            }
            [HarmonyPrefix, HarmonyPatch("DoCitySelection")]
            public static bool DoCitySelectionPrefix(AInputHandler __instance)
            {
                //DebugMessage.instance.ShowMessage("DoCitySelection");
                if (Editor.mouseCursorOnEditorWindow == false)
                    Editor.CloseEditorWindow();

                return !Editor.mouseCursorOnEditorWindow;
            }
            [HarmonyPrefix, HarmonyPatch("DoMiscKeys")]
            public static bool DoMiscKeysPrefix(AInputHandler __instance)
            { // dont show game menu
                return !Editor.showingEditorWindow;
            }
            [HarmonyPrefix, HarmonyPatch("ClearSelection")]
            public static void SetSelectionPostfix(AInputHandler __instance)
            {
                //DebugMessage.instance.ShowMessage("ClearSelection ");
                Editor.ClearSelection();
            }
            [HarmonyPostfix, HarmonyPatch("SetSelectedLocation")]
            public static void SetSelectedLocationPostfix(AInputHandler __instance, ALocation locToSelect)
            {
                //DebugMessage.instance.ShowMessage("SetSelection ");
                //Main.logger.LogInfo("SetSelection");
                if (locToSelect)
                    Editor.SelectLocation(locToSelect);
            }
            [HarmonyPrefix, HarmonyPatch("DoZoom")]
            public static bool SelectTilePrefix(AInputHandler __instance)
            {
                return !Editor.mouseCursorOnEditorWindow;
            }
            [HarmonyPrefix, HarmonyPatch("SetSelection", new Type[] { typeof(ALocation) })]
            public static bool SetSelectionPrefix(AInputHandler __instance)
            {
                return !Editor.mouseCursorOnEditorWindow;
            }
        }

        [HarmonyPatch(typeof(ACommandColumnDomainEntry), "OnDomainEntryPowerUseButtonClicked")]
        class ACommandColumnDomainEntry_OnDomainEntryPowerUseButtonClicked_Patch
        {
            public static bool Prefix(ACommandColumnDomainEntry __instance)
            {
                return !Editor.mouseCursorOnEditorWindow;
            }
        }

        [HarmonyPatch(typeof(ACommandColumnModule))]
        class ACommandColumnModule_Patch
        {
            [HarmonyPrefix, HarmonyPatch("OnModuleButtonPressed")]
            public static bool OnModuleButtonPressedPrefix(ACommandColumnModule __instance)
            {
                return !Editor.mouseCursorOnEditorWindow;
            }
            [HarmonyPrefix, HarmonyPatch("OnRushButtonPressed")]
            public static bool OnRushButtonPressedPrefix(ACommandColumnModule __instance)
            {
                return !Editor.mouseCursorOnEditorWindow;
            }
            [HarmonyPrefix, HarmonyPatch("OnEmptyDomainButtonPressed")]
            public static bool OnEmptyDomainButtonPressedrefix(ACommandColumnModule __instance)
            {
                return !Editor.mouseCursorOnEditorWindow;
            }
        }

        [HarmonyPatch(typeof(AUIManager))]
        class AUIManager_Patch
        {
            [HarmonyPrefix, HarmonyPatch("ToggleOrSwitchDomainDialog")]
            public static bool ToggleOrSwitchDomainDialogPrefix(AUIManager __instance, string domain)
            {
                return !Editor.mouseCursorOnEditorWindow;
            }
            [HarmonyPrefix, HarmonyPatch("OpenDiplomacyOverview")]
            public static bool OpenDiplomacyOverviewPrefix(AUIManager __instance)
            {
                if (Editor.mouseCursorOnEditorWindow == false)
                    Editor.CloseEditorWindow();

                return !Editor.mouseCursorOnEditorWindow;
            }
            //[HarmonyPostfix, HarmonyPatch("OnTimelineButtonPressed")]
            public static void OnTimelineButtonPressedPostfix(AUIManager __instance)
            {
                Editor.CloseEditorWindow();
            }
        }

        [HarmonyPatch(typeof(ATooltipController))]
        class ATooltipController_Patch
        {
            [HarmonyPrefix, HarmonyPatch("SetMouseoverText")]
            public static bool ToggleOrSwitchDomainDialogPrefix(ATooltipController __instance)
            {
                return !Editor.mouseCursorOnEditorWindow;
            }
        }

        [HarmonyPatch(typeof(ACityFrame))]
        class ACityFrame_Patch
        {
            [HarmonyPrefix, HarmonyPatch("OnFlagButtonClicked")]
            public static bool OnFlagButtonClickedPrefix(ACityFrame __instance)
            {
                return !Editor.mouseCursorOnEditorWindow;
            }
        }

        [HarmonyPatch(typeof(AGameAlert))]
        class AGameAlert_Patch
        {
            [HarmonyPrefix, HarmonyPatch("DoTheThing")]
            public static bool DoTheThingPrefix(AGameAlert __instance, bool fromInput, bool fromWorld)
            { // combat replay
                if (fromWorld)
                    return !Editor.mouseCursorOnEditorWindow;

                return true;
            }
        }


        [HarmonyPatch(typeof(ALocation))]
        class ALocation_Patch
        {
            [HarmonyPostfix, HarmonyPatch("AddTerrainTypeTooltip")]
            public static void AddTerrainTypeTooltipPostfix(ALocation __instance, ref StringBuilder sb)
            {
                if (Config.showTileCoordInTooltip.Value == false)
                    return;

                string newLine = Environment.NewLine;
                sb.Remove(sb.Length - newLine.Length, newLine.Length);
                sb.Remove(sb.Length - newLine.Length, newLine.Length);
                sb.Append("   " + __instance.TileCoord.X + "  " + __instance.TileCoord.Y);
                sb.AppendLine();
            }
        }

        [HarmonyPatch(typeof(AResearchDialog))]
        class AResearchDialog_Patch
        {
            [HarmonyPostfix, HarmonyPatch("OpenDialog")]
            public static void OpenDialogPostfix(AResearchDialog __instance)
            {
                Editor.CloseEditorWindow();
            }
        }

        [HarmonyPatch(typeof(ABuilderButtonPanel))]
        class ABuilderButtonPanel_Patch
        {
            [HarmonyPrefix, HarmonyPatch("OnBuildImprovementButtonPressed")]
            public static bool OnBuildImprovementButtonPressedPrefix(ABuilderButtonPanel __instance)
            {
                //DebugMessage.instance.ShowMessage("OnBuildImprovementButtonPressed");
                return !Editor.mouseCursorOnEditorWindow;
            }
            //[HarmonyPrefix, HarmonyPatch("OnBuildImprovementPickerButtonPressed")]
            public static void OnBuildImprovementPickerButtonPressedPrefix(ABuilderButtonPanel __instance)
            {
                DebugMessage.instance.ShowMessage("OnBuildImprovementPickerButtonPressed");
            }
        }

        [HarmonyPatch(typeof(AActionPanel))]
        class ACommand_Patch
        {
            [HarmonyPrefix, HarmonyPatch("OnCardButtonPressed")]
            public static bool PlayPrefix(AActionPanel __instance, ACard c)
            { // unit panel buttons
                //Main.logger.LogDebug("OnCardButtonPressed " + c.ID);
                //DebugMessage.instance.ShowMessage("CommandPlayCardInternal " + __instance.ID);
                return !Editor.mouseCursorOnEditorWindow;
            }
            //[HarmonyPrefix, HarmonyPatch("OnTileActionButtonPressed")]
            public static void OnTileActionButtonPressedPrefix(AActionPanel __instance, string tileActionText)
            {
                Main.logger.LogDebug("AActionPanel OnTileActionButtonPressed " + tileActionText);
                //DebugMessage.instance.ShowMessage("CommandPlayCardInternal " + __instance.ID);
                //return !Editor.mouseCursorOnEditorWindow;
            }
            //[HarmonyPrefix, HarmonyPatch("SetActions")]
            public static void SetActionsPrefix(AActionPanel __instance, List<string> actionNames)
            {
                foreach (var t in actionNames)
                {
                    Main.logger.LogDebug("SetActions " + t);
                }
                //DebugMessage.instance.ShowMessage("CommandPlayCardInternal " + __instance.ID);
                //return !Editor.mouseCursorOnEditorWindow;
            }
        }

        [HarmonyPatch(typeof(AArmyPanel))]
        class ACommandManager_Patch
        {
            //[HarmonyPrefix, HarmonyPatch("CreateCardActionButton")]
            public static void ExecuteCommandsPrefix(AArmyPanel __instance, ACard actionCard)
            {
                Main.logger.LogDebug("CreateCardActionButton " + actionCard.ID);
                //DebugMessage.instance.ShowMessage("ExecuteCommands " + __instance);
                //return !Editor.mouseCursorOnEditorWindow;
            }
            [HarmonyPrefix, HarmonyPatch("OnSecondaryUnitCardClicked")]
            public static void OnSecondaryUnitCardClickedPrefix(AArmyPanel __instance, AEntityCharacter forUnit)
            {
                Main.logger.LogDebug("OnSecondaryUnitCardClicked " + forUnit.TypeID);
                //DebugMessage.instance.ShowMessage("ExecuteCommands " + __instance);
                //return !Editor.mouseCursorOnEditorWindow;
            }
            [HarmonyPrefix, HarmonyPatch("OnTileActionButtonPressed")]
            public static bool CreateArmyCardPrefix(AArmyPanel __instance, string tileActionText)
            { // promote unit button
                Main.logger.LogDebug("AArmyPanel OnTileActionButtonPressed " + tileActionText);
                //DebugMessage.instance.ShowMessage("ExecuteCommands " + __instance);
                return !Editor.mouseCursorOnEditorWindow;
            }
        }





    }
}
