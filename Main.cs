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
        public const string PLUGIN_VERSION = "1.1.0";

        public static ConfigFile config;
        public static ManualLogSource logger;


        private void Awake()
        {
            Harmony harmony = new Harmony(PLUGIN_GUID);
            harmony.PatchAll();
            config = this.Config;
            Ingame_Editor.Config.Bind();
            logger = Logger;
            Logger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");
        }


        [HarmonyPatch(typeof(AGame), "Start")]
        class AGame_Start_Patch
        {
            public static void Postfix(AGame __instance)
            {
                __instance.gameObject.AddComponent<Editor>();
                //__instance.gameObject.AddComponent<DebugMessage>();
                AGameConstants.Instance.Cheats.DebugGameDataMouseover = Ingame_Editor.Config.showGameDataVarsInTooltip.Value;
                ADevConfig.EnableConsole = Ingame_Editor.Config.enableConsole.Value;
                ADevConfig.ShowDebugUnitID = Ingame_Editor.Config.showUnitIDinTooltip.Value;
            }
        }


    }
}
