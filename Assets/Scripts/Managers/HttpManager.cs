using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HttpManager : MonoBehaviour
{
    public static HttpManager Instance { get; private set; }
    
    private const string BumpOffCourseApiUri = "https://two025-adc-data.onrender.com/trajectory";
    private const string BumpOffCourseApiContentType = "application/json";
    
    
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

    public void RequestBumpOffCourseApi(Vector3 origin, Vector3 destination, float flightTime, float startTime)
    {
        float[] originPostData = { origin.x, origin.y, origin.z };
        float[] destinationPostData = { destination.x, destination.y, destination.z };
        
        Dictionary<string, dynamic> postData = new() 
        {
            {"origin", originPostData},
            {"destination", destinationPostData},
            {"flightTime", flightTime},
            {"startTime", startTime},
        };
        
        StartCoroutine(PingBumpOffCourseApi(postData));
    }
    
    private static IEnumerator PingBumpOffCourseApi(Dictionary<string, dynamic> postData)
    {
        var webRequest = UnityWebRequest.Post(
            BumpOffCourseApiUri,
            postData.ToString(),
            BumpOffCourseApiContentType
        );
        
        using (webRequest)
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(webRequest.error);
            }
            else
            {
                Debug.Log(webRequest.downloadHandler.text);
            }
        }
    }
    
    # endregion
}