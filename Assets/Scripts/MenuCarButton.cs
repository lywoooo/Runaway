using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuCarButton : MonoBehaviour
{
    public enum ActionType { LoadScene, Quit }

    [Header("Label")]
    public string hoverText = "PLAY";
    public TextMeshProUGUI actionText;
    public string defaultText = "SELECT A VEHICLE";

    [Header("Action")]
    public ActionType action = ActionType.LoadScene;
    public string sceneToLoad = "Game";

    [Header("Drive away")]
    public float driveTime = 0.8f;
    public float driveSpeed = 12f;

    static bool clicked;

    void OnMouseEnter()
    {
        if (clicked) return;
        if (actionText) actionText.text = hoverText;
    }

    void OnMouseExit()
    {
        if (clicked) return;
        if (actionText) actionText.text = defaultText;
    }

    void OnMouseDown()
    {
        if (clicked) return;
        clicked = true;
        StartCoroutine(DriveAndDoAction());
    }

    IEnumerator DriveAndDoAction()
    {
        float t = 0f;
        while (t < driveTime)
        {
            t += Time.deltaTime;
            transform.position += transform.forward * driveSpeed * Time.deltaTime;
            yield return null;
        }

        if (action == ActionType.Quit)
            Application.Quit();
        else
            SceneManager.LoadScene(sceneToLoad);
    }
}
