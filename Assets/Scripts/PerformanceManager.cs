using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PerformanceManager : MonoBehaviour
{
    public static PerformanceManager Instance { get; private set; }

    public enum PerformanceMode
    {
        Default,
        OptimizedNoVisualChange,
        HighPerformance70FPS
    }

    public PerformanceMode currentMode = PerformanceMode.Default;

    int defaultQualityLevel;
    int defaultWidth;
    int defaultHeight;
    bool defaultFullscreen;
    int defaultVSyncCount;
    int defaultTargetFrameRate;
    float defaultShadowDistance;
    int defaultPixelLightCount;
    int defaultTextureLimit;
    int defaultShadowCascades;
    int defaultShadowResolution;
    Volume postProcessVolume;

    List<(Light light, LightShadows shadowMode)> lightShadowStates = new List<(Light, LightShadows)>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        defaultQualityLevel = QualitySettings.GetQualityLevel();
        defaultWidth = Screen.width;
        defaultHeight = Screen.height;
        defaultFullscreen = Screen.fullScreen;
        defaultVSyncCount = QualitySettings.vSyncCount;
        defaultTargetFrameRate = Application.targetFrameRate;
        defaultShadowDistance = QualitySettings.shadowDistance;
        defaultPixelLightCount = QualitySettings.pixelLightCount;
        defaultTextureLimit = QualitySettings.globalTextureMipmapLimit;
        defaultShadowCascades = QualitySettings.shadowCascades;
        defaultShadowResolution = (int)QualitySettings.shadowResolution;
    }

    void Start()
    {
        postProcessVolume = FindFirstObjectByType<Volume>();
    }

    public void SetMode(PerformanceMode mode)
    {
        currentMode = mode;

        switch (mode)
        {
            case PerformanceMode.Default:
                ApplyDefaultMode();
                break;
            case PerformanceMode.OptimizedNoVisualChange:
                ApplyOptimizedNoVisualChange();
                break;
            case PerformanceMode.HighPerformance70FPS:
                ApplyHighPerformance70FPS();
                break;
        }
    }

    public void ApplyDefaultMode()
    {
        QualitySettings.SetQualityLevel(defaultQualityLevel);
        QualitySettings.vSyncCount = defaultVSyncCount;
        Application.targetFrameRate = defaultTargetFrameRate;
        Screen.SetResolution(defaultWidth, defaultHeight, defaultFullscreen);
        QualitySettings.shadowDistance = defaultShadowDistance;
        QualitySettings.pixelLightCount = defaultPixelLightCount;
        QualitySettings.globalTextureMipmapLimit = defaultTextureLimit;
        QualitySettings.shadowCascades = defaultShadowCascades;
        QualitySettings.shadowResolution = (ShadowResolution)defaultShadowResolution;

        RestoreLightShadows();
        SetVolumeActive(true);
    }

    public void ApplyOptimizedNoVisualChange()
    {
        QualitySettings.SetQualityLevel(defaultQualityLevel);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Screen.SetResolution(defaultWidth, defaultHeight, defaultFullscreen);

        QualitySettings.shadowDistance = defaultShadowDistance;
        QualitySettings.pixelLightCount = defaultPixelLightCount;
        QualitySettings.globalTextureMipmapLimit = defaultTextureLimit;
        QualitySettings.shadowCascades = defaultShadowCascades;
        QualitySettings.shadowResolution = (ShadowResolution)defaultShadowResolution;

        RestoreLightShadows();
        SetVolumeActive(true);
    }

    public void ApplyHighPerformance70FPS()
    {
        QualitySettings.SetQualityLevel(0);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 70;

        int newWidth = Mathf.RoundToInt(defaultWidth * 0.75f);
        int newHeight = Mathf.RoundToInt(defaultHeight * 0.75f);
        Screen.SetResolution(newWidth, newHeight, defaultFullscreen);

        QualitySettings.shadowDistance = 0;
        QualitySettings.pixelLightCount = 1;
        QualitySettings.globalTextureMipmapLimit = 1;
        QualitySettings.shadowCascades = 0;
        QualitySettings.shadowResolution = ShadowResolution.Low;

        DisableLightShadows();
        
    }

    void DisableLightShadows()
    {
        lightShadowStates.Clear();
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            lightShadowStates.Add((light, light.shadows));
            light.shadows = LightShadows.None;
        }
    }

    void RestoreLightShadows()
    {
        foreach (var entry in lightShadowStates)
        {
            if (entry.light != null)
                entry.light.shadows = entry.shadowMode;
        }
        lightShadowStates.Clear();
    }

    void SetVolumeActive(bool active)
    {
        if (postProcessVolume != null)
            postProcessVolume.enabled = active;
    }
}
