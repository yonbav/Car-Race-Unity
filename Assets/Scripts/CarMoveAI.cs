using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Vehicles.Car;

public class CarMoveAI : MonoBehaviour
{
    [SerializeField]
    public Transform Destination;
    public GameObject OpponentsCar;

    NavMeshAgent NavMeshAgent;
    
    void Start()
    {
        NavMeshAgent = this.GetComponent<NavMeshAgent>();
    }
    
    void Update()
    {
        if (NavMeshAgent == null)
        {
            Debug.LogError($"The nav mesh agent component is not attached to " + gameObject.name);
        }
        else
        {
            SetDestination();
        }
    }

    private void SetDestination()
    {
        if (Destination != null)
        {
            Vector3 targetVector = Destination.transform.position;
            NavMeshAgent.SetDestination(targetVector);
        }
    }
}
