using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private GameObject loseScreen;   
    [SerializeField] private GameObject winScreen;    

    [Header("Scenes")]
    [SerializeField] private string startSceneName = "Start";

    private bool ended;

    void Awake()
    {
        Time.timeScale = 1f;
        HidePanel(loseScreen);
        HidePanel(winScreen);
    }

    void HidePanel(GameObject panel)
    {
        if (!panel) return;
        panel.SetActive(false);

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (!cg) cg = panel.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    public void GameOver()
    {
        if (ended) return;
        ended = true;
        ShowPanel(loseScreen);
        EndGameplay();
    }

    public void Win()
    {
        if (ended) return;
        ended = true;
        ShowPanel(winScreen);
        EndGameplay();
    }

    void EndGameplay()
    {
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ShowPanel(GameObject panel)
    {
        if (!panel) return;
        panel.SetActive(true);
        StartCoroutine(FadeIn(panel));
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
    if (loseScreen) loseScreen.SetActive(false);
    if (winScreen) winScreen.SetActive(false);

        Time.timeScale = 1f;
        SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);
    }

    public void Quit()
    {
        Application.Quit();
    }

    IEnumerator FadeIn(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (!cg) yield break;

        cg.alpha = 0f;
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime)
        {
            cg.alpha = t;
            yield return null;
        }
        cg.alpha = 1f;
    }
}
