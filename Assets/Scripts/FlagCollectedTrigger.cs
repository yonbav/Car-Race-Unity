using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class FlagCollectedTrigger : MonoBehaviour
{
    public static int userScore;
    
    public GameObject UserCollectedFlagTrigger;
    //public GameObject OpponentCollectedFlagTrigger;

    public GameObject Gate;
    public GameObject UserScore;
    //public GameObject OpponentScore;

    void Start()
    {
        UserCollectedFlagTrigger.SetActive(true);
    }

    void OnTriggerEnter()
    {
        // Increasing user score when triggers
        userScore++;
        
        // Changing the user score on the score board
        UserScore.GetComponent<Text>().text = userScore.ToString();

        // Moving the 
        MoveGate();
    }

    void MoveGate()
    {
        var position = Gate.transform.localPosition;
        position.z += 10;
        var rotation = Gate.transform.localRotation;
        Gate.transform.SetPositionAndRotation(position, rotation); 
    }
}
