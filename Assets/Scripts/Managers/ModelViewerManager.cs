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
        Button[] buttons = modelSelectorButtonsParent.GetComponentsInChildren<Button>();

        for (int i = 0; i < buttons.Length; i++)
        {
            print(i);
            buttons[i].onClick.RemoveAllListeners();

            int index = i;
            buttons[i].onClick.AddListener(() => ModelButtonClicked(index));
        }
    }

    private void ModelButtonClicked(int index)
    {
        for (int i = 0; i < modelsParent.transform.childCount; i++)
        {
            print("INDEX: " + index + " " + i);
            modelsParent.GetChild(i).gameObject.SetActive(i == index);
        }
    }
}
