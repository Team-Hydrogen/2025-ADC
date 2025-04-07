using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class HttpHelper
{
    private IEnumerator SendRequest(string uri, string postData, string contentType = "application/json")
    {
        var webRequest = UnityWebRequest.Post(
            uri,
            postData,
            contentType
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
                
            }
        }
    }
}