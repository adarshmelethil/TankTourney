using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public int m_NumRoundsToWin = 5;
    public float m_StartDelay = 3f;
    public float m_EndDelay = 3f;
    public CameraControl m_CameraControl;
    public Text m_MessageText;
    public GameObject m_TankPrefab;
    public GenerateMaze mazeGenerator;
    private TankManager[] m_Tanks;           //array of tank managers figure out how to do this with a group of ai tanks instead of players
    public GameObject m_flag;

    public Text[] m_TankTimerTexts;
    public Button pauseButton;
    public Button unPauseButton;
    public Canvas pauseMenu;

    public Transform team1Spawn;
    public Transform team2Spawn;

    //public PlayerServer[] m_PlayerServers;
    private const int NUM_PLAYERS = 2;

    private const float STARTING_TIME = 10000f;
    private int m_RoundNumber;              //round number
    private WaitForSeconds m_StartWait;     //delay for coroutine
    private WaitForSeconds m_EndWait;
    private TankManager m_RoundWinner;      //refer to specific tanks
    private bool gameIsPaused = false;

    private void Start()
    {
        //pauseButton.gameObject.SetActive(true);
        //unPauseButton.gameObject.SetActive(false);
        Time.timeScale = 1;
        pauseButton.gameObject.SetActive(true);
        pauseMenu.gameObject.SetActive(false);
        m_StartWait = new WaitForSeconds(m_StartDelay);
        m_EndWait = new WaitForSeconds(m_EndDelay);

        SpawnAllTanks();
        SetCameraTargets();

        StartCoroutine(GameLoop());

    }

    private void Update()
    {
        if (Input.GetKeyDown("escape"))
        {
            if (gameIsPaused)
            {
                UnPause();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Restart()
    {
        SceneManager.LoadScene(0);
    }

    public void Pause()
    {
        gameIsPaused = true;
        pauseButton.gameObject.SetActive(false);
        //unPauseButton.gameObject.SetActive(true);
        pauseMenu.gameObject.SetActive(true);
        Time.timeScale = 0;
    }

    public void UnPause()
    {
        gameIsPaused = false;
        pauseButton.gameObject.SetActive(true);
        //unPauseButton.gameObject.SetActive(false);
        pauseMenu.gameObject.SetActive(false);
        Time.timeScale = 1;
    }

    public void Quit()
    {
        Application.Quit();
    }

    //change this method here to take a list of tanks as usual but spawn them based off team and in random locations within a range
    private void SpawnAllTanks()
    {
        m_Tanks = new TankManager[NUM_PLAYERS];
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i] = new TankManager();
            m_Tanks[i].teamNumber = i+1;

            if (m_Tanks[i].teamNumber == 1)
            {
                m_Tanks[i].m_SpawnPoint = team1Spawn;
            }
            else
            {
                m_Tanks[i].m_SpawnPoint = team2Spawn;
            }

            m_Tanks[i].m_PlayerNumber = i + 1;
            m_Tanks[i].m_Instance =
                Instantiate(m_TankPrefab, m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation) as GameObject;
            m_Tanks[i].Setup(m_Tanks[i].m_SpawnPoint, m_flag.transform);
        }
    }


    private void SetCameraTargets()
    {
        Transform[] targets = new Transform[m_Tanks.Length];

        for (int i = 0; i < targets.Length; i++)
        {
            targets[i] = m_Tanks[i].m_Instance.transform;
        }

        m_CameraControl.m_Targets = targets;
    }


    private IEnumerator GameLoop()
    {
        yield return StartCoroutine(RoundStarting()); //waiting for RoundStarting to finish
        yield return StartCoroutine(RoundPlaying()); //wait for roundplaying to finish
        yield return StartCoroutine(RoundEnding()); //wait for round ending

        SceneManager.LoadScene(0);
    }


    private IEnumerator RoundStarting()
    {
        ResetAllTanks();
        DisableTankControl();


        mazeGenerator.generate();

        for (int i = 0; i < m_Tanks.Length; i++)
            m_Tanks[i].setTime(STARTING_TIME);

        setTimerText();


        m_CameraControl.SetStartPositionAndSize();
        m_RoundNumber++;
        m_MessageText.text = "ROUND " + m_RoundNumber;


        yield return m_StartWait;
    }

    private void setTimerText()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
            m_TankTimerTexts[i].text = $"{(int)m_Tanks[i].getTime()} Seconds";
    }


    private IEnumerator RoundPlaying()
    {
        EnableTankControl();

        m_MessageText.text = string.Empty;

        //while (!OneTankLeft())//change to the flag held time or something
        while (!OneTankWon())
        {
            setTimerText();

            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (!m_Tanks[i].holdingFlag()){
                    m_Tanks[i].updateTime(Time.deltaTime);
                }
            }
                

            for (int i = 0; i < m_Tanks.Length; i++)
            {
                if (m_Tanks[i].isDead())
                {
                    m_Tanks[i].Reset(m_Tanks[i].m_SpawnPoint);
                }
            }
            
            yield return null;
        }
    }


    private IEnumerator RoundEnding()
    {
        DisableTankControl();

        m_RoundWinner = null;

        
        m_RoundWinner = GetRoundWinner();
        if (m_RoundWinner == null) {
            Debug.Log("FOUND NO WINNER!!");
        }

        m_MessageText.text = EndMessage();

        yield return m_EndWait;
    }

    private bool OneTankWon()
    {
        int numTanksLeft = m_Tanks.Length;
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].getTime() <= 0)
                numTanksLeft--;
        }
        return numTanksLeft <= 1;
    }

    private TankManager GetRoundWinner()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].getTime() > 0)
            {
                return m_Tanks[i];
            }
            else
            {
                //the winner based off kills goes here and then the winner based off flag distance
                m_Tanks[i].distanceToFlag = Mathf.Abs(Vector3.Distance(m_Tanks[i].m_Instance.transform.position, m_flag.transform.position));
            }
        }
        //after forloop, may want to check kill counts here before distance toFlag

        //return Mathf.Max(m_Tanks[0].kills,m_Tanks[0].kills);

        if (m_Tanks[0].distanceToFlag < m_Tanks[1].distanceToFlag)
        {
            return m_Tanks[0];
        }
        else
        {
            return m_Tanks[1];
        }
    }


    private string EndMessage()
    {
        //change this part to change the method based off which team won may not even need a function for this anymore
        string message = "DRAW!";

        if (m_RoundWinner != null)
            message =$"Player{m_RoundWinner.m_PlayerNumber} WON THE GAME!";

        return message;
    }


    private void ResetAllTanks()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].Reset(m_Tanks[i].m_SpawnPoint);
        }
    }


    private void EnableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].EnableControl();
        }
    }


    private void DisableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].DisableControl();
        }
    }
}
