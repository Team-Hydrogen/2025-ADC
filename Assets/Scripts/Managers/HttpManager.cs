using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HttpManager : MonoBehaviour
{
    public static HttpManager Instance { get; private set; }
    
    private const string BumpOffCourseApiUri = "https://5ef6-2601-18c-500-fbb-18f2-2a3b-3c1e-d7bb.ngrok-free.app/trajectory";
    private const string BumpOffCourseApiContentType = "application/json";
    
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

    private class TransitionPathRequest
    {
        // Original data
        public float[] originPosition;
        public float[] originVelocity;
        // Destination data
        public float[] destinationPosition;
        public float[] destinationVelocity;
        // Time
        public float startTime;
        public float flightTime;

        public TransitionPathRequest(float[] op, float[] ov, float[] dp, float[] dv, float st, float ft)
        {
            originPosition = op;
            originVelocity = ov;
            destinationPosition = dp;
            destinationVelocity = dv;
            startTime = st;
            flightTime = ft;
        }
    }

    public void TransitionPathApi(Vector3 originPosition, Vector3 originVelocity, Vector3 destinationPosition,
        Vector3 destinationVelocity, float startTime, float flightTime)
    {
        float[] originPositionPostData = { originPosition.x, originPosition.y, originPosition.z };
        float[] originVelocityPostData = { originVelocity.x, originVelocity.y, originVelocity.z };
        float[] destinationPositionPostData = { destinationPosition.x, destinationPosition.y, destinationPosition.z };
        float[] destinationVelocityPostData = { destinationVelocity.x, destinationVelocity.y, destinationVelocity.z };

        var apiRequest = new TransitionPathRequest(
            originPositionPostData,
            originVelocityPostData,
            destinationPositionPostData,
            destinationVelocityPostData,
            startTime,
            flightTime
        );
        var postData = JsonUtility.ToJson(apiRequest);
        
        StartCoroutine(PingTransitionPathApi(postData));
    }
    
    private IEnumerator PingTransitionPathApi(string postData)
    {
        var webRequest = UnityWebRequest.Post(
            BumpOffCourseApiUri,
            postData,
            BumpOffCourseApiContentType
        );
        
        using (webRequest)
        {
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                String errorMessage = $"Error: {webRequest.error}";
                UIManager.Instance.ShowNotification(errorMessage, Notification.NotificationType.Dismissable);
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