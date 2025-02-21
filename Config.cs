using BepInEx.Configuration;
using CPrompt;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace Ingame_Editor
{
    internal class Config
    {
        public static ConfigEntry<float> editorUIscale;
        public static ConfigEntry<bool> tileGoodsCheck;
        public static ConfigEntry<bool> enableConsole;
        public static ConfigEntry<bool> showGameDataVarsInTooltip;
        public static ConfigEntry<bool> landmarkCheck;
        public static ConfigEntry<bool> showTileCoordInTooltip;
        public static ConfigEntry<KeyCode> hotkey;


        public static void Bind()
        {
            editorUIscale = Main.config.Bind("", "Editor UI scale", 1f);
            tileGoodsCheck = Main.config.Bind("", "Check if tile goods are valid for tile and player when placing them", true);
            landmarkCheck = Main.config.Bind("", "Check if landmark is valid for tile when placing it", true);
            enableConsole = Main.config.Bind("", "Enable developer console", true);
            enableConsole.SettingChanged += EnableConsoleChanged;
            showTileCoordInTooltip = Main.config.Bind("", "Show tile coordinates in tooltip", false);
            showGameDataVarsInTooltip = Main.config.Bind("", "Show GameData variables in tooltip", false);
            showGameDataVarsInTooltip.SettingChanged += ShowGameDataVarsInTooltipChanged;
            hotkey = Main.config.Bind("", "Editor window hotkey", KeyCode.F5);

        }

        private static void EnableConsoleChanged(object sender, EventArgs e)
        {
            ADevConfig.EnableConsole = enableConsole.Value;
            //Main.logger.LogDebug("OnEnableConsoleChanged " + ADevConfig.EnableConsole);
        }

        private static void ShowGameDataVarsInTooltipChanged(object sender, EventArgs e)
        {
            AGameConstants.Instance.Cheats.DebugGameDataMouseover = showGameDataVarsInTooltip.Value;
            //Main.logger.LogDebug("ShowGameDataVarsInTooltipChanged " + AGameConstants.Instance.Cheats.DebugGameDataMouseover);

        }

    }
}
