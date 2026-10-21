using UnityEngine;

public class fuseManager : MonoBehaviour
{
    public static fuseManager Instance; 

    public GameObject fusePrefab;
    public Transform[] spawnPoints;

    private int fusiveisJaSpawnados = 0;
    private int ultimoPontoIndex = -1;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        SpawnRandomFuse(); 
    }

    public void SpawnRandomFuse()
    {
        if (GameManager.Instance != null && fusiveisJaSpawnados >= GameManager.Instance.fusiveisNecessarios)
        {
            return;
        }

        if (spawnPoints.Length == 0 || fusePrefab == null) return;

        int randomIndex = Random.Range(0, spawnPoints.Length);

        while (randomIndex == ultimoPontoIndex && spawnPoints.Length > 1)
        {
            randomIndex = Random.Range(0, spawnPoints.Length);
        }
        
        ultimoPontoIndex = randomIndex;
        Transform selectedPoint = spawnPoints[randomIndex];

        Instantiate(fusePrefab, selectedPoint.position, Quaternion.identity);
        
        fusiveisJaSpawnados++;
    }
}