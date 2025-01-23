using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private RectTransform startSimulationButton;
    [SerializeField] private RectTransform missionInformationButton;
    [SerializeField] private RectTransform modelViewerButton;
    [SerializeField] private RectTransform quitButton;

    private void Start()
    {
        StartCoroutine(MainMenuButtonIntroOffset(startSimulationButton, 1.5f));
        StartCoroutine(MainMenuButtonIntroOffset(missionInformationButton, 2f));
        StartCoroutine(MainMenuButtonIntroOffset(modelViewerButton, 2.5f));
        StartCoroutine(MainMenuButtonIntroOffset(quitButton, 3f));
    }

    public void LoadMainScene()
    {
        SceneManager.LoadScene(2);
    }

    public void QuitApplication()
    {
        Application.Quit();
    }

    private IEnumerator MainMenuButtonIntroOffset(RectTransform button, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        button.GetComponent<Animator>().SetTrigger("Intro");

        yield return null;
    }

    //public IEnumerator LoadMainSceneAsync()
    //{
    //    yield return new WaitForSecondsRealtime(1);

    //    AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(2);

    //    // Wait until the asynchronous scene fully loads
    //    while (!asyncLoad.isDone)
    //    {
    //        yield return null;
    //    }
    //}
}
