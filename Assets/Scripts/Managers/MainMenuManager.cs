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
        StartCoroutine(MainMenuButtonIntroOffset(startSimulationButton, 2f));
        StartCoroutine(MainMenuButtonIntroOffset(missionInformationButton, 2.5f));
        StartCoroutine(MainMenuButtonIntroOffset(modelViewerButton, 3f));
        StartCoroutine(MainMenuButtonIntroOffset(quitButton, 3.5f));
    }

    public void LoadMainScene()
    {
        SceneManager.LoadScene(2);
    }

    private IEnumerator MainMenuButtonIntroOffset(RectTransform rect, float timeOffset)
    {
        Vector2 originalPosition = rect.anchoredPosition;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x - 2000, rect.anchoredPosition.y);

        yield return new WaitForSeconds(timeOffset);

        rect.gameObject.SetActive(true);

        //while (rect.anchoredPosition.x <= originalPosition.x)
        //{
        //    Vector2 currentPosition = rect.anchoredPosition;

        //    rect.anchoredPosition = new Vector2((currentPosition.x + 0) * Time.deltaTime, currentPosition.y);
        //    yield return null;
        //}
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
