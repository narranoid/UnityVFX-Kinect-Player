using KinectXEFTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class XEFPlayer : MonoBehaviour
{
    [SerializeField] private float startDelay = 0f;

    [Header("Kinect File Data")]
    [SerializeField] private string xefPath;
    [SerializeField] private bool fromStreamingAssets = true;
    [SerializeField] private bool useCustomFPS = false;
    [SerializeField] private float customFPS = 30f; // Kinect V2 default
    [SerializeField] private bool playEveryFrame = false;

    [Header("Frame Cropping")]
    [SerializeField] private bool croppingEnabled = false;
    [SerializeField] private uint cropStart;
    [SerializeField] private uint stopAtFrame;

    [Header("Color Data")]
    [SerializeField] private Vector2Int colorResolution = new Vector2Int(1920, 1080); // Kinect V2 default
    [SerializeField] private Shader colorConvertShader;
    private RenderTexture colorRenderTexture;
    private Material colorConvertMaterial;

    [Header("Depth Data")]
    [SerializeField] private Vector2Int depthResolution = new Vector2Int(512, 424); // Kinect V2 default
    [SerializeField] private Shader depthConvertShader;
    private RenderTexture depthRenderTexture;
    private Material depthConvertMaterial;

    [Header("Body Index Data")]
    [SerializeField] private Shader bodyIndexConvertShader;
    private RenderTexture bodyIndexRenderTexture;
    private Material bodyIndexConvertMaterial;

    private XEFEventReader reader;
    private XEFEvent currentEvent;
    private int frameCounter = 0;

    private Texture2D colorTexture;
    private Texture2D depthTexture;
    private Texture2D bodyIndexTexture;
    private Coroutine updateCoroutine;
    
    private IDictionary<XEFJointType, Vector3> jointPositions = new Dictionary<XEFJointType, Vector3>();


    public Texture ColorTexture { get => colorRenderTexture; }

    public Texture DepthTexture { get => depthRenderTexture; }

    public Texture BodyIndexTexture { get => bodyIndexRenderTexture; }

    public Vector2Int Resolution { get => colorResolution; }

    public int PointCount { get => colorResolution.x * colorResolution.y; }

    public float Aspect
    {
        get => ((float)colorResolution.x) / ((float)colorResolution.y);
    }

    public Matrix4x4 ProjectionMatrix
    {
        get
        {
            return Matrix4x4.Perspective(53.8f, Aspect, .01f, 8f);
        }
    }

    public Matrix4x4 GPUProjectionMatrix
    {
        get => GL.GetGPUProjectionMatrix(ProjectionMatrix, false);
    }

    public Vector2 FOV
    {
        get => new Vector2(84.1f, 53.8f);
    }

    public Vector2 FocalLength
    {
        get
        {
            return new Vector2(
                (colorResolution.x * 0.5f) / Mathf.Tan(Mathf.Deg2Rad * FOV.x * 0.5f),
                (colorResolution.y * 0.5f) / Mathf.Tan(Mathf.Deg2Rad * FOV.y * 0.5f)
            );
        }
    }

    public Vector3 GetJointPosition(XEFJointType joint)
    {
        if(jointPositions.TryGetValue(joint, out Vector3 pos))
            { return pos; }
        return Vector4.zero;
    }

    private void InitColorTexture()
    {
        if(colorTexture != null)
            { Destroy(colorTexture); }
        colorTexture = new Texture2D(colorResolution.x / 2, colorResolution.y, TextureFormat.RGBA32,
            false, true); // mipCount = false, linear = true
        colorTexture.filterMode = FilterMode.Point;
    }

    private void InitDepthTexture()
    {
        if(depthTexture != null)
            { Destroy(depthTexture); }
        depthTexture = new Texture2D(depthResolution.x, depthResolution.y, TextureFormat.R16,
            false, true); // mipCount = false, linear = true
        depthTexture.filterMode = FilterMode.Point;
    }

    private void OnEnable()
    {
        //Debug.Log("Focal Length: " + FocalLength);
        InitColorTexture();
        InitDepthTexture();

        bodyIndexTexture = new Texture2D(512, 424, TextureFormat.R8,
            false, true); // mipCount = false, linear = true

        colorConvertMaterial = new Material(colorConvertShader);
        colorConvertMaterial.SetInt("_TextureWidth", colorResolution.x);
        colorConvertMaterial.SetInt("_TextureHeight", colorResolution.y);

        depthConvertMaterial = new Material(depthConvertShader);
        depthConvertMaterial.SetInt("_ColorWidth", colorResolution.x);
        depthConvertMaterial.SetInt("_ColorHeight", colorResolution.y);
        depthConvertMaterial.SetInt("_DepthWidth", depthResolution.x);
        depthConvertMaterial.SetInt("_DepthHeight", depthResolution.y);

        bodyIndexConvertMaterial = new Material(bodyIndexConvertShader);
        bodyIndexConvertMaterial.SetInt("_ColorWidth", colorResolution.x);
        bodyIndexConvertMaterial.SetInt("_ColorHeight", colorResolution.y);
        bodyIndexConvertMaterial.SetInt("_BodyIndexWidth", depthResolution.x);
        bodyIndexConvertMaterial.SetInt("_BodyIndexHeight", depthResolution.y);

        updateCoroutine = StartCoroutine(UpdateCoroutine());
    }

    private IEnumerator UpdateCoroutine()
    {
        yield return new WaitForSeconds(startDelay);

        string fullXefPath = xefPath;
        if(fromStreamingAssets)
            { fullXefPath = Path.Combine(Application.streamingAssetsPath, xefPath); }
        reader = new XEFEventReader(fullXefPath);
        
        if(!croppingEnabled || SkipFrames(cropStart))
        while (UpdateData(out bool advanceFrame))
        {
            if (advanceFrame)
            {
                frameCounter++;
                if(croppingEnabled && stopAtFrame > 0 && frameCounter >= stopAtFrame)
                { break; }
                else if(playEveryFrame)
                {
                        yield return null;
                }
                else if (useCustomFPS)
                {
                        yield return new WaitForSecondsRealtime(1f / customFPS);
                }
                else
                { yield return new WaitForSeconds((float)(currentTime - previousTime)); }
            }
                
        }
        CleanUp();
    }

    private bool SkipFrames(uint frameCount)
    {
        for (int i = 0; i < frameCount; i++)
        {
            if ((currentEvent = reader.GetNextEvent(StreamDataTypeIds.UncompressedColor)) == null)
                { return false; }
        }
        return true;
    }

    double currentTime = 0f;
    double previousTime = 0f;

    private bool UpdateData(out bool advanceFrame)
    {
        advanceFrame = false;
        if ((currentEvent = reader.GetNextEvent(new Guid[] {
                StreamDataTypeIds.UncompressedColor,
                StreamDataTypeIds.Depth,
                StreamDataTypeIds.Body,
                StreamDataTypeIds.BodyIndex
                //StreamDataTypeIds.Properties
              })) != null)
        {
            //Debug.Log("Frame: " + currentEvent.FrameIndex);
            byte[] data = currentEvent.EventData;
            if (currentEvent.EventStreamDataTypeId == StreamDataTypeIds.UncompressedColor)
            {
                InitColorTexture();
                colorTexture.LoadRawTextureData(data);
                colorTexture.Apply();
                colorConvertMaterial.SetTexture("_YUYVTexture", colorTexture);

                if(colorRenderTexture != null)
                    { RenderTexture.ReleaseTemporary(colorRenderTexture); }
                colorRenderTexture = RenderTexture.GetTemporary(colorResolution.x, colorResolution.y, 0,
                    RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

                Graphics.Blit(null, colorRenderTexture, colorConvertMaterial);

                previousTime = currentTime;
                currentTime = currentEvent.RelativeTime.Duration().TotalSeconds;
                OnFrameAdvance?.Invoke();
                advanceFrame = true;
            }
            else if(currentEvent.EventStreamDataTypeId == StreamDataTypeIds.Depth)
            {
                InitDepthTexture();

                depthTexture.LoadRawTextureData(data);
                depthTexture.Apply();
                depthConvertMaterial.SetTexture("_DepthTexture", depthTexture);

                if(depthRenderTexture != null)
                    { RenderTexture.ReleaseTemporary(depthRenderTexture); }
                depthRenderTexture = RenderTexture.GetTemporary(colorResolution.x, colorResolution.y, 0,
                    RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);

                Graphics.Blit(null, depthRenderTexture, depthConvertMaterial);
            }
            else if (currentEvent.EventStreamDataTypeId == StreamDataTypeIds.BodyIndex)
            {
                bodyIndexTexture.LoadRawTextureData(data);
                bodyIndexTexture.Apply();
                bodyIndexConvertMaterial.SetTexture("_BodyIndexTexture", bodyIndexTexture);

                if (bodyIndexRenderTexture != null)
                { RenderTexture.ReleaseTemporary(bodyIndexRenderTexture); }
                bodyIndexRenderTexture = RenderTexture.GetTemporary(colorResolution.x, colorResolution.y, 0,
                    RenderTextureFormat.R8, RenderTextureReadWrite.Linear);

                Graphics.Blit(null, bodyIndexRenderTexture, bodyIndexConvertMaterial);
            }
            else if (currentEvent.EventStreamDataTypeId == StreamDataTypeIds.Body)
            {
                XEFBodyFrame bodyFrame = XEFBodyFrame.FromByteArray(data);
                XEFBodyData body = GetFirstTracked(bodyFrame.BodyData);
                //Debug.Log("Body: "+body.TrackingState);
                //Debug.Log("Bodies: "+bodyFrame.BodyData.Length);
                if (body != null && body.TrackingState == XEFBodyTrackingState.TRACKED)
                {
                    foreach(XEFJointType joint in body.SkeletonJointPositions.Keys)
                    {
                        XEFVector xefPos = body.SkeletonJointPositions[joint];
                        /*Debug.Log("XEF Pos of " + joint + 
                            ": X=" + xefPos.x + "  Y=" + xefPos.y +
                            "  Z="+xefPos.z+"  W="+xefPos.w);*/
                        jointPositions[joint] = new Vector3(xefPos.x, xefPos.y, xefPos.z);
                    }
                }
            }

            return true;
        }

        return false;
    }

    private XEFBodyData GetFirstTracked(XEFBodyData[] bodies)
    {
        for (int i = 0; i < bodies.Length; i++)
        {
            if(bodies[i].TrackingState == XEFBodyTrackingState.TRACKED)
            {
                //Debug.Log("Tracked body index: " + i);
                return bodies[i];
            }
        }
        return null;
    }

    public event Action OnFrameAdvance;

    private void OnDisable()
    {
        if(updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            CleanUp();
        }
    }

    private void CleanUp()
    {
        Debug.Log("Played back " + frameCounter + " frames.");
        updateCoroutine = null;
        currentEvent = null;
        if(reader != null) reader.Close();
    }
}
