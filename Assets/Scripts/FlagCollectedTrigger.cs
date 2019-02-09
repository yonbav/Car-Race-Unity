using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class FlagCollectedTrigger : MonoBehaviour
{
    private const int GameWinningScore = 5;
    public static int UserScore;
    public static int OpponentScore;

    public GameObject UserCollectedFlagTrigger;

    public GameObject Gate;
    public GameObject UserCar;
    public GameObject OpponentCar;
    public GameObject UserScoreLabel;
    public GameObject OpponentScoreLabel;

    void Start()
    {
        UserCollectedFlagTrigger.SetActive(true);
    }

    void OnTriggerEnter(Collider other)
    {
        // Removing the gate
        RemoveGate();

        Debug.Log(other.tag);

        // Checking if the opponent car activated the trigger or the user car activated it
        if (OpponentCar.tag == other.tag)
            IncreaseOpponentScore();
        else if (UserCar.tag == other.tag)
            IncreaseUserScore();
        else
            ActivateGate();

        CheckGameEnd();
    }

    private void CheckGameEnd()
    {
        if (OpponentScore == GameWinningScore)
        {
            PlayerLost();
        }

        if (UserScore == GameWinningScore)
        {
            PlayerWon();
        }
    }

    private void PlayerWon()
    {
    }

    private void PlayerLost()
    {
    }

    private void IncreaseOpponentScore()
    {
        // Increasing opponent score when triggers
        OpponentScore++;

        // Changing the opponent score on the score board
        OpponentScoreLabel.GetComponent<Text>().text = OpponentScore.ToString();
    }

    private void IncreaseUserScore()
    {
        // Increasing user score when triggers
        UserScore++;

        // Changing the user score on the score board
        UserScoreLabel.GetComponent<Text>().text = UserScore.ToString();
    }

    private void RemoveGate()
    {
        Gate.SetActive(false);
    }

    private void ActivateGate()
    {
        Gate.SetActive(true);
    }
}
