using System;
using UnityEngine;
using UnityEngine.UI;

public class TankShooting : MonoBehaviour
{
    public int m_PlayerNumber = 1;       //instead probably replace this with tankteam for ai instead of input
    public Rigidbody m_Shell;            //shell prefab to instantiate
    public Transform m_FireTransform;    //where shooting from
    public Slider m_AimSlider;           //charge slider may not need this depending on how you want to do the tank shooting
    public AudioSource m_ShootingAudio;  
    public AudioClip m_ChargingClip;     
    public AudioClip m_FireClip;         
    public float m_MinLaunchForce = 15f; //these three variables are for charge shooting if we don't use this just use max launch force
    public float m_MaxLaunchForce = 30f; 
    public float m_MaxChargeTime = 0.75f;

    
    private string m_FireButton;         //input most likely not needed but for debugging
    private float m_CurrentLaunchForce;  
    private float m_ChargeSpeed;         
    private bool m_Fired;                //fired or not so that we don't fire more than once per press
    private bool m_botFire;

    //poorly placed killcount variable but this is the easiest place to get information on killcount
    private int killCount = 0;

    public float timeBetweenShots = 1f;
    [SerializeField]
    private float timeSinceLastShot = 1f;

    //different from start because this can be called multiple times so everytime the tank responds
    private void OnEnable()
    {
        m_CurrentLaunchForce = m_MinLaunchForce;
        m_AimSlider.value = m_MinLaunchForce;
    }


    private void Start()
    {
        m_FireButton = "Fire" + m_PlayerNumber;
        m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
    }
    
    //do we want a charge time or just fire the shells at full force
    private void Update()
    {
        timeSinceLastShot += Time.deltaTime;

        if(timeSinceLastShot > timeBetweenShots)
        {
            // Track the current state of the fire button and make decisions based on the current launch force.
            if (!m_botFire)
            {
                m_AimSlider.value = m_MinLaunchForce;
                if ((m_CurrentLaunchForce >= m_MaxLaunchForce) && !m_Fired)
                {
                    //at max charge, not fired yet
                    m_CurrentLaunchForce = m_MaxLaunchForce;
                    Fire();
                }
                else if (Input.GetButtonDown(m_FireButton))
                {
                    //have pressed fire for first time?
                    m_Fired = false;
                    m_CurrentLaunchForce = m_MinLaunchForce;

                    m_ShootingAudio.clip = m_ChargingClip;
                    m_ShootingAudio.Play();
                }
                else if (Input.GetButton(m_FireButton) && !m_Fired)
                {
                    //holding fire button, not fired yet
                    m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
                    m_AimSlider.value = m_CurrentLaunchForce;
                }
                else if (Input.GetButtonUp(m_FireButton) && !m_Fired)
                {
                    //Debug.Log($"Fire {m_CurrentLaunchForce}");
                    //we released the button, having not fired yet
                    Fire();
                }
            }else
            {
                Fire();
            }
        }
    }

    public void BotFire(float fireval)
    {
        if (fireval > 0)
        {
            m_CurrentLaunchForce = Math.Max(m_MinLaunchForce, m_MaxLaunchForce * Math.Min(1, Math.Max(0, fireval)));
            m_botFire = true;
        }
    }


    private void Fire()
    {
        timeSinceLastShot = 0f;
        // Instantiate and launch the shell.
        m_Fired = true;
        m_botFire = false;
        Rigidbody shellInstance = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;

        shellInstance.velocity = m_CurrentLaunchForce * m_FireTransform.forward; //charge shot
        shellInstance.GetComponent<ShellExplosion>().m_shooter = transform; //sets the shooter of the shell
        // shellInstance.velocity = m_MaxLaunchForce * m_FireTransform.forward; //no charge shot

        m_ShootingAudio.clip = m_FireClip;
        m_ShootingAudio.Play();

        m_CurrentLaunchForce = m_MinLaunchForce;
    }
    
    //private set for score, for some reason needs increment when using sendmessage as killCount++ doesn't work with sendmessage
    private void SetScore(int increment)
    {
        killCount = killCount+increment;
    }

    //public get used for the UI's kill count text and game end possible result
    public int GetScore()
    {
        return killCount;
    }
}