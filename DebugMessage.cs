using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Ingame_Editor
{
    internal class DebugMessage : MonoBehaviour
    {
        private string message = "";
        private bool showMessage = false;
        public static DebugMessage instance;
        Texture2D background;
        //float messageTime;

        void Start()
        {
            instance = this;
            background = Util.MakeTexture(2, 2, Color.black);
        }

        public void ShowMessage(string msg, float timeOnScreen = 2f)
        {
            if (showMessage && msg == message)
                return;

            message = msg;
            showMessage = true;
            StartCoroutine(HideMessageAfterDelay(message, timeOnScreen));
        }

        void OnGUI()
        {
            if (showMessage == false)
                return;

            GUIStyle messageStyle = new GUIStyle(GUI.skin.label);
            messageStyle.alignment = TextAnchor.MiddleCenter;
            messageStyle.fontSize = 30;
            messageStyle.normal.textColor = Color.white;
            GUIStyle transparentStyle = new GUIStyle();
            transparentStyle.normal.background = background;
            // Calculate the position and size of the message box
            float width = 400;
            float height = 100;
            float x = (Screen.width - width) / 2;
            float y = (Screen.height - height) / 2;

            // Draw a transparent box (optional, if you want a completely invisible container)
            GUI.Box(new Rect(x, y, width, height), "", transparentStyle);

            // Draw the message
            GUI.Label(new Rect(x, y, width, height), message, messageStyle);
            //if (GUI.Button(new Rect(x + width / 2 - 50, y + height - 30, 100, 20), "OK"))
        }

        private IEnumerator HideMessageAfterDelay(string msg, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (message == msg)
                showMessage = false;
        }


    }
}
