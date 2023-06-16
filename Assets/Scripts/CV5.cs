using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.VideoModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CV5 : MonoBehaviour
{
    [SerializeField] private int    m_maxCorner = 800;
    [SerializeField] private double m_qualityLevel = 0.25;
    [SerializeField] private double m_minDistance = 7;
    [SerializeField] private int    m_downsamplingCoefficent = 0;
    [SerializeField] private int    m_estimationInterval = 5;
    [SerializeField] private int    m_trackCount = 10; // Track Count는 Estimation Interval보다 크면 소용 없다.
    [SerializeField] private float  m_lineLengthCoefficent = 2.0f;
    [SerializeField] private Material m_shader = null;
    [SerializeField] private Scalar m_color = new Scalar(255, 255, 255, 100);
    [SerializeField] private Scalar m_circleColor = new Scalar(255, 255, 255, 100);

    private Mat                     m_curFrameMat;
    private Mat                     m_prevFrameMat;
    private MatOfPoint              m_corner;
    private MatOfPoint2f            m_prevCorner;
    private MatOfPoint2f            m_curCorner;
    private MatOfByte               m_status;
    private MatOfFloat              m_err;
    private Texture2D               m_texture;
    private List<List<Point>>       m_points;
    private int                     m_frameIdx;
    private Scalar                  m_colorBlack = new Scalar(0, 0, 0, 255);
    private int                     m_width = 1920;
    private int                     m_height = 1080;

    private void Awake()
    {
        m_curFrameMat  = new Mat();
        m_prevFrameMat = new Mat();
        m_corner       = new MatOfPoint();
        m_prevCorner   = new MatOfPoint2f();
        m_curCorner    = new MatOfPoint2f();
        m_status       = new MatOfByte();
        m_err          = new MatOfFloat();
    }

    private void Start()
    {
        m_downsamplingCoefficent = (int)Mathf.Pow(2.0f, m_downsamplingCoefficent);
        m_points = new List<List<Point>>();

        m_width = Screen.width;
        m_height = Screen.height;
    }

    private void OnRenderImage(RenderTexture _source, RenderTexture _destination)
    {
        // 다운 샘플링
        RenderTexture downSampling = RenderTexture.GetTemporary(_source.width / m_downsamplingCoefficent, _source.height / m_downsamplingCoefficent);
        Graphics.Blit(_source, downSampling);

        Texture2D texture = ConvertTexture2DFromRenderTexture(downSampling);
        RenderTexture.ReleaseTemporary(downSampling);

        CalculateOpticalFlow(texture);

        m_shader.SetTexture("_SubTex", texture);
        Graphics.Blit(_source, _destination, m_shader);
    }

    private void CalculateOpticalFlow(Texture2D _texture)
    {
        Mat frame = new Mat(_texture.height, _texture.width, CvType.CV_8UC4);
        Mat res = Mat.zeros(_texture.height, _texture.width, CvType.CV_8UC4);
        
        Utils.texture2DToMat(_texture, frame);
        Imgproc.cvtColor(frame, m_curFrameMat, Imgproc.COLOR_RGBA2GRAY);

        // 이전 프레임에 계산 되었는지?
        if (m_points.Count > 0)
        {
            // 가장 마지막에 추가한 포인트를 가져온다.
            List<Point> prevCornerPts = new List<Point>();
            for (int i = 0; i < m_points.Count; i++)
            {
                int lasdIdx = m_points[i].Count - 1;
                prevCornerPts.Add(m_points[i][lasdIdx]);
            }

            m_prevCorner.fromList(prevCornerPts);

            // 이전 프레임->현재 프레임으로 이동할 때, 이동한 코너들을 계산한다.
            Video.calcOpticalFlowPyrLK(m_prevFrameMat, m_curFrameMat, m_prevCorner, m_curCorner, m_status, m_err);

            List<Point> prevPts = m_prevCorner.toList();
            List<Point> curPts = m_curCorner.toList();
            for (int i = 0; i < curPts.Count; i++)
            {
                
                m_points[i].Add(curPts[i]);
;
                if (m_points[i].Count > m_trackCount)
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
            Imgproc.polylines(res, matOfPoints, false, m_color);
        }

        // 특정 간격마다 Featrue를 추출한다.
        if(m_frameIdx % m_estimationInterval == 0)
        {
            Imgproc.goodFeaturesToTrack(m_curFrameMat, m_corner, m_maxCorner, m_qualityLevel, m_minDistance);

            if (m_corner.rows() > 0)
            {
                List<Point> corners = m_corner.toList();

                // 기존 포인트들을 다 지운다.
                m_points.Clear();

                // 추출한 코너들을 포인트 리스트의 리스트에 추가한다.
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
        if(m_curFrameMat != null) m_curFrameMat.Dispose();
        if(m_prevFrameMat != null) m_prevFrameMat.Dispose();
        if(m_corner != null) m_corner.Dispose();
        if(m_prevCorner != null) m_prevCorner.Dispose();
        if(m_curCorner != null) m_curCorner.Dispose();
        if(m_status != null) m_status.Dispose();
        if(m_err != null) m_err.Dispose();
    }

    Texture2D ConvertTexture2DFromRenderTexture(RenderTexture _renderTexture)
    {
        Texture2D tex = new Texture2D(_renderTexture.width, _renderTexture.height, TextureFormat.RGB24, false);
        RenderTexture.active = _renderTexture;
        tex.ReadPixels(new UnityEngine.Rect(0, 0, _renderTexture.width, _renderTexture.height), 0, 0);
        tex.Apply();
        return tex;
    }

    float WeightByScreenDistance(float _width, float _height, double _x, double _y)
    {
        float textureLength = Mathf.Sqrt(
            Mathf.Pow(_width / 2.0f, 2.0f)
          + Mathf.Pow(_height / 2.0f, 2.0f));

        float length = Mathf.Sqrt(
            Mathf.Pow((_width/ 2.0f) - (float)_x, 2.0f) 
          + Mathf.Pow((_height / 2.0f) - (float)_y / 2, 2.0f));
        
        return length / textureLength;
    }
}