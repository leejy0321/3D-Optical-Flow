using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZenFulcrum.Track;

public class StopCount : MonoBehaviour
{
    int count = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (count > 0)
        {
            transform.GetComponent<Track>().brakes.maxForce = 25;
        }
    }

    private void OnTriggerEnter(Collider other)
    {

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Cart")
        {
            count++;
        }
    }
}
