using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchCamera : MonoBehaviour
{
    public GameObject CameraOne;
    public GameObject CameraTow;

    private AudioListener CameraOneAudioListener;
    private AudioListener CameraTowAudioListener;
    
    void Start()
    {
        // Getting the audio listeners
        CameraOneAudioListener = CameraOne.GetComponent<AudioListener>();
        CameraTowAudioListener = CameraTow.GetComponent<AudioListener>();
    }
    
    void Update()
    {
        // Checking if one of the keys that changing the camera were pressed
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
        {
            ChangeCamera();
        }
    }

    private void ChangeCamera()
    {
        // Setting camera one 
        CameraTow.SetActive(CameraOne.activeSelf);
        CameraTowAudioListener.enabled = CameraOne.activeSelf;
        
        // Setting camera tow
        CameraOne.SetActive(!CameraOne.activeSelf);
        CameraOneAudioListener.enabled = !CameraOne.activeSelf;
    }
}
