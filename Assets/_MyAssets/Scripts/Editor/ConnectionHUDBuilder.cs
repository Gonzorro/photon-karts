using PhotonKarts.Networking;
using PhotonKarts.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PhotonKarts.Editor
{
    /// <summary>
    /// Creates the ConnectionHUD Canvas in the active scene.
    /// Menu: PhotonKarts → Create Connection HUD
    /// </summary>
    public static class ConnectionHUDBuilder
    {
        [MenuItem("PhotonKarts/Create Connection HUD")]
        public static void CreateConnectionHUD()
        {
            var stateSO = FindOrPromptStateSO();

            // Canvas
            var canvasGO = new GameObject("ConnectionHUD");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Panel (top-left anchor)
            var panelGO        = new GameObject("Panel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRect      = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot     = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta        = new Vector2(320, 320);

            var bg = panelGO.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.55f);

            // Text
            var textGO   = new GameObject("Text");
            textGO.transform.SetParent(panelGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin        = Vector2.zero;
            textRect.anchorMax        = Vector2.one;
            textRect.offsetMin        = new Vector2(8, 8);
            textRect.offsetMax        = new Vector2(-8, -8);

            var tmp              = textGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize         = 14;
            tmp.color            = Color.white;
            tmp.alignment        = TextAlignmentOptions.TopLeft;
            tmp.overflowMode     = TextOverflowModes.Overflow;
            tmp.enableWordWrapping = false;
            tmp.text             = "Connecting...";

            // HUD component
            var hud = canvasGO.AddComponent<ConnectionHUD>();
            var so  = typeof(ConnectionHUD).GetField("_state",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var tx  = typeof(ConnectionHUD).GetField("_text",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            so?.SetValue(hud, stateSO);
            tx?.SetValue(hud, tmp);

            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Connection HUD");
            Selection.activeGameObject = canvasGO;

            Debug.Log("[ConnectionHUDBuilder] HUD created. Wire ConnectionStateSO to FusionConnectionManager if not already done.");
        }

        private static ConnectionStateSO FindOrPromptStateSO()
        {
            var guids = AssetDatabase.FindAssets("t:ConnectionStateSO");
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<ConnectionStateSO>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));

            // None found — create one next to the script
            var so = ScriptableObject.CreateInstance<ConnectionStateSO>();
            AssetDatabase.CreateAsset(so, "Assets/_MyAssets/ConnectionStateSO.asset");
            AssetDatabase.SaveAssets();
            Debug.Log("[ConnectionHUDBuilder] Created ConnectionStateSO at Assets/_MyAssets/ConnectionStateSO.asset");
            return so;
        }
    }
}
