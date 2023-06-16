using OpenCVForUnity.CoreModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AOFParams : MonoBehaviour
{
    public static AOFParams instance;

    public int m_maxCorner = 800;
    public double m_qualityLevel = 0.25;
    public double m_minDistance = 7;
    public int m_downsamplingCoefficent = 0;
    public int m_estimationInterval = 5;
    public int m_trackCount = 10;
    public Material m_shader = null;
    public Scalar m_color = new Scalar(255, 255, 255, 24);

    public int m_PointMax = 125;

    public bool isEmptyForLeft = false;
    public bool isEmptyForRight = false;

    private void Awake()
    {
        instance = this;
    }
}
