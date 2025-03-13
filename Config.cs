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
        public static ConfigEntry<bool> enableConsole;
        public static ConfigEntry<bool> showGameDataVarsInTooltip;
        public static ConfigEntry<bool> showTileCoordInTooltip;
        public static ConfigEntry<bool> showUnitIDinTooltip;
        public static ConfigEntry<KeyCode> editorHotkey;


        public static void Bind()
        {
            editorUIscale = Main.config.Bind("", "Editor UI scale", 1f);
            enableConsole = Main.config.Bind("", "Enable developer console", true);
            enableConsole.SettingChanged += EnableConsoleChanged;
            showTileCoordInTooltip = Main.config.Bind("", "Show tile coordinates in tooltip", false);
            showUnitIDinTooltip = Main.config.Bind("", "Show unit ID in tooltip", false);
            showUnitIDinTooltip.SettingChanged += showUnitIDinTooltipChanged;
            showGameDataVarsInTooltip = Main.config.Bind("", "Show GameData variables in tooltip", false);
            showGameDataVarsInTooltip.SettingChanged += ShowGameDataVarsInTooltipChanged;
            editorHotkey = Main.config.Bind("", "Editor window hotkey", KeyCode.F5);

        }

        private static void showUnitIDinTooltipChanged(object sender, EventArgs e)
        {
            ADevConfig.ShowDebugUnitID = showUnitIDinTooltip.Value;
            //Main.logger.LogDebug("OnEnableConsoleChanged " + ADevConfig.EnableConsole);
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
