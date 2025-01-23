using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SplashLoadingMenu : MonoBehaviour
{
    [SerializeField] Slider loadingBar;


    private void Start()
    {
        StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator LoadSceneAsync()
    {
        yield return new WaitForSecondsRealtime(1);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(1);

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            loadingBar.value = asyncLoad.progress;
            yield return null;
        }
    }
}
