using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowRendering : MonoBehaviour
{
    public GameObject cart;
    public GameObject sphere;
    public GameObject settings;

    Rigidbody cartRigid;
    Vector3 currPos, spherePos, deltaPos;
    LineRenderer flow;
    int count = 1;
    int pos;
    List<Vector3> posList;
    Vector3[] posArray;
    Transform tempTransform;

    // Start is called before the first frame update
    void Start()
    {
        pos = settings.GetComponent<Settings>().flowFrame;
        cartRigid = cart.GetComponent<Rigidbody>();
        flow = GetComponent<LineRenderer>();
        flow.positionCount = pos;
        currPos = transform.position;
        posList = new List<Vector3>(pos);
        posArray = new Vector3[pos];
        tempTransform = sphere.transform;

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
        if (settings.GetComponent<Settings>().mode == Settings.Mode.Off)
            return;

        currPos = transform.position;
        posList.RemoveAt(0);
        posList.Add(currPos);
        posArray = posList.ToArray();

        if (settings.GetComponent<Settings>().mode == Settings.Mode.Reverse)
        {
            spherePos = sphere.transform.position;
            List<Vector3> reverseList = new List<Vector3>();

            foreach (Vector3 point in posList)
            {
                //deltaPos = spherePos - point;
                //Vector3 reversePoint = point + deltaPos * 2;
                //reverseList.Add(reversePoint);
                tempTransform.position = point;
                Vector3 localPoint = tempTransform.localPosition;
                Vector3 reversedLocalPoint = new Vector3(localPoint.x, localPoint.y, -localPoint.z + 1.0f);
                tempTransform.localPosition = reversedLocalPoint;
                Vector3 reversedPoint = tempTransform.position;
                reverseList.Add(reversedPoint);
            }

            posArray = reverseList.ToArray();
        }

        flow.SetPositions(posArray);
    }
}
