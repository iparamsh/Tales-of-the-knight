using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class HUDOverlay : MonoBehaviour
{
    private PlayerController playerController;
    private PlayerCombat playerCombat;

    private GUIStyle textStyle;
    private GUIStyle shadowStyle;
    private GUIStyle controlsStyle;
    private GUIStyle controlsShadowStyle;

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        playerCombat = FindFirstObjectByType<PlayerCombat>();
    }

    void OnGUI()
    {
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                richText = true
            };
            textStyle.normal.textColor = Color.white;
            textStyle.hover.textColor = Color.white;

            shadowStyle = new GUIStyle(textStyle);
            shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.7f);
            shadowStyle.hover.textColor = new Color(0f, 0f, 0f, 0.7f);

            controlsStyle = new GUIStyle(textStyle)
            {
                fontSize = 10
            };
            controlsStyle.normal.textColor = new Color(1f, 1f, 1f, 0.35f);
            controlsStyle.hover.textColor = new Color(1f, 1f, 1f, 0.35f);

            controlsShadowStyle = new GUIStyle(controlsStyle);
            controlsShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.2f);
            controlsShadowStyle.hover.textColor = new Color(0f, 0f, 0f, 0.2f);
        }

        DrawHeals();
        DrawControls();
    }

    void DrawHeals()
    {
        if (playerController == null) return;

        string text = $"Heals: {playerController.CurrentHeals} / {playerController.MaxHeals}";
        float x = 20f;
        float y = Screen.height - 60f;

        GUI.Label(new Rect(x + 1, y + 1, 200, 30), text, shadowStyle);
        GUI.Label(new Rect(x, y, 200, 30), text, textStyle);
    }

    void DrawControls()
    {
        string controls = BuildControlsText();

        float width = 200f;
        float lineHeight = 14f;
        int lineCount = controls.Split('\n').Length;
        float height = lineCount * lineHeight;
        float x = Screen.width - width - 6f;
        float y = Screen.height - height - 6f;

        GUI.Label(new Rect(x + 1, y + 1, width, height), controls, controlsShadowStyle);
        GUI.Label(new Rect(x, y, width, height), controls, controlsStyle);
    }

    string BuildControlsText()
    {
        var sb = new StringBuilder();

        if (playerController != null)
        {
            sb.AppendLine("Move:          " + Binding(playerController.MoveAction));
            sb.AppendLine("Jump:          " + Binding(playerController.JumpAction));
            sb.AppendLine("Roll:          " + Binding(playerController.RollAction));
            sb.AppendLine("Sprint:        " + Binding(playerController.SprintAction));
            sb.AppendLine("Heal:          " + Binding(playerController.HealAction));
        }

        if (playerCombat != null)
        {
            sb.AppendLine("Light Attack:  " + Binding(playerCombat.LightAttackAction));
            sb.AppendLine("Heavy Attack:  " + Binding(playerCombat.HeavyAttackAction));
            sb.AppendLine("Plunge:        " + Binding(playerCombat.PlungeAction));
        }

        return sb.ToString().TrimEnd();
    }

    static string Binding(InputAction action)
    {
        if (action == null) return "?";
        string s = action.GetBindingDisplayString();
        return string.IsNullOrEmpty(s) ? "?" : s;
    }
}
