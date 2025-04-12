using System;
using TMPro;
using UnityEngine;

public class Notification : MonoBehaviour
{
    #region References
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private GameObject closeButton;
    [SerializeField] private GameObject yesButton;
    [SerializeField] private GameObject cancelButton;
    #endregion
    
    #region Private Variables
    private string _title;
    private NotificationType _type;
    
    private RectTransform _rectTransform;
    #endregion
    
    #region Actions
    private Action OnYesButtonPressedCallback;
    #endregion
    
    #region Event Functions
    
    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
    }
    
    #endregion
    
    public void Setup(string title)
    {
        Setup(title, NotificationType.Dismissible, null);
    }

    public void Setup(string title, NotificationType type, Action onYesButtonPressedCallback)
    {
        //transform.position = new Vector3(0, -123, 0);
        //RectTransform rectTransform = GetComponent<RectTransform>();
        
        _rectTransform.anchoredPosition = new Vector2(0f, 417f);
        
        _title = title;
        _type = type;
        
        titleText.text = title;
        
        closeButton.SetActive(false);
        yesButton.SetActive(false);
        cancelButton.SetActive(false);

        if (type is NotificationType.AskYesCancel or NotificationType.AskYesNo)
        {
            yesButton.SetActive(true);
            cancelButton.SetActive(true);

            if (onYesButtonPressedCallback != null)
            {
                OnYesButtonPressedCallback = onYesButtonPressedCallback;
            }

            else
            {
                Debug.LogError("No \"Yes Button Pressed\" callback provided for notification");
            }
        }

        else if (type == NotificationType.Dismissible)
        {
            closeButton.SetActive(true);
        }
    }
    
    public void OnYesButtonPressed()
    {
        if (_type != NotificationType.AskYesCancel && _type != NotificationType.AskYesNo)
        {
            return;
        }
        
        OnYesButtonPressedCallback?.Invoke();
        DismissNotification();
    }

    public void DismissNotification()
    {
        Destroy(this.gameObject);
    }

    public enum NotificationType
    {
        Dismissible,
        AskYesNo,
        AskYesCancel
    }
}
