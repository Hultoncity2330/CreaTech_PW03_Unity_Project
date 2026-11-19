using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(FreeCamera))]
public class FreeCameraHUD : MonoBehaviour
{
    private FreeCamera cameraControls;
    private GUIStyle headingStyle;
    private GUIStyle textStyle;
    private bool showHelp = true;

    private void Awake()
    {
        cameraControls = GetComponent<FreeCamera>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            showHelp = !showHelp;
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || !cameraControls.isActiveAndEnabled)
            return;

        if (!showHelp && cameraControls.IsCaptured)
            return;

        if (headingStyle == null)
            CreateStyles();

        float width = Mathf.Min(370f, Screen.width - 24f);
        float panelX = (Screen.width - width) / 2f;
        Rect panel = new Rect(panelX, 12f, width, showHelp ? 145f : 46f);
        Color previousColor = GUI.color;
        Color previousContentColor = GUI.contentColor;
        GUI.contentColor = Color.white;

        GUI.color = new Color(0.08f, 0.11f, 0.13f, 0.92f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = new Color(0.58f, 0.85f, 0.66f);
        GUI.DrawTexture(new Rect(panel.x, panel.y, 3f, panel.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        string title = cameraControls.IsCaptured ? "Caméra libre" : "Cliquer dans la vue pour piloter";
        float x = panel.x + 14f;
        float contentWidth = panel.width - 26f;
        WhiteLabel(new Rect(x, panel.y + 9f, contentWidth, 27f), title, headingStyle);

        if (showHelp)
        {
            WhiteLabel(new Rect(x, panel.y + 38f, contentWidth, 22f),
                $"{cameraControls.MovementKeys} ou flèches   Déplacement", textStyle);
            WhiteLabel(new Rect(x, panel.y + 63f, contentWidth, 22f),
                "Espace / Ctrl   Monter / descendre     Maj   Accélérer", textStyle);
            WhiteLabel(new Rect(x, panel.y + 88f, contentWidth, 22f),
                "Souris   Regarder     Échap   Libérer     F1   Aide", textStyle);
            WhiteLabel(new Rect(x, panel.y + 113f, contentWidth, 22f),
                "Afficher/Masquer les volets de stats : Tab", textStyle);
        }

        GUI.color = previousColor;
        GUI.contentColor = previousContentColor;
    }

    private void CreateStyles()
    {
        headingStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, richText = true };
        headingStyle.normal.textColor = Color.white;
        headingStyle.hover.textColor = Color.white;
        headingStyle.active.textColor = Color.white;
        textStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true };
        textStyle.normal.textColor = Color.white;
        textStyle.hover.textColor = Color.white;
        textStyle.active.textColor = Color.white;
    }
    // Explicit rich-text white, independent of the GUI skin's text tint.
    private static void WhiteLabel(Rect rect, string text, GUIStyle style)
    {
        Color previousColor = GUI.color;
        Color previousContentColor = GUI.contentColor;
        GUI.color = Color.white;
        GUI.contentColor = Color.white;
        GUI.Label(rect, "<color=#FFFFFFFF>" + text + "</color>", style);
        GUI.color = previousColor;
        GUI.contentColor = previousContentColor;
    }

}
