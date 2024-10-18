using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    // Singleton logic
    public static AudioManager current;

    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;

    public AudioClip level_one;
    public AudioClip level_two;
    
    public AudioClip hud_track;

    public AudioClip jump;
    public AudioClip level_complete;
    public AudioClip slide;
    public AudioClip wall_jump;
    public AudioClip car;
    public AudioClip hurt;

    private void Awake() {
        current = this;
    }

    private void Start()
    {
        // Get the currently active scene
        Scene currentScene = SceneManager.GetActiveScene();

        // Change the music based on the scene name or index
        switch (currentScene.name)
        {
            case "Debug":
                musicSource.clip = hud_track;
                break;
            case "MainMenu":
                musicSource.clip = hud_track;
                break;
            case "Tutorial":
                musicSource.clip = level_one;
                break;
            case "2ndLevel":
                musicSource.clip = level_two;
                break;
            default:
                musicSource.clip = hud_track;
                break;
        }
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }
}
