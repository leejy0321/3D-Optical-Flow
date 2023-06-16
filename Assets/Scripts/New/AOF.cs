using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.VideoModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Unity.VisualScripting.Member;
using Rect = UnityEngine.Rect;

public class AOF : MonoBehaviour
{
    public bool m_active = true;

    private Mat m_curFrameMat;
    private Mat m_prevFrameMat;
    private MatOfPoint m_corner;
    private MatOfPoint2f m_prevCorner;
    private MatOfPoint2f m_curCorner;
    private MatOfByte m_status;
    private MatOfFloat m_err;

    private List<List<Point>> m_points;

    private int m_frameIdx;


    public bool isLeft;


    private void Awake()
    {
        m_curFrameMat = new Mat();
        m_prevFrameMat = new Mat();
        m_corner = new MatOfPoint();
        m_prevCorner = new MatOfPoint2f();
        m_curCorner = new MatOfPoint2f();
        m_status = new MatOfByte();
        m_err = new MatOfFloat();
    }

    private void Start()
    {
        m_points = new List<List<Point>>();
    }



    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {

        if (isLeft && Camera.current.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left)
        {
            RenderTexture downSampling_left = RenderTexture.GetTemporary(source.width / AOFParams.instance.m_downsamplingCoefficent, source.height / AOFParams.instance.m_downsamplingCoefficent);

            Graphics.Blit(source, downSampling_left);

            Texture2D texture = ConvertTexture2DFromRenderTexture(downSampling_left);

            CalculateOpticalFlow_forLeft(texture);

            AOFParams.instance.m_shader.SetTexture("_SubTex", texture);

            Graphics.Blit(source, destination, AOFParams.instance.m_shader, 0);

            RenderTexture.ReleaseTemporary(downSampling_left);

        }
        else if (isLeft == false && Camera.current.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right)
        {
            RenderTexture downSampling_left = RenderTexture.GetTemporary(source.width / AOFParams.instance.m_downsamplingCoefficent, source.height / AOFParams.instance.m_downsamplingCoefficent);

            Graphics.Blit(source, downSampling_left);
            Texture2D texture = ConvertTexture2DFromRenderTexture(downSampling_left);

            CalculateOpticalFlow_forLeft(texture);

            AOFParams.instance.m_shader.SetTexture("_SubTex", texture);

            Graphics.Blit(source, destination, AOFParams.instance.m_shader, 0);

            RenderTexture.ReleaseTemporary(downSampling_left);
        }
        else
        {
            //Debug.Log("Else");
            Graphics.Blit(source, destination);
        }
    }

    private void CalculateOpticalFlow_forLeft(Texture2D _texture)
    {
        Mat frame = Mat.zeros(_texture.height, _texture.width, CvType.CV_8UC4);
        Mat res = Mat.zeros(_texture.height, _texture.width, CvType.CV_8UC4);

        Utils.texture2DToMat(_texture, frame);
        Imgproc.cvtColor(frame, m_curFrameMat, Imgproc.COLOR_RGBA2GRAY);

        if (m_points.Count > 0)
        {
            List<Point> prevCornerPts = new List<Point>();
            for (int i = 0; i < m_points.Count; i++)
            {
                int lasdIdx = m_points[i].Count - 1;
                prevCornerPts.Add(m_points[i][lasdIdx]);
            }

            m_curCorner.fromList(prevCornerPts);
            Video.calcOpticalFlowPyrLK(m_curFrameMat, m_prevFrameMat, m_curCorner, m_prevCorner, m_status, m_err);

            MatOfPoint2f temp = new MatOfPoint2f();
            Video.calcOpticalFlowPyrLK(m_prevFrameMat, m_curFrameMat, m_curCorner, temp, m_status, m_err);

            List<Point> curPts = m_prevCorner.toList();
            List<Point> tempPts = temp.toList();
            for (int i = 0; i < curPts.Count; i++)
            {
                Point pt = curPts[i] - tempPts[i];
                if (pt.dot(pt) < 3.0)
                {
                    continue;
                }

                m_points[i].Add(curPts[i]);

                if (m_points[i].Count > AOFParams.instance.m_trackCount)
                {
                    m_points[i].RemoveAt(0);
                }

            }

            List<MatOfPoint> matOfPoints = new List<MatOfPoint>();
            for (int i = 0; i < m_points.Count; i++)
            {
                MatOfPoint ptsMat = new MatOfPoint();
                ptsMat.fromList(m_points[i]);
                matOfPoints.Add(ptsMat);
            }
            Imgproc.polylines(res, matOfPoints, false, AOFParams.instance.m_color);
        }

        if (m_frameIdx % AOFParams.instance.m_estimationInterval == 0)
        {
            Imgproc.goodFeaturesToTrack(m_prevFrameMat, 
                m_corner, AOFParams.instance.m_maxCorner, 
                AOFParams.instance.m_qualityLevel, 
                AOFParams.instance.m_minDistance);

            if (m_corner.rows() > 0)
            {
                List<Point> corners = m_corner.toList();

                m_points.Clear();
                for (int i = 0; i < m_corner.rows(); i++)
                {
                    List<Point> pts = new List<Point>();
                    m_points.Add(pts);
                    m_points[i].Add(corners[i]);
                }
            }
        }

        m_frameIdx += 1;
        m_curFrameMat.copyTo(m_prevFrameMat);

        Utils.matToTexture2D(res, _texture);
    }

    private void OnDestroy()
    {
        if (m_curFrameMat != null) m_curFrameMat.Dispose();
        if (m_prevFrameMat != null) m_prevFrameMat.Dispose();
        if (m_corner != null) m_corner.Dispose();
        if (m_prevCorner != null) m_prevCorner.Dispose();
        if (m_curCorner != null) m_curCorner.Dispose();
        if (m_status != null) m_status.Dispose();
        if (m_err != null) m_err.Dispose();
    }


    Texture2D ConvertTexture2DFromRenderTexture(RenderTexture _renderTexture)
    {
        Texture2D tex = new Texture2D(_renderTexture.width, _renderTexture.height, TextureFormat.RGB24, false);
        RenderTexture.active = _renderTexture;
        tex.ReadPixels(new UnityEngine.Rect(0, 0, _renderTexture.width, _renderTexture.height), 0, 0);
        tex.Apply();
        return tex;
    }

}
