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

            // Panel (top-left anchor, auto-height)
            var panelGO  = new GameObject("Panel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin        = new Vector2(0, 1);
            panelRect.anchorMax        = new Vector2(0, 1);
            panelRect.pivot            = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta        = new Vector2(340, 0);   // width fixed, height driven by content

            var bg = panelGO.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.6f);

            var layout = panelGO.AddComponent<VerticalLayoutGroup>();
            layout.padding          = new RectOffset(10, 10, 8, 8);
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth      = true;
            layout.childControlHeight     = true;

            var fitter = panelGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Text (single child drives the panel height)
            var textGO   = new GameObject("Text");
            textGO.transform.SetParent(panelGO.transform, false);
            textGO.AddComponent<RectTransform>();

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize           = 13;
            tmp.color              = Color.white;
            tmp.alignment          = TextAlignmentOptions.TopLeft;
            tmp.enableWordWrapping = false;
            tmp.text               = "Connecting...";

            // HUD component
            var hud = canvasGO.AddComponent<ConnectionHUD>();
            var soField = typeof(ConnectionHUD).GetField("_state",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var txField = typeof(ConnectionHUD).GetField("_text",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            soField?.SetValue(hud, stateSO);
            txField?.SetValue(hud, tmp);

            // Auto-wire SO to FusionConnectionManager if it's in the scene
            var fcm = Object.FindFirstObjectByType<FusionConnectionManager>();
            if (fcm != null)
            {
                var fcmField = typeof(FusionConnectionManager).GetField("_connectionState",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                fcmField?.SetValue(fcm, stateSO);
                EditorUtility.SetDirty(fcm);
                Debug.Log("[ConnectionHUDBuilder] Auto-wired ConnectionStateSO to FusionConnectionManager.");
            }
            else
            {
                Debug.LogWarning("[ConnectionHUDBuilder] FusionConnectionManager not found in scene — wire ConnectionStateSO manually.");
            }

            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Connection HUD");
            Selection.activeGameObject = canvasGO;
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
