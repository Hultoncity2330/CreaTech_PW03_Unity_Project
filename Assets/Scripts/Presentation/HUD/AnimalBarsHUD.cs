using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnimalBarsHUD : MonoBehaviour
{
    [SerializeField] private RectTransform sheepPanel;
    [SerializeField] private RectTransform wolfPanel;
    [SerializeField] private float refreshInterval = 0.5f;

    private readonly Dictionary<AgentBehaviour, AnimalBarRow> rows = new();
    private readonly List<AgentBehaviour> toRemove = new();
    private int sheepId;
    private int wolfId;
    private float nextRefresh;

    private void Start()
    {
        PreparePanel(sheepPanel, "SHEEP");
        PreparePanel(wolfPanel, "WOLVES");
        RefreshAnimals();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + Mathf.Max(0.1f, refreshInterval);
            RefreshAnimals();
        }
    }

    private static void PreparePanel(RectTransform panel, string heading)
    {
        if (panel == null)
            return;

        Image background = panel.GetComponent<Image>();
        if (background != null)
            background.color = new Color(0.08f, 0.11f, 0.16f, 0.8f);

        TMP_Text titleText = CreateOrGetText(panel, "HUDTitle");
        titleText.text = heading;
        titleText.fontSize = 32f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;

        TMP_Text energyHeader = CreateOrGetText(panel, "EnergyHeader");
        energyHeader.text = "Energy";
        energyHeader.fontSize = 17f;
        energyHeader.fontStyle = FontStyles.Bold;
        energyHeader.color = new Color(1f, 0.85f, 0.15f);
        energyHeader.alignment = TextAlignmentOptions.Center;

        TMP_Text foodHeader = CreateOrGetText(panel, "FoodHeader");
        foodHeader.text = "Food";
        foodHeader.fontSize = 17f;
        foodHeader.fontStyle = FontStyles.Bold;
        foodHeader.color = new Color(1f, 0.48f, 0.12f);
        foodHeader.alignment = TextAlignmentOptions.Center;

        UpdatePanelHeaderLayout(panel);
    }

    private static TMP_Text CreateOrGetText(RectTransform panel, string objectName)
    {
        Transform existing = panel.Find(objectName);
        if (existing == null)
        {
            GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(panel, false);
            existing = obj.transform;
        }

        TMP_Text text = existing.GetComponent<TMP_Text>();
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private static void UpdatePanelHeaderLayout(RectTransform panel)
    {
        float width = Mathf.Max(1f, panel.rect.width - 20f);

        const float percentWidth = 62f;
        const float gap = 6f;
        float barWidth = Mathf.Max(15f, (width - 2f * percentWidth - 3f * gap) / 2f);
        float startX = 10f;

        RectTransform title = (RectTransform)panel.Find("HUDTitle");
        title.anchorMin = new Vector2(0f, 1f);
        title.anchorMax = new Vector2(1f, 1f);
        title.pivot = new Vector2(0.5f, 1f);
        title.anchoredPosition = new Vector2(0f, -4f);
        title.sizeDelta = new Vector2(0f, 40f);

        float energyX = startX + percentWidth + gap;
        float foodX = energyX + barWidth + gap;
        float labelY = 43f;

        PlaceHeader((RectTransform)panel.Find("EnergyHeader"), energyX, labelY, barWidth, 22f);
        PlaceHeader((RectTransform)panel.Find("FoodHeader"), foodX, labelY, barWidth, 22f);
    }

    private void RefreshAnimals()
    {
        if (sheepPanel == null || wolfPanel == null)
            return;

        UpdatePanelHeaderLayout(sheepPanel);
        UpdatePanelHeaderLayout(wolfPanel);

        AgentBehaviour[] animals = FindObjectsByType<AgentBehaviour>();
        HashSet<AgentBehaviour> alive = new(animals);

        toRemove.Clear();
        foreach (var entry in rows)
        {
            if (entry.Key == null || !alive.Contains(entry.Key))
            {
                if (entry.Value != null)
                    Destroy(entry.Value.gameObject);
                toRemove.Add(entry.Key);
            }
        }

        foreach (AgentBehaviour old in toRemove)
            rows.Remove(old);

        foreach (AgentBehaviour animal in animals)
        {
            if (rows.ContainsKey(animal))
                continue;

            bool isSheep = animal.GetSimType() == SimulationType.Sheep;
            RectTransform panel = isSheep ? sheepPanel : wolfPanel;
            string label = isSheep ? $"Sheep {++sheepId}" : $"Wolf {++wolfId}";

            GameObject obj = new GameObject(label, typeof(RectTransform), typeof(AnimalBarRow));
            obj.transform.SetParent(panel, false);
            AnimalBarRow row = obj.GetComponent<AnimalBarRow>();
            row.Initialize(animal, label);
            rows.Add(animal, row);
        }

        int sheepCount = 0;
        int wolfCount = 0;

        foreach (AgentBehaviour animal in animals)
        {
            if (animal == null || animal.IsDead)
                continue;

            if (animal.GetSimType() == SimulationType.Sheep)
                sheepCount++;
            else if (animal.GetSimType() == SimulationType.Wolf)
                wolfCount++;
        }

        sheepPanel.Find("HUDTitle").GetComponent<TMP_Text>().text = $"SHEEP ({sheepCount})";
        wolfPanel.Find("HUDTitle").GetComponent<TMP_Text>().text = $"WOLVES ({wolfCount})";

        ArrangeRows(sheepPanel, SimulationType.Sheep);
        ArrangeRows(wolfPanel, SimulationType.Wolf);
    }

    private void ArrangeRows(RectTransform panel, SimulationType animalType)
    {
        int line = 0;
        float width = Mathf.Max(1f, panel.rect.width - 20f);

        foreach (var entry in rows)
        {
            if (entry.Key != null && entry.Key.GetSimType() == animalType)
            {
                entry.Value.SetLayout(10f, 82f + line * 68f, width, 62f);
                line++;
            }
        }
    }

    private static void PlaceHeader(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }
}
