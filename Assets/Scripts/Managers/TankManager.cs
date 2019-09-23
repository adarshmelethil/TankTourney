using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TankManager
{
    [HideInInspector] public int m_PlayerNumber;             //filters through shooting and moving script for input but we should have team number instead or something like that
    [HideInInspector] public int teamNumber;                 //filters through shooting and moving script for input but we should have team number instead or something like that
    [HideInInspector] public string m_ColoredPlayerText;
    [HideInInspector] public Transform m_SpawnPoint;         //initial spawn point before random range
    [HideInInspector] public GameObject m_Instance;          //storing a tank's instance
    [HideInInspector] public GameObject m_Gameboard;
    [HideInInspector] public int m_Wins;                     //number of tank wins


    private Vector3 spawnPoint;
    private TankMovement m_Movement;
    private TankShooting m_Shooting;
    private TankHealth m_Health;
    
    private GameObject m_CanvasGameObject;
    private Color m_PlayerColor;
    private PlayerServer m_playerServer;
    private TankObservation m_tankObservation;

    private float m_timer;
    public float distanceToFlag; //can change to public getter and setters instead

    public void Setup(Transform spawnPoint, Transform flag)
    {
        //sets random spawn point for tank
        this.spawnPoint = m_SpawnPoint.position;

        m_Movement = m_Instance.GetComponent<TankMovement>();
        m_Movement.m_PlayerNumber = teamNumber;

        m_Shooting = m_Instance.GetComponent<TankShooting>();
        m_Shooting.m_PlayerNumber = teamNumber;

        m_Health = m_Instance.GetComponent<TankHealth>();

        // Observation
        m_tankObservation = m_Instance.GetComponent<TankObservation>();
        m_tankObservation.SetFlag(flag);

        m_Instance.GetComponentsInChildren<ScreenshotHandler>()[0].SetScreenshotName($"Player{teamNumber}");
        

        // Player server
        m_playerServer = new PlayerServer();

        m_Movement.m_PlayerNumber = m_PlayerNumber;
        m_Shooting.m_PlayerNumber = m_PlayerNumber;

        m_CanvasGameObject = m_Instance.GetComponentInChildren<Canvas>().gameObject;

        if (teamNumber == 1)
        {
            m_PlayerColor = Color.red;
        }
        else if (teamNumber == 2)
        {
            m_PlayerColor = Color.blue;
        }
        else //this one shouldn't matter so long as I did the mod operation correctly
        {
            m_PlayerColor = Color.black;
        }

        m_ColoredPlayerText = "<color=#" + ColorUtility.ToHtmlStringRGB(m_PlayerColor) + ">TEAM " + teamNumber + "</color>"; //html like text to handle the text colour

        MeshRenderer[] renderers = m_Instance.GetComponentsInChildren<MeshRenderer>();//gets all mesh renderers in tank prefab

        //set all renderers in tank's meshrenderers to the player's colour
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = m_PlayerColor;
        }
    }

    public void DisableControl()
    {
        m_Movement.enabled = false;
        m_Shooting.enabled = false;

        m_playerServer.Stop();

        m_CanvasGameObject.SetActive(false);
    }


    public void EnableControl()
    {
        m_Movement.enabled = true;
        m_Shooting.enabled = true;

        m_playerServer.Run(teamNumber, m_Movement, m_tankObservation);

        m_CanvasGameObject.SetActive(true);
    }

    public void Reset(Transform spawnPoint)
    {
        //sets random spawn point for tanks
        this.spawnPoint = m_SpawnPoint.position;
        m_Instance.transform.position = this.spawnPoint;
        m_Instance.transform.rotation = m_SpawnPoint.rotation;

        //make everything false then true for a full reset
        m_Instance.SetActive(false);
        m_Instance.SetActive(true);
    }

    void OnDestroy()
    {
        m_playerServer.Stop();
    }

    public bool isDead()
    {
        return m_Health.isDead();
    }

    public void setTime(float t)
    {
        m_timer = t;
    }

    public float getTime()
    {
        return m_timer;
    }

    public void updateTime(float deltaTime)
    {
        m_timer -= deltaTime;

    }

    public bool holdingFlag()
    {
        return m_Health.holdingFlag();
    }

}
