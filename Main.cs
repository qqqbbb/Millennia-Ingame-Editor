using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CPrompt;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ingame_Editor
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Main : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "qqqbbb.Millennia.InGameEditor";
        public const string PLUGIN_NAME = "InGame Editor";
        public const string PLUGIN_VERSION = "1.0.0";

        public static ConfigFile config;
        public static ManualLogSource logger;
        //PayUpkeepPenalty
        //CreateWorldOverlayText
        //aentity.CurrentLoc.RevealArea(1, result);
        //test selectedLoc.GetUnits(AGame.cUnitLayerNormal);

        private void Awake()
        {
            Harmony harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll();
            //Harmony.CreateAndPatchAll(typeof(EditorWindow));
            config = this.Config;
            Ingame_Editor.Config.Bind();
            logger = Logger;
            Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");
        }

        private void Start()
        {
            //AGame.Instance.gameObject.AddComponent<EditorWindow>();
            //AGame.Instance.SetIsCheating();
        }


        [HarmonyPatch(typeof(AGame), "Start")]
        class AGame_Start_Patch
        {
            public static void Postfix(AGame __instance)
            {

                //logger.LogInfo("AGame Start " + AGameConstants.Instance.Cheats.DebugGameDataMouseover);
                //AGameConstants.Instance.Cheats.DebugGameDataMouseover = true;
                //if (__instance.GetGameType() == AGameType.AGT_SinglePlayer)
                {
                    __instance.gameObject.AddComponent<Editor>();
                    __instance.gameObject.AddComponent<DebugMessage>();
                    AGameConstants.Instance.Cheats.DebugGameDataMouseover = Ingame_Editor.Config.showGameDataVarsInTooltip.Value;
                    ADevConfig.EnableConsole = Ingame_Editor.Config.enableConsole.Value;
                }
            }
        }

        //[HarmonyPatch(typeof(ATechTreeDialog), "SetPlayer")]
        class AResearchPresentAge_Start_Patch
        {
            public static void Postfix(ATechTreeDialog __instance)
            {
                logger.LogInfo("ATechTreeDialog SetPlayer");
                if (ATechManager.Instance == null)
                {
                    logger.LogInfo("  ATechManager == null");
                    return;
                }
                //if (__instance.BaseTechCard == null)
                //{
                //    logger.LogInfo(" InitializeToAge BaseTechCard == null");
                //    return;
                //}
                //List<ACard> techs = ATechManager.Instance.GetPossibleAgesFrom(__instance.BaseTechCard);
                //foreach (var t in techs)
                {
                    //logger.LogDebug(" " + t);
                }
                //AGame.Instance.gameObject.AddComponent<EditorWindow>();
                //AGame.Instance.SetIsCheating();
            }
        }

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(AResearchFutureAge), "OnTechButtonClicked")]
        public static bool Override_OnTechButtonClicked(ref AResearchFutureAge __instance, ACard techCard)
        {
            if (!AInputHandler.IsShiftActive())
                return true;
            Traverse.Create((object)__instance).Field("ResearchDialog").GetValue<AResearchDialog>().ForceResearch(techCard, true);
            return false;
        }

    }
}
