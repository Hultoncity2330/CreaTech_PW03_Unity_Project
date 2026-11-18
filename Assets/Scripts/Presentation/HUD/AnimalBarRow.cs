using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnimalBarRow : MonoBehaviour
{
    private AgentBehaviour animal;
    private TMP_Text nameText;
    private TMP_Text energyText;
    private TMP_Text hungerText;
    private RectTransform energyFill;
    private RectTransform hungerFill;

    public void Initialize(AgentBehaviour agent, string displayName)
    {
        animal = agent;
        nameText = CreateText("Name", displayName);
        nameText.alignment = TextAlignmentOptions.Left;
        nameText.fontStyle = FontStyles.Bold;

        energyFill = CreateBar("Energy", new Color(1f, 0.85f, 0.15f));
        hungerFill = CreateBar("Hunger", new Color(1f, 0.48f, 0.12f));

        // Percentages are separate from (and outside) their bars.
        energyText = CreateText("EnergyPercent", "100%");
        hungerText = CreateText("HungerPercent", "100%");
    }

    public void SetLayout(float x, float y, float width, float height)
    {
        RectTransform row = (RectTransform)transform;
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(0f, 1f);
        row.pivot = new Vector2(0f, 1f);
        row.anchoredPosition = new Vector2(x, -y);
        row.sizeDelta = new Vector2(width, height);

        // Distinct sections: [energy %] [yellow bar]  [orange bar] [hunger %].
        const float percentWidth = 56f;
        const float gap = 8f;
        const float barHeight = 24f;
        float barWidth = Mathf.Max(15f, (width - 2f * percentWidth - 3f * gap) / 2f);
        float barY = 38f;

        Place(nameText.rectTransform, 0f, 0f, width, 32f);
        nameText.fontSize = 25f;

        Place(energyText.rectTransform, 0f, barY, percentWidth, barHeight);
        Place((RectTransform)energyFill.parent,
            percentWidth + gap, barY, barWidth, barHeight);
        Place((RectTransform)hungerFill.parent,
            percentWidth + gap + barWidth + gap, barY, barWidth, barHeight);
        Place(hungerText.rectTransform,
            percentWidth + gap + barWidth + gap + barWidth + gap,
            barY, percentWidth, barHeight);

        energyText.fontSize = 21f;
        hungerText.fontSize = 21f;
        energyText.alignment = TextAlignmentOptions.Center;
        hungerText.alignment = TextAlignmentOptions.Center;
    }

    private void Update()
    {
        if (animal != null)
        {
            float energy = Mathf.Clamp(animal.Energy, 0f, 100f);
            float hunger = Mathf.Clamp(animal.Hunger, 0f, 100f);

            // Energy fills from left to right.
            energyFill.anchorMin = Vector2.zero;
            energyFill.anchorMax = new Vector2(energy / 100f, 1f);

            // Hunger fills from right to left.
            hungerFill.anchorMin = new Vector2(1f - hunger / 100f, 0f);
            hungerFill.anchorMax = Vector2.one;

            energyText.text = $"{energy:0}%";
            hungerText.text = $"{hunger:0}%";
        }
    }

    private RectTransform CreateBar(string barName, Color fillColor)
    {
        GameObject background = new GameObject(barName, typeof(RectTransform), typeof(Image));
        background.transform.SetParent(transform, false);
        background.GetComponent<Image>().color = new Color(0.12f, 0.13f, 0.16f, 0.9f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(background.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = fillColor;
        return fillRect;
    }

    private TMP_Text CreateText(string objectName, string value)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(transform, false);
        TMP_Text text = obj.GetComponent<TMP_Text>();
        text.text = value;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }
}
