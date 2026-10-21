using UnityEngine;

public class fuseManager : MonoBehaviour
{
    public GameObject fusePrefab;
    public Transform[] spawnPoints;

    private GameObject currentFuse;

    void Start()
    {
        SpawnRandomFuse();
    }

    public void SpawnRandomFuse()
    {
        if (spawnPoints.Length == 0 || fusePrefab == null) return;

        if (currentFuse != null)
        {
            Destroy(currentFuse);
        }

        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform selectedPoint = spawnPoints[randomIndex];

        currentFuse = Instantiate(fusePrefab, selectedPoint.position, Quaternion.identity);
    }
}