using System.Collections.Generic;
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager não encontrado; não é possível configurar a quantidade de fusíveis.", this);
            return;
        }

        SpawnRandomFuse();
    }

    public void SpawnRandomFuse()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager não encontrado; não é possível gerar fusíveis.", this);
            return;
        }

        if (fusiveisJaSpawnados >= GameManager.Instance.fusiveisNecessarios)
        {
            return;
        }

        if (fusePrefab == null)
        {
            Debug.LogError("Prefab de fusível não configurado no fuseManager.", this);
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Nenhum ponto de spawn configurado no fuseManager.", this);
            return;
        }

        List<int> pontosValidos = new List<int>();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null && i != ultimoPontoIndex)
                pontosValidos.Add(i);
        }

        if (pontosValidos.Count == 0)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                    pontosValidos.Add(i);
            }
        }

        if (pontosValidos.Count == 0)
        {
            Debug.LogError("Todos os pontos de spawn de fusíveis estão vazios.", this);
            return;
        }

        int randomIndex = pontosValidos[Random.Range(0, pontosValidos.Count)];
        ultimoPontoIndex = randomIndex;
        Transform selectedPoint = spawnPoints[randomIndex];

        Instantiate(fusePrefab, selectedPoint.position, Quaternion.identity);
        
        fusiveisJaSpawnados++;
    }
}