﻿using UnityEngine;
using UnityEngine.UI;

public class TankHealth : MonoBehaviour
{
    public float m_StartingHealth = 100f;          
    public Slider m_Slider;                        //healthslider
    public Image m_FillImage;                      //image component of fill game object
    public Slider ui_HealthSlider;
    public Color m_FullHealthColor = Color.green;  
    public Color m_ZeroHealthColor = Color.red;    
    public GameObject m_ExplosionPrefab;
    
    
    private AudioSource m_ExplosionAudio;          //reference to component in instantiated game object 
    private ParticleSystem m_ExplosionParticles;   //same as explosion audio
    private float m_CurrentHealth;
    private bool m_Dead;

    Flag flag;

    private void Awake()
    {
        m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();
        m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();

        m_ExplosionParticles.gameObject.SetActive(false);
    }


    private void OnEnable()
    {
        m_CurrentHealth = m_StartingHealth;
        m_Dead = false;

        SetHealthUI();
    }
    

    public void TakeDamage(float amount)
    {
        // Adjust the tank's current health, update the UI based on the new health and check whether or not the tank is dead.
        m_CurrentHealth -= amount;

        SetHealthUI();

        if(m_CurrentHealth <= 0f && !m_Dead)
        {
            OnDeath();
        }
    }

    private void SetHealthUI()
    {
        // Adjust the value and colour of the slider.
        m_Slider.value = m_CurrentHealth;
        if (ui_HealthSlider != null)//this is needed because the game doesn't start with tanks but the UI starts right in the start menu
        {
            ui_HealthSlider.value = m_CurrentHealth;
        }

        m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, (m_CurrentHealth/m_StartingHealth));
    }

    private void OnDeath()
    {
        // Play the effects for the death of the tank and deactivate it.
        m_Dead = true;

        m_ExplosionParticles.transform.position = transform.position;
        m_ExplosionParticles.gameObject.SetActive(true);

        m_ExplosionParticles.Play();
        m_ExplosionAudio.Play();
        
        gameObject.SetActive(false);

        if (flag != null)
        {
            flag.Drop();
            flag = null;
        }
    }

    public bool isDead()
    {
        return m_Dead;
    }

    public void holdFlag(Flag f)
    {
        flag = f;
    }

    public bool holdingFlag()
    {
        return flag != null;
    }

}