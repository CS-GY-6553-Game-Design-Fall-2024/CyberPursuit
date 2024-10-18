using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class GameOverScreen : MonoBehaviour
{
    public void  RestartButton()
    {
        Time.timeScale = 1;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
        UnityEngine.Debug.Log("RESTART");
    }

    public void HomeButton()
    {
        SceneManager.LoadScene(0);
        UnityEngine.Debug.Log("Home");
    }

}
