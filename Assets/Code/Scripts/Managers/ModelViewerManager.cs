using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModelViewerManager : MonoBehaviour
{
    [SerializeField] private Transform modelSelectorButtonsParent;
    [SerializeField] private Transform modelsParent;

    private void Start()
    {
        var buttons = modelSelectorButtonsParent.GetComponentsInChildren<Button>();

        for (var i = 0; i < buttons.Length; i++)
        {
            buttons[i].onClick.RemoveAllListeners();
            
            var index = i;
            buttons[i].onClick.AddListener(() => ModelButtonClicked(index));
        }
        
        // Automatically selects the first button.
        buttons[0].Select();
    }
    
    private void ModelButtonClicked(int index)
    {
        for (var i = 0; i < modelsParent.transform.childCount; i++)
        {
            modelsParent.GetChild(i).gameObject.SetActive(i == index);
        }
    }
}