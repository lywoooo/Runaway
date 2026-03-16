using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class GoBack : MonoBehaviour
{
    [SerializeField] private string startSceneName = "Start";

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);
    }
}
