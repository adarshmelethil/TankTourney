using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateMaze : MonoBehaviour
{

    public GameObject prefab;
    
    public int width;
    
    public int height;

    public float span = 5f;
    public Transform startPoint;

    [HideInInspector]public Tile[] tiles;

    [HideInInspector] public List<Edge> edges;
    [HideInInspector] public List<Edge> edgesMirror;
    [HideInInspector] public List<Edge> ogEdges;
    [HideInInspector] public List<Edge> ogEdgesMirror;

    [HideInInspector] public int EdgeIndex = 0;

    public void generate()
    {
        tiles = new Tile[width * height];

        for (int i = 0; i < width * height; i++)
        {
            tiles[i] = new Tile();
        }

        edges = new List<Edge>();
        edgesMirror = new List<Edge>();

        SpawnLeftRightBoundaries();
        SpawnUpDownBoundaries();
        SpawnInnerEdgesLeftRight();
        SpawnInnerEdgesUpDown();

        ogEdges = new List<Edge>(edges);
        ogEdgesMirror = new List<Edge>(edgesMirror);

        RemoveEdgeCoroutine();
    }
    
    // Generating frame (outter boundaries)
    // make it just < height or width to remove one wall for gameplay
    public void SpawnLeftRightBoundaries()
    {
        for (int z = 1; z <= height; z++)
        {
            for (int x = 0; x <= width; x++)
            {
                if (x==0 || x== width)
                {
                    Instantiate(prefab, new Vector3((startPoint.position.x + x * span), 0, (startPoint.position.z + z * span)), Quaternion.Euler(0, 90, 0));
                    //This is for the mirrored maze
                    Instantiate(prefab, new Vector3((startPoint.position.x + x * span), 0, (startPoint.position.z + z * span) * -1), Quaternion.Euler(0, 90, 0));
                }
            }
        }
    }
    //instantiate bottom wall
    public void SpawnUpDownBoundaries()
    {
        int counter = 0;
        //set for (int z = 1; z <= height; z++) to remove walls on one side 0 will enclose the maze
        for (int z = 0; z <= height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                if (z == height)
                {
                    Instantiate(prefab, new Vector3((startPoint.position.x + x * span) + (span/2), 0, (startPoint.position.z + z * span) + (span/2)), Quaternion.Euler(0, 0, 0));
                    //This is for the mirrored maze
                    Instantiate(prefab, new Vector3((startPoint.position.x + x * span) + (span/2), 0, ((startPoint.position.z + z * span) + (span/2))*-1), Quaternion.Euler(0, 0, 0));
                }
                if (z == 0)
                {
                    //Random.Range has an inclusive 0 and exclusive 2
                    int check = Random.Range(0, 2);
                    //This is here just in case we created a wall in every other slot. If that's the case the last position of the maze will never have a wall ensuring there's always an opening
                    if (counter == width - 1)
                    {
                        break;
                    }
                    else if (check == 0)
                    {
                        Instantiate(prefab, new Vector3((startPoint.position.x + x * span) + (span / 2), 0, (startPoint.position.z + z * span) + (span / 2)), Quaternion.Euler(0, 0, 0));
                        //This is for the mirrored maze
                        Instantiate(prefab, new Vector3((startPoint.position.x + x * span) + (span / 2), 0, ((startPoint.position.z + z * span) + (span / 2)) * -1), Quaternion.Euler(0, 0, 0));
                        counter++;
                    }
                }

            }
        }
    }

    // Generating edges (innner Boundaries)
    public void SpawnInnerEdgesLeftRight()
    {
        EdgeIndex = 0;

        for (int z = 1; z <= height; z++)
        {
            for (int x = 1; x < width; x++)
            {
                GameObject go = (GameObject)Instantiate(prefab, new Vector3((startPoint.position.x + x * span), 0, (startPoint.position.z + z * span)), Quaternion.Euler(0, 90, 0));

                Edge edge = go.AddComponent<Edge>() as Edge;

                go.GetComponent<Edge>().tiles = new Tile[2];
                go.GetComponent<Edge>().tiles[0] = tiles[EdgeIndex];
                go.GetComponent<Edge>().tiles[1] = tiles[EdgeIndex+1];
                edges.Add(go.GetComponent<Edge>());
                
                GameObject goM = (GameObject)Instantiate(prefab, new Vector3((startPoint.position.x + x * span), 0, (startPoint.position.z + z * span) * -1), Quaternion.Euler(0, 90, 0));

                Edge edgeM = goM.AddComponent<Edge>() as Edge;

                goM.GetComponent<Edge>().tiles = new Tile[2];
                goM.GetComponent<Edge>().tiles[0] = tiles[EdgeIndex];
                goM.GetComponent<Edge>().tiles[1] = tiles[EdgeIndex + 1];
                edgesMirror.Add(goM.GetComponent<Edge>());
                
                EdgeIndex++;
            }
            EdgeIndex++;
        }
    }
    public void SpawnInnerEdgesUpDown()
    {
        EdgeIndex = 0;

        for (int z = 1; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject go = (GameObject)Instantiate(prefab, new Vector3((startPoint.position.x + x * span) + (span/2), 0, (startPoint.position.z + z * span) + (span/2)), Quaternion.Euler(0, 0, 0));

                Edge edge = go.AddComponent<Edge>() as Edge;

                go.GetComponent<Edge>().tiles = new Tile[2];
                go.GetComponent<Edge>().tiles[0] = tiles[EdgeIndex];
                go.GetComponent<Edge>().tiles[1] = tiles[EdgeIndex + 1];

                edges.Add(go.GetComponent<Edge>());
                //(z * span) shifts it over by span because the forloop doesn't change the position of the first wall
                GameObject goM = (GameObject)Instantiate(prefab, new Vector3((startPoint.position.x + x * span) + (span / 2), 0, (startPoint.position.z + (z * span) + span) * -1 + (span / 2)), Quaternion.Euler(0, 0, 0));

                Edge edgeM = goM.AddComponent<Edge>() as Edge;

                goM.GetComponent<Edge>().tiles = new Tile[2];
                goM.GetComponent<Edge>().tiles[0] = tiles[EdgeIndex];
                goM.GetComponent<Edge>().tiles[1] = tiles[EdgeIndex + 1];

                edgesMirror.Add(goM.GetComponent<Edge>());
                
                EdgeIndex++;
            }
        }
    }

    public void RemoveEdges()
    {
        // Get a random edge

        int randInt = Random.Range(0, edges.Count);

        Edge randomEdge = edges[randInt];
        Edge randomEdgeM = edgesMirror[randInt];

        // Remove random edge from list
        edges.RemoveAt(randInt);
        edgesMirror.RemoveAt(randInt);

        // Both null
        if (Tile.getHighestParent(randomEdge.tiles[0]) == Tile.getHighestParent(randomEdge.tiles[1]))
        {
            if (Tile.getHighestParent(randomEdge.tiles[0]) == null && Tile.getHighestParent(randomEdge.tiles[1]) == null)
            {
                randomEdge.tiles[0].parent = randomEdge.tiles[1];
                randomEdge.disableEdge();
                randomEdgeM.disableEdge();
            }
        }

        else
        {
            if (Tile.getHighestParent(randomEdge.tiles[0]) == null && Tile.getHighestParent(randomEdge.tiles[1]) == null)
            {
                Tile.getHighestParent(randomEdge.tiles[0]).parent = randomEdge.tiles[1];
                randomEdge.disableEdge();
                randomEdgeM.disableEdge();
            }
            else
            {
                Tile.getHighestParent(randomEdge.tiles[1]).parent = randomEdge.tiles[0];
                randomEdge.disableEdge();
                randomEdgeM.disableEdge();
            }
        }
    }

    public void RemoveEdgeCoroutine()
    {
        int loopNum = edges.Count;

        for (int i = 0; i < loopNum; i++)
        { 
            RemoveEdges();
        }
    }

    public void ResetMaze()
    {
        edges = ogEdges;
        edgesMirror = ogEdgesMirror;
        int loopNum = edges.Count;
        Debug.Log("Edge count " + loopNum);
        for(int i = 0; i < loopNum; i++)
        {
            edges[i].enableEdge();
            edgesMirror[i].enableEdge();
        }
    }
}

[System.Serializable]
public class Tile
{
   
    public Tile parent;

    public static Tile getHighestParent(Tile tile)
    {
        if (tile.parent == null)
        {
            return tile;
        }

        else
        {
            return getHighestParent(tile.parent);
        }
    }
}
