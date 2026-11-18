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
        if (panel != null)
        {
            Image background = panel.GetComponent<Image>();
            if (background != null)
                background.color = new Color(0.08f, 0.11f, 0.16f, 0.8f);

            Transform existing = panel.Find("HUDTitle");
            if (existing == null)
            {
                GameObject title = new GameObject("HUDTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                title.transform.SetParent(panel, false);
                existing = title.transform;
            }

            RectTransform rect = (RectTransform)existing;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -4f);
            rect.sizeDelta = new Vector2(0f, 40f);

            TMP_Text text = existing.GetComponent<TMP_Text>();
            text.text = heading;
            text.fontSize = 32f;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
        }
    }

    private void RefreshAnimals()
    {
        if (sheepPanel != null && wolfPanel != null)
        {
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
                if (!rows.ContainsKey(animal))
                {
                    bool isSheep = animal.GetSimType() == SimulationType.Sheep;
                    RectTransform panel = isSheep ? sheepPanel : wolfPanel;
                    string label = isSheep ? $"Sheep {++sheepId}" : $"Wolf {++wolfId}";
                    GameObject obj = new GameObject(label, typeof(RectTransform), typeof(AnimalBarRow));
                    obj.transform.SetParent(panel, false);
                    AnimalBarRow row = obj.GetComponent<AnimalBarRow>();
                    row.Initialize(animal, label);
                    rows.Add(animal, row);
                }
            }

            int sheepCount = 0;
            int wolfCount = 0;

            foreach (AgentBehaviour animal in animals)
            {
                if (animal != null && !animal.IsDead)
                {
                    if (animal.GetSimType() == SimulationType.Sheep)
                        sheepCount++;
                    else if (animal.GetSimType() == SimulationType.Wolf)
                        wolfCount++;
                }
            }

            sheepPanel.Find("HUDTitle").GetComponent<TMP_Text>().text = $"SHEEP ({sheepCount})";
            wolfPanel.Find("HUDTitle").GetComponent<TMP_Text>().text = $"WOLVES ({wolfCount})";

            ArrangeRows(sheepPanel, SimulationType.Sheep);
            ArrangeRows(wolfPanel, SimulationType.Wolf);
        }
    }

    private void ArrangeRows(RectTransform panel, SimulationType animalType)
    {
        int line = 0;
        float width = Mathf.Max(1f, panel.rect.width - 20f);

        // One simple vertical list; no columns and no Scroll View.
        foreach (var entry in rows)
        {
            if (entry.Key != null && entry.Key.GetSimType() == animalType)
            {
                entry.Value.SetLayout(10f, 48f + line * 78f, width, 74f);
                line++;
            }
        }
    }
}
