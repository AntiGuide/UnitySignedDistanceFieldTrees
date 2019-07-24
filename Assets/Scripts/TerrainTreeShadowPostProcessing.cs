using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class TerrainTreeShadowPostProcessingRender : PostProcessEffectRenderer<TerrainTreeShadowPostProcessing> {
    private static readonly int inverseView = Shader.PropertyToID("_InverseView");
    private static readonly int shadowColor = Shader.PropertyToID("_ShadowColor");
    private static readonly int sunDirection = Shader.PropertyToID("_SunDirection");
    private static readonly int spheres = Shader.PropertyToID("_Spheres");
    private static readonly int fadeStart = Shader.PropertyToID("_FadeStart");
    private static readonly int fadeDistance = Shader.PropertyToID("_FadeDistance");
    private static readonly int leafTexture = Shader.PropertyToID("_LeafTexture");

    private Shader _shader;
    private ComputeBuffer _spheresBuffer;
    private static readonly int leafTextureScale = Shader.PropertyToID("_LeafTextureScale");
    private static readonly int leafTextureSoftness = Shader.PropertyToID("_LeafTextureSoftness");

    public override DepthTextureMode GetCameraFlags() {
        return DepthTextureMode.Depth;
    }

    public override void Init() {
        base.Init();
        _shader = Shader.Find("Hidden/TerrainTreeShadows");
        var shadowManger = TerrainTreeShadowManager.Instance;
        var treeCount = shadowManger.spheres.Length;
        var sizeSphere = System.Runtime.InteropServices.Marshal.SizeOf(typeof(TerrainTreeShadowManager.Sphere));
        _spheresBuffer = new ComputeBuffer(treeCount, sizeSphere, ComputeBufferType.Default);
        _spheresBuffer.SetData(shadowManger.spheres);
    }

    public override void Release() {
        _spheresBuffer.Release();
        base.Release();
    }

    public override void Render(PostProcessRenderContext context) {
        var shadowManager = TerrainTreeShadowManager.Instance;
        var sheet = context.propertySheets.Get(_shader);
        sheet.properties.SetMatrix(inverseView, context.camera.cameraToWorldMatrix);
        sheet.properties.SetColor(shadowColor, settings.shadowColor.value);
        sheet.properties.SetVector(sunDirection, shadowManager.sun.transform.forward);
        sheet.properties.SetFloat(fadeStart, QualitySettings.shadowDistance - settings.shadowFade.value);
        sheet.properties.SetFloat(fadeDistance, settings.shadowFade.value);
        sheet.properties.SetTexture(leafTexture, settings.leafTexture.value != null ? settings.leafTexture.value : Texture2D.whiteTexture);
        sheet.properties.SetFloat(leafTextureScale, settings.leafTextureScale);
        sheet.properties.SetFloat(leafTextureSoftness, settings.leafTextureSoftness);
        sheet.properties.SetBuffer(spheres, _spheresBuffer);
        
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}

[System.Serializable]
[PostProcess(typeof(TerrainTreeShadowPostProcessingRender), PostProcessEvent.BeforeTransparent, "Custom/Terrain Tree Shadows")]
public class TerrainTreeShadowPostProcessing : PostProcessEffectSettings {
    public ColorParameter shadowColor = new ColorParameter { value = Color.black };
    public FloatParameter shadowFade = new FloatParameter{value = 0};
    public TextureParameter leafTexture = new TextureParameter();
    public FloatParameter leafTextureScale = new FloatParameter();
    public FloatParameter leafTextureSoftness = new FloatParameter();
    
    public override bool IsEnabledAndSupported(PostProcessRenderContext context) {
        return TerrainTreeShadowManager.Instance != null && base.IsEnabledAndSupported(context);
    }
}