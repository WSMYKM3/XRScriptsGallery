//this script can be attached to any gameobject that needs detect the collision with this owner,and then trigger Playstep or other events
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TriggerZone : MonoBehaviour
{
    public string targetTag;//this need to fill in inspector
    public UnityEvent<GameObject> OnEnterEvent;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == targetTag)
        {
            OnEnterEvent.Invoke(other.gameObject);//here is for trigger Playstep[index]
        }
    }
}
