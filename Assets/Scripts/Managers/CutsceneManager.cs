using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class CutsceneManager : MonoBehaviour
{
    [SerializeField] private VideoPlayer player;
    [SerializeField] private int nextSceneBuildIndex;
    [SerializeField] private bool loadNextSceneAsync = false;

    [SerializeField] private TextMeshProUGUI statusText;

    private float sceneLoadDelay = 2f;
    private float sceneActiveTime = 0;
    private bool startedLoadingScene = false;
    private bool videoFinished = false;

    private void OnEnable()
    {
        player.loopPointReached += SwitchScene;
    }

    private void OnDisable()
    {
        player.loopPointReached -= SwitchScene;
    }

    private void Start()
    {
        if (!loadNextSceneAsync)
        {
            statusText.text = "Press space to skip";
        }
    }

    private void Update()
    {
        sceneActiveTime += Time.deltaTime;
        if (sceneActiveTime > sceneLoadDelay && !startedLoadingScene)
        {
            if (loadNextSceneAsync)
            {
                print("Started loading next scene");
                startedLoadingScene = true;
                StartCoroutine(LoadSceneAsync());
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SwitchSceneMainMenu();
        }

        if (Input.GetKeyDown(KeyCode.Space) && !loadNextSceneAsync)
        {
            ForceSwitchScene();
        }
    }

    private void ForceSwitchScene()
    {
        SceneManager.LoadScene(nextSceneBuildIndex);
    }

    private void SwitchScene(VideoPlayer source)
    {
        if (!loadNextSceneAsync)
        {
            SceneManager.LoadScene(nextSceneBuildIndex);
        }
        
        // if scene loading is async
        videoFinished = true;
        print("VIDEO FINISHED");
    }

    private void SwitchSceneMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    private IEnumerator LoadSceneAsync()
    {
        yield return null;

        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(nextSceneBuildIndex);
        asyncOperation.allowSceneActivation = false;

        Application.backgroundLoadingPriority = ThreadPriority.Low;

        while (!asyncOperation.isDone)
        {
            statusText.text = "Loading... (" + (asyncOperation.progress * 100) + "%)";

            if (asyncOperation.progress >= 0.9f)
            {
                if (videoFinished)
                {
                    asyncOperation.allowSceneActivation = true;
                }

                statusText.text = "Press space to skip";
                if (Input.GetKeyDown (KeyCode.Space))
                {
                    asyncOperation.allowSceneActivation = true;
                }
            }

            yield return null;
        }
    }
}
