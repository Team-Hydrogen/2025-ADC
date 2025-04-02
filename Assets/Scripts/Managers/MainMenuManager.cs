using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private RectTransform backgroundImage;
    [SerializeField] private RectTransform blackScreen;
    [SerializeField] private RectTransform startSimulationButton;
    [SerializeField] private RectTransform missionInformationButton;
    [SerializeField] private RectTransform modelViewerButton;
    [SerializeField] private RectTransform helpButton;
    [SerializeField] private RectTransform quitButton;

    private void Start()
    {
        StartCoroutine(MainMenuUIIntro(backgroundImage, 0f));
        StartCoroutine(MainMenuUIIntro(blackScreen, 1f));
        StartCoroutine(MainMenuUIIntro(startSimulationButton, 1.5f));
        StartCoroutine(MainMenuUIIntro(missionInformationButton, 2f));
        StartCoroutine(MainMenuUIIntro(modelViewerButton, 2.5f));
        StartCoroutine(MainMenuUIIntro(helpButton, 3f));
        StartCoroutine(MainMenuUIIntro(quitButton, 3.5f));
    }

    public void LoadMainScene()
    {
        StartCoroutine(MainMenuUIOutro(quitButton, 0f));
        StartCoroutine(MainMenuUIOutro(helpButton, 0.05f));
        StartCoroutine(MainMenuUIOutro(modelViewerButton, 0.1f));
        StartCoroutine(MainMenuUIOutro(missionInformationButton, 0.15f));
        StartCoroutine(MainMenuUIOutro(startSimulationButton, 0.2f));
        StartCoroutine(MainMenuUIOutro(backgroundImage, 0.25f));
        StartCoroutine(MainMenuUIOutro(blackScreen, 0.25f));
        StartCoroutine(SwitchToLoadingScene());
    }

    public void QuitApplication()
    {
        Application.Quit();
    }

    private IEnumerator MainMenuUIIntro(RectTransform element, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        element.GetComponent<Animator>().SetTrigger("Intro");

        yield return null;
    }

    private IEnumerator MainMenuUIOutro(RectTransform element, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        element.GetComponent<Animator>().SetTrigger("Outro");
        element.GetComponent<Animator>().SetBool("CanChangeStates", false);

        yield return null;
    }

    private IEnumerator SwitchToLoadingScene()
    {
        yield return new WaitForSeconds(1f);

        LoadingSceneManager.sceneToLoad = 2;
        SceneManager.LoadScene(0);
    }
}
