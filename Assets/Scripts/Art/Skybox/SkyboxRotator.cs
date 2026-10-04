using UnityEngine;
using UnityEngine.SceneManagement;

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

    private bool activated;

    private float currentRotation;


    /// <summary>
    /// 如果直接单独运行 3DScene，
    /// 保持原来的“自动开始”体验。
    ///
    /// Additive 加载时：
    /// 当前 Active Scene 还是 SampleScene，
    /// 因此这里不会提前初始化，
    /// 而是等待 World 生命周期显式 Activate。
    /// </summary>
    private void Start()
    {
        
        if (gameObject.scene ==
            SceneManager.GetActiveScene())
        {
            if (!TryActivate(
                    out string error))
            {
                Debug.LogWarning(
                    $"[SkyboxRotator] {error}",
                    this);
            }
        }
    }


    private void Update()
    {
        if (!activated ||
            skyboxMaterial == null)
        {
            return;
        }

        float delta =
            useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

        currentRotation +=
            angularYSpeed * delta;

        currentRotation =
            Mathf.Repeat(
                currentRotation,
                360f);

        skyboxMaterial.SetFloat(
            "_Rotation",
            currentRotation);
    }


    /// <summary>
    /// 在对应 World Scene 已经成为 Active Scene 后调用。
    ///
    /// 可以重复调用；
    /// 已经激活时直接成功。
    /// </summary>
    public bool TryActivate(
        out string error)
    {
        if (activated)
        {
            error = null;
            return true;
        }


        originalSkybox =
            RenderSettings.skybox;


        if (originalSkybox == null)
        {
            error =
                $"活动场景 " +
                $"'{SceneManager.GetActiveScene().name}' " +
                $"没有设置 Skybox。";

            return false;
        }


        Material candidate;

        if (instantiateMaterial)
        {
            candidate =
                new Material(
                    originalSkybox);
        }
        else
        {
            candidate =
                originalSkybox;
        }


        if (!candidate.HasProperty(
                "_Rotation"))
        {
            if (instantiateMaterial)
            {
                Destroy(candidate);
            }

            error =
                $"Skybox Material " +
                $"'{originalSkybox.name}' " +
                $"的 Shader 没有 _Rotation 属性。";

            return false;
        }


        skyboxMaterial =
            candidate;


        if (instantiateMaterial)
        {
            RenderSettings.skybox =
                skyboxMaterial;

            createdInstance =
                true;
        }


        currentRotation =
            skyboxMaterial.GetFloat(
                "_Rotation");


        activated =
            true;


        error = null;

        return true;
    }


    /// <summary>
    /// World 放弃 Environment ownership 时调用。
    /// </summary>
    public void Deactivate()
    {
        if (!activated)
            return;


        if (createdInstance &&
            skyboxMaterial != null)
        {
            if (RenderSettings.skybox ==
                skyboxMaterial)
            {
                RenderSettings.skybox =
                    originalSkybox;
            }

            Destroy(
                skyboxMaterial);
        }


        originalSkybox =
            null;

        skyboxMaterial =
            null;

        createdInstance =
            false;

        activated =
            false;
    }


    private void OnDestroy()
    {
        Deactivate();
    }
}