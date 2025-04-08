using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine.Networking;

public static class HttpRequest
{
    public static IEnumerator RequestApi(string uri, string postData, string contentType = "application/json",
        Action<string> callback = null)
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
                String notificationErrorMessage = $"Error: {webRequest.error}";
                String warningErrorMessage = webRequest.result.ToString();
                
                UIManager.Instance.ShowNotification(notificationErrorMessage);
                throw new WarningException(warningErrorMessage);
            }
            
            callback?.Invoke(webRequest.downloadHandler.text);
        }
    }
}