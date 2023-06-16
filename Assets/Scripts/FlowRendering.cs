using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowRendering : MonoBehaviour
{
    public GameObject cart;

    Rigidbody cartRigid;
    Vector3 prevPos, currPos, nextPos;
    LineRenderer flow;
    int count = 1;
    int pos = 7;
    List<Vector3> posList;
    Vector3[] posArray;

    // Start is called before the first frame update
    void Start()
    {
        cartRigid = cart.GetComponent<Rigidbody>();
        flow = GetComponent<LineRenderer>();
        flow.positionCount = pos;
        currPos = transform.position;
        posList = new List<Vector3>(pos);
        posArray = new Vector3[pos];

        for (int i = 0; i < pos; i++)
        {
            posList.Add(currPos);
        }

        posArray = posList.ToArray();
        flow.SetPositions(posArray);
    }

    // Update is called once per frame
    void Update()
    {
        currPos = transform.position;
        posList.RemoveAt(0);
        posList.Add(currPos);
        posArray = posList.ToArray();
        flow.SetPositions(posArray);
    }
}
