using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ShockWaveEffect : MonoBehaviour
{
    [Header("Shockwave Settings")]
    public float Duration = 0.2f;
    public float Width = 0.2f;        // Shader _Width
    public float Strength = 0.02f;    // Shader _DistortionStrength
    public Color LightColor = Color.white;
    public float Smooth = 1f;
    public bool loop = false;
    public float Size = 4f;

    [Header("References")]
    public Camera ShockWaveCamera;           // 主摄像机，渲染 CaptureMask 层
    public Camera ShockWaveCullingCamera;    // 反向摄像机，渲染 ~CaptureMask 层
    public SpriteRenderer ShockWaveSprite;   // 渲染 Sprite
    public Shader ShockWaveShader;            // 使用的 Shader
    public LayerMask CaptureMask;             // 相机只渲染这些层

    private Material _materialInstance;
    private RenderTexture _rt;          // CaptureMask 层 RT
    private RenderTexture _cullingRT;  // 逆向 CaptureMask 层 RT
    private float _time;

    private static readonly int ShaderTimeID = Shader.PropertyToID("_UpdateTime");
    private static readonly int ShaderWidthID = Shader.PropertyToID("_Width");
    private static readonly int ShaderStrengthID = Shader.PropertyToID("_DistortionStrength");
    private static readonly int ShaderCaptureTexID = Shader.PropertyToID("_CaptureTex");
    private static readonly int ShaderCullingTexID = Shader.PropertyToID("_CullingTex");
    private static readonly int ShaderLitColorID = Shader.PropertyToID("_LightColor");
    private static readonly int ShaderSmoothID = Shader.PropertyToID("_Smooth");

    void Start()
    {
        if (ShockWaveShader == null || ShockWaveCamera == null || ShockWaveCullingCamera == null || ShockWaveSprite == null)
        {
            Debug.LogError("ShockWaveEffect: Missing required components.");
            Destroy(gameObject);
            return;
        }

        ShockWaveSprite.transform.localScale = Vector3.one * Size;
        // 计算RT大小，正方形，基于Sprite大小和像素单位
        float ppu = ShockWaveSprite.sprite.pixelsPerUnit;
        Vector2 worldSize = ShockWaveSprite.bounds.size;
        float maxWorldSize = Mathf.Max(worldSize.x, worldSize.y);

        int rtSize = Mathf.CeilToInt(maxWorldSize * ppu);
        rtSize = Mathf.Clamp(rtSize, 32, 2048); // 限制范围

        // 创建两个RT
        _rt = new RenderTexture(rtSize, rtSize, 0, RenderTextureFormat.ARGB32);
        _rt.Create();

        _cullingRT = new RenderTexture(rtSize, rtSize, 0, RenderTextureFormat.ARGB32);
        _cullingRT.Create();

        // 配置主摄像机
        SetupCamera(ShockWaveCamera, CaptureMask, _rt, maxWorldSize);

        // 配置反向摄像机
        SetupCamera(ShockWaveCullingCamera, ~CaptureMask.value, _cullingRT, maxWorldSize);

        // 关闭自动渲染，手动控制Render()
        ShockWaveCamera.enabled = false;
        ShockWaveCullingCamera.enabled = false;

        // 创建材质实例并赋值
        _materialInstance = new Material(ShockWaveShader);
        _materialInstance.SetFloat(ShaderWidthID, Width);
        _materialInstance.SetFloat(ShaderStrengthID, Strength);
        _materialInstance.SetTexture(ShaderCaptureTexID, _rt);
        _materialInstance.SetTexture(ShaderCullingTexID, _cullingRT);
        _materialInstance.SetColor(ShaderLitColorID,LightColor);
        _materialInstance.SetFloat(ShaderSmoothID, Smooth);

        ShockWaveSprite.material = _materialInstance;

        // 首帧渲染
        ShockWaveCamera.Render();
        ShockWaveCullingCamera.Render();
    }

    void SetupCamera(Camera cam, int cullingMask, RenderTexture targetRT, float maxWorldSize)
    {
        cam.orthographic = true;
        cam.orthographicSize = maxWorldSize / 2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.clear;
        cam.cullingMask = cullingMask;
        cam.targetTexture = targetRT;
    }

    void Update()
    {
        _time += Time.deltaTime;
        float t = Mathf.Clamp01(_time / Duration);
        if (_materialInstance != null)
            _materialInstance.SetFloat(ShaderTimeID, t);

        // 每帧手动渲染两个摄像机
        ShockWaveCamera.Render();
        ShockWaveCullingCamera.Render();

        if (_time >= Duration)
        {
            if (loop)
                _time = 0f;
            else
            {
                Cleanup();
                Destroy(gameObject);
            }
        }
    }

    void Cleanup()
    {
        if (_rt != null)
        {
            _rt.Release();
            Destroy(_rt);
            _rt = null;
        }
        if (_cullingRT != null)
        {
            _cullingRT.Release();
            Destroy(_cullingRT);
            _cullingRT = null;
        }
        if (_materialInstance != null)
        {
            Destroy(_materialInstance);
            _materialInstance = null;
        }
        if (ShockWaveCamera != null)
            ShockWaveCamera.targetTexture = null;
        if (ShockWaveCullingCamera != null)
            ShockWaveCullingCamera.targetTexture = null;
        if (ShockWaveSprite != null)
            ShockWaveSprite.material = null;
    }

    void OnDestroy()
    {
        Cleanup();
    }
}
