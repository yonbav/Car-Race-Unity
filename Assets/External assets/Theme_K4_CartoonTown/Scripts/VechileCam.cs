using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VechileCam : MonoBehaviour {

    public Transform target;
    public Vector3 offset = new Vector3(-25, 35, -20);
    public float lag = 0.1f;
	
	// Update is called once per frame
	void Update () {
        var camPosition = target.position + offset;
        camPosition = Vector3.Lerp(transform.position, camPosition, 1 - lag);
        transform.position = camPosition;
        transform.LookAt(target);
	}
}
