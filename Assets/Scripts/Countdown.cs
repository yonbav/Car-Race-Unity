using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.UI;
using UnityStandardAssets.Vehicles.Car;

public class Countdown : MonoBehaviour
{
    public const int CountDownStart = 3;
    public GameObject CountDownLabel;
    public AudioSource GetReady;
    public AudioSource GoAudio;
    public GameObject LapTimer;
    public GameObject UserCarControls;
    public GameObject OpponentCarControls;

    void Start()
    {
        StartCoroutine(StartGame());
    }
    
    IEnumerator StartGame()
    {
        yield return StartCounting();
        ActivateGame();
    }

    IEnumerator StartCounting()
    {
        for (int i = CountDownStart; i > 0; i--)
        {
            CountDownLabel.GetComponent<Text>().text = i.ToString();
            CountDownLabel.SetActive(true);
            yield return new WaitForSeconds(1);
            GetReady.Play();
            CountDownLabel.SetActive(false);
        }
    }

    void ActivateGame()
    {
        GoAudio.Play();
        LapTimer.SetActive(true);
        UserCarControls.GetComponent<CarUserControl>().enabled = true;
        OpponentCarControls.GetComponent<CarAIControl>().enabled = true;
    }
}
