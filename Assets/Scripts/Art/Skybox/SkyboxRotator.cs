using UnityEngine;

[DisallowMultipleComponent]
public class SkyboxRotator : MonoBehaviour
{
    [Header("天空盒绕 Y 轴旋转速度（度/秒）")]
    public float angularYSpeed = 5f;

    [Header("使用不受 Time.timeScale 影响的时间")]
    public bool useUnscaledTime = false;

    [Header("运行时实例化天空盒材质，避免修改项目中的材质资产")]
    public bool instantiateMaterial = true;

    private Material originalSkybox;
    private Material skyboxMaterial;
    private bool createdInstance;
    private float currentRotation;

    private void Start()
    {
        originalSkybox = RenderSettings.skybox;

        if (originalSkybox == null)
        {
            Debug.LogWarning("[SkyboxRotator] 当前场景没有设置 Skybox。");
            enabled = false;
            return;
        }

        if (instantiateMaterial)
        {
            skyboxMaterial = new Material(originalSkybox);
            RenderSettings.skybox = skyboxMaterial;
            createdInstance = true;
        }
        else
        {
            skyboxMaterial = originalSkybox;
        }

        if (!skyboxMaterial.HasProperty("_Rotation"))
        {
            Debug.LogWarning("[SkyboxRotator] 当前天空盒 Shader 没有 _Rotation 属性，无法旋转。");
            enabled = false;
            return;
        }

        currentRotation = skyboxMaterial.GetFloat("_Rotation");
    }

    private void Update()
    {
        if (skyboxMaterial == null)
            return;

        float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        currentRotation += angularYSpeed * delta;
        currentRotation = Mathf.Repeat(currentRotation, 360f);

        skyboxMaterial.SetFloat("_Rotation", currentRotation);
    }

    private void OnDestroy()
    {
        if (createdInstance && skyboxMaterial != null)
        {
            if (RenderSettings.skybox == skyboxMaterial)
            {
                RenderSettings.skybox = originalSkybox;
            }

            Destroy(skyboxMaterial);
        }
    }
}