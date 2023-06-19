using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Settings : MonoBehaviour
{
    public enum Mode
    {
        Forward,
        Reverse,
        Off
    }

    public Mode mode = Mode.Forward;
    public int flowFrame = 10;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
