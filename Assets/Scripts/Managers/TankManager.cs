using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    private GenerateMaze m_GenerateMaze;
    private GameObject healthCanvas;
    private GameObject killCountCanvas;
    private Text killCount;

    private float m_timer;
    public float distanceToFlag; //can change to public getter and setters instead

    public void Setup(Transform spawnPoint, Transform flag, GenerateMaze generateMaze)
    {
        //sets random spawn point for tank
        this.spawnPoint = m_SpawnPoint.position;
        m_GenerateMaze = generateMaze;

        m_Movement = m_Instance.GetComponent<TankMovement>();
        m_Movement.m_PlayerNumber = teamNumber;

        m_Shooting = m_Instance.GetComponent<TankShooting>();
        m_Shooting.m_PlayerNumber = teamNumber;

        //UI Healthbar manager
        m_Health = m_Instance.GetComponent<TankHealth>();
        healthCanvas = GameObject.FindGameObjectWithTag("HealthBar" + teamNumber);
        m_Health.ui_HealthSlider = healthCanvas.GetComponentInChildren<Slider>();

        //Kill count Ui manager
        killCountCanvas = GameObject.FindGameObjectWithTag("KillCount" + teamNumber);
        killCount = killCountCanvas.GetComponent<Text>();
        killCount.text = "KILL COUNT: " + getKillCount().ToString();//Not updating for some reason

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
        if (m_Movement != null)
            m_Movement.enabled = false;
        if (m_Shooting != null)
            m_Shooting.enabled = false;
        if (m_playerServer != null)
            m_playerServer.Stop();

        if (m_CanvasGameObject != null)
            m_CanvasGameObject.SetActive(false);
    }

    public bool playerConnected()
    {
        return m_playerServer.playerConnected();
    }
    public void EnableControl(bool debug)
    {
        m_Movement.enabled = true;
        m_Shooting.enabled = true;

        m_playerServer.Run(debug, teamNumber, m_Movement, m_tankObservation, m_Shooting, m_GenerateMaze);

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

    public void Destroy()
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

    //gets the killCount value for the tank
    public void updateKillCount()
    {
        getKillCount();
        killCount.text = "KILL COUNT: " + getKillCount().ToString();
    }

    public bool holdingFlag()
    {
        return m_Health.holdingFlag();
    }

    //getter to the shooting objects getscore method, this is to ensure we don't get the wrong tanks info
    public float getKillCount()
    {
        return m_Shooting.GetScore();
    }
}
