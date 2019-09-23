using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Edge : MonoBehaviour
{
    public Tile[] tiles;

    public void enableEdge()
    {
        gameObject.GetComponent<MeshRenderer>().enabled = true;
        gameObject.GetComponent<BoxCollider>().enabled = true;
    }

    public void disableEdge()
    {
        gameObject.GetComponent<MeshRenderer>().enabled = false;
        gameObject.GetComponent<BoxCollider>().enabled = false;
    }
}


