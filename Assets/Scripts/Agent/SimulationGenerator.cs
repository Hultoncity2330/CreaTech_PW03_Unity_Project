using UnityEngine;

public class SimulationGenerator : MonoBehaviour
{
    [SerializeField] private GameObject sheepPrefab;
    [SerializeField] private GameObject wolfPrefab;

    [SerializeField] private int sheepCount = 10;
    [SerializeField] private int wolfCount = 3;

    [SerializeField] private float spawnRange = 30f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Sheep generator
        for (int i = 0; i < sheepCount; i++)
        {
            Vector3 position = new Vector3(Random.Range(-spawnRange, spawnRange), 0f, Random.Range(-spawnRange, spawnRange));
            float angle = Random.Range(0f, 360f);
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            Instantiate(sheepPrefab, position, rotation);
        }

        // Wolf generator
        for (int i = 0; i < wolfCount; i++)
        {
            Vector3 position = new Vector3(Random.Range(-spawnRange, spawnRange), 0f, Random.Range(-spawnRange, spawnRange));
            float angle = Random.Range(0f, 360f);
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            Instantiate(wolfPrefab, position, rotation);
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
