using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HttpManager : MonoBehaviour
{
    public static HttpManager Instance { get; private set; }
    private const string TransitionPathApiUri = "https://ce21-2601-18c-500-fbb-7f8c-2e76-f43d-413a.ngrok-free.app/trajectory";
    private const string TransitionPathApiContentType = "application/json";
    
    public static event Action<string> OnPathCalculated;
    
    
    #region Event Functions

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    #endregion
    
    
    # region Request Functions

    private class BumpOffCourseRequest
    {
        public float[] origin;
        public float[] destination;
        public float flightTime;
        public float startTime;

        public BumpOffCourseRequest(float[] o, float[] d, float ft, float st)
        {
            origin = o;
            destination = d;
            flightTime = ft;
            startTime = st;
        }
    }

    public void RequestBumpOffCourseApi(Vector3 origin, Vector3 destination, float flightTime, float startTime)
    {
        float[] originPostData = { origin.x, origin.y, origin.z };
        float[] destinationPostData = { destination.x, destination.y, destination.z };

        var apiRequest = new BumpOffCourseRequest(
            originPostData, 
            destinationPostData, 
            flightTime, 
            startTime
        );
        var postData = JsonUtility.ToJson(apiRequest);
        
        StartCoroutine(PingBumpOffCourseApi(postData));
    }
    
    private IEnumerator PingBumpOffCourseApi(string postData)
    {
        var webRequest = UnityWebRequest.Post(
            TransitionPathApiUri,
            postData,
            TransitionPathApiContentType
        );
        
        using (webRequest)
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                UIManager.Instance.ShowNotification($"Error: {webRequest.error}", Notification.NotificationType.Dismissible);
                Debug.Log(webRequest.error);
                Debug.Log(webRequest.result);
            }
            else
            {
                OnPathCalculated?.Invoke(webRequest.downloadHandler.text);
            }
        }
    }
    
    # endregion
}