using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flag : MonoBehaviour
{

    bool held;
    Transform holder;
    public Vector3 startingPos = new Vector3(0,1,0);

    // Start is called before the first frame update
    void Start()
    {
        transform.position = startingPos;
    }

    private void Update()
    {
        if (held)
            gameObject.transform.position = new Vector3(holder.position.x, 5f, holder.position.z);
    }

    public void Drop()
    {
        held = false;
        gameObject.transform.position = new Vector3(gameObject.transform.position.x, 1, gameObject.transform.position.z);
    }

    private void OnTriggerEnter(Collider tank)
    {
        if (tank.gameObject.tag == "Player")
        {
            held = true;
            holder = tank.gameObject.transform;
            tank.gameObject.GetComponent<TankHealth>().holdFlag(this);
        }
    }
    
}
