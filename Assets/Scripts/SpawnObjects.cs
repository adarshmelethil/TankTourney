using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; //for debugging

//TODO: BIGGEST THING make sure they don't block a path, easiset way would be to scan before spawn to check for nearby objects
public class SpawnObjects : MonoBehaviour
{
    public GameObject[] obstaclePrefab;

    public int objectCount;

    public Vector3 center;
    public Vector3 size;

    public float spawnRadius;
    public LayerMask ignoreMask;

    // Start is called before the first frame update
    void Start()
    {
        SpawnObstacles(objectCount);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Q))
        {
            SceneManager.LoadScene(0);
        }
    }

    public void SpawnObstacles(int numOfObjects)
    {
        for (int i = 0; i < numOfObjects; i++)
        {
            bool canSpawn = false;
            int safetyCount = 0;
            Vector3 pos;
            while (!canSpawn)
            {
                pos = center + new Vector3(Random.Range(-size.x / 2, size.x / 2), 0, Random.Range(0, size.z / 2));
                canSpawn = PreventSpawnOverlap(pos, spawnRadius);
                if (canSpawn)
                {
                    int prefabNum = Random.Range(0, obstaclePrefab.Length);
                    float randRotation = Random.Range(0, 360);
     
                    Vector3 flippedPos = pos;
                    flippedPos.z = -flippedPos.z;
                    
                    Instantiate(obstaclePrefab[prefabNum], pos, Quaternion.Euler(new Vector3(0, randRotation, 0)));
                    Instantiate(obstaclePrefab[prefabNum], flippedPos, Quaternion.Euler(new Vector3(0, -randRotation, 0)));
                    break;
                }

                safetyCount++;

                if(safetyCount > 10)
                {
                    Debug.Log("Failed to spawn object");
                    break;
                }
             }
        }
    }

    private bool PreventSpawnOverlap(Vector3 spawnPos, float overlapRadius)
    {
        Collider[] colliders = Physics.OverlapSphere(spawnPos, overlapRadius, ignoreMask);
        if (colliders != null && colliders.Length > 1)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawCube(center, size);
    }
}
