using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Antennas")]
    [SerializeField] private List<string> antennaNames = new List<string>();
    [SerializeField] private List<TextMeshProUGUI> antennaText = new List<TextMeshProUGUI>();
    [SerializeField] private List<Color> antennaTextColor = new List<Color>();

    [Header("Time Counter")]
    [SerializeField] private TextMeshProUGUI dayCounter;
    [SerializeField] private TextMeshProUGUI hourCounter;
    [SerializeField] private TextMeshProUGUI minuteCounter;
    [SerializeField] private TextMeshProUGUI secondCounter;

    private static readonly Color DisabledAntennaTextColor = new Color(0.8f, 0.8f, 0.8f);

    // Start is called before the first frame update
    void Start()
    {
        UpdateAntenna("DSS24", 123684);
        UpdateAntenna("DSS34", 0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DisableAntenna()
    {
        
    }

    public void UpdateAntenna(string antennaName, int connectionSpeed)
    {
        const string connectionSpeedUnit = "kbps";
        // Gets the index of the antenna name and maps it to its text object.
        int index = antennaNames.IndexOf(antennaName);
        // The default display of the antenna name is added.
        antennaText[index].text = antennaName;
        antennaText[index].color = DisabledAntennaTextColor;
        // If the connection speed exceeds 0, it is displayed.
        if (connectionSpeed > 0)
        {
            antennaText[index].text += ": " + connectionSpeed.ToString("N0") + " " + connectionSpeedUnit;
            antennaText[index].color = antennaTextColor[index];
        }
    }

    public void SetTime(int days, int hours, int minutes, int seconds)
    {
        dayCounter.text = days.ToString();
        hourCounter.text = hours.ToString();
        minuteCounter.text = minutes.ToString();
        secondCounter.text = seconds.ToString();
    }

    public void IncrementTime(int changeInDays, int changeInHours, int changeInMinutes, int changeInSeconds)
    {
        dayCounter.text = (int.Parse(dayCounter.text) - changeInDays).ToString();
        hourCounter.text = (int.Parse(hourCounter.text) - changeInHours).ToString();
        minuteCounter.text = (int.Parse(minuteCounter.text) - changeInMinutes).ToString();
        secondCounter.text = (int.Parse(secondCounter.text) - changeInSeconds).ToString();
    }
}
