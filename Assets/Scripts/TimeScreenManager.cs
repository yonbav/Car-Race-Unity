using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeScreenManager : MonoBehaviour
{
    public TimeSpan TotalTime;

    public static int playerLapsCount;
    public static int OpponentLapsCount;
    
    public GameObject TimeValueLabel;


    void Start()    
    {
        TotalTime = new TimeSpan(0);
    }

    void Update()
    {
        TotalTime = TotalTime.Add(TimeSpan.FromSeconds(Time.deltaTime));
        TimeValueLabel.GetComponent<Text>().text =
            $"{TotalTime.Minutes:D2}:{TotalTime.Seconds:D2}:{TotalTime.Milliseconds:D2}";

    }
}
