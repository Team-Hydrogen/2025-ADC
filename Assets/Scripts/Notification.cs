using System;
using TMPro;
using UnityEngine;

public class Notification : MonoBehaviour
{
    private string title;
    private NotificationType type;

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private GameObject closeButton;
    [SerializeField] private GameObject yesButton;
    [SerializeField] private GameObject cancelButton;

    private Action OnYesButtonPressedCallback;

    public void Setup(string title)
    {
        Setup(title, NotificationType.Dismissable, null);
    }

    public void Setup(string title, NotificationType type, Action onYesButtonPressedCallback)
    {
        //transform.position = new Vector3(0, -123, 0);
        //RectTransform rectTransform = GetComponent<RectTransform>();

        RectTransform rectTransform = transform.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(0f, 417f);

        this.title = title;
        this.type = type;

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

        else if (type == NotificationType.Dismissable)
        {
            closeButton.SetActive(true);
        }
    }

    public void OnYesButtonPressed()
    {
        if (type == NotificationType.AskYesCancel || type == NotificationType.AskYesNo)
        {
            OnYesButtonPressedCallback?.Invoke();
            DismissNotification();
        }
    }

    public void DismissNotification()
    {
        Destroy(this.gameObject);
    }

    public enum NotificationType
    {
        Dismissable,
        AskYesNo,
        AskYesCancel
    }
}
