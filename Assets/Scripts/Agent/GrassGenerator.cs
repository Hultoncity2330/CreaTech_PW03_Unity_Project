using UnityEngine;

/// <summary>Generates grass at startup and spawns one new tuft at random intervals.</summary>
public class GrassGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject grassPrefab;
    [SerializeField] private Collider terrainCollider;

    [Header("Boundaries")]
    [SerializeField] private Vector2 areaCenter = Vector2.zero;
    [SerializeField, Min(0f)] private float areaHalfSize = 60f;

    [Header("Grass generation")]
    [SerializeField] private int initialCount = 200;
    [SerializeField, Min(0)] private int maximumCount = 300; // 0 = no limit
    [SerializeField] private float minRegrowTime = 0.5f;
    [SerializeField] private float maxRegrowTime = 1f;

    private float nextRegrowTime;

    private void Start()
    {
        if (grassPrefab != null && terrainCollider != null)
        {
            for (int i = 0; i < initialCount && (maximumCount == 0 || transform.childCount < maximumCount); i++)
                SpawnGrass();
        }
        else
        {
            Debug.LogWarning("GrassGenerator: assign Grass Prefab and Terrain Collider.");
        }

        ScheduleRegrowth();
    }

    private void Update()
    {
        if (Time.time >= nextRegrowTime)
        {
            if (grassPrefab != null && terrainCollider != null &&
                (maximumCount == 0 || transform.childCount < maximumCount))
                SpawnGrass();

            ScheduleRegrowth();
        }
    }

    private void ScheduleRegrowth()
    {
        nextRegrowTime = Time.time + Random.Range(minRegrowTime, maxRegrowTime);
    }

    private void SpawnGrass()
    {
        Bounds bounds = terrainCollider.bounds;
        // Intersect the square boundary with the actual terrain bounds.
        float minX = Mathf.Max(bounds.min.x, areaCenter.x - areaHalfSize);
        float maxX = Mathf.Min(bounds.max.x, areaCenter.x + areaHalfSize);
        float minZ = Mathf.Max(bounds.min.z, areaCenter.y - areaHalfSize);
        float maxZ = Mathf.Min(bounds.max.z, areaCenter.y + areaHalfSize);

        if (minX >= maxX || minZ >= maxZ)
            return;

        float x = Random.Range(minX, maxX);
        float z = Random.Range(minZ, maxZ);

        Ray ray = new Ray(new Vector3(x, bounds.max.y + 10f, z), Vector3.down);
        RaycastHit hit;

        if (terrainCollider.Raycast(ray, out hit, bounds.size.y + 20f))
        {
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            Instantiate(grassPrefab, hit.point, rotation, transform);
        }
    }
}
