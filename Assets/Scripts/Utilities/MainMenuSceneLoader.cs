using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuSceneLoader : MonoBehaviour
{
    private bool startSimulationButtonPressed = false;

    private void Start()
    {
        StartCoroutine(LoadSceneAsync());
    }

    public void StartSimulationButtonPressed()
    {
        startSimulationButtonPressed = true;
    }

    private IEnumerator LoadSceneAsync()
    {
        yield return null;

        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(1);
        asyncOperation.allowSceneActivation = false;

        while (!asyncOperation.isDone)
        {
            if (asyncOperation.progress >= 0.9f && startSimulationButtonPressed)
            {
                asyncOperation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
