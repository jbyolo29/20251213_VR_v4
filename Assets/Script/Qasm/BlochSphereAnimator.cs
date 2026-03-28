using UnityEngine;
using System.Collections.Generic;

public class BlochSphereAnimator : MonoBehaviour
{
    public Transform pointerPivot;
    public Renderer fogRenderer;
    public float smoothSpeed = 5.0f;
    public float pointerLength = 1.2f;

    [Header("Title Label (頂部標題文字)")]
    public Transform titleLabel;
    private Vector3 originalTitleScale;

    [Header("Entanglement Settings (糾纏視覺設定)")]
    public Color pureColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
    public Color entangledColor = new Color(0.1f, 0.2f, 0.7f, 0.95f);

    [Tooltip("糾纏時外層霧氣縮放倍率 (你上次測試 2.0 效果很好)")]
    public float entangledSphereScale = 2.0f;
    public float breatheSpeed = 2.0f;

    [Header("Dark Core Settings (深灰漸層核心)")]
    public Color coreColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
    public float maxCoreScale = 0.8f;

    // ★ 效能優化 1：將過度重疊的 4 層降為 2 層，拯救 VR 效能！
    private int coreLayers = 2;

    private Quaternion targetRotation = Quaternion.identity;
    private Vector3 targetScale = Vector3.one;

    private Vector3 originalFogScale;
    private Transform[] axesAndLabels;
    private Vector3[] originalAxesScales;

    private GameObject[] coreSpheres;
    private Material[] coreMaterials;

    // ★ 效能優化 2：將 Shader 字串轉為整數 ID 快取，避免 Update 迴圈卡頓
    private int id_PointerPos;
    private int id_Radius;
    private int id_Color;
    private int id_BaseColor;
    private int id_TintColor;
    private int id_FogColor;

    void Start()
    {
        if (pointerPivot == null) pointerPivot = transform;

        if (fogRenderer != null) originalFogScale = fogRenderer.transform.localScale;
        if (titleLabel != null) originalTitleScale = titleLabel.localScale;

        // 預先取得所有 Shader 屬性的底層 ID
        id_PointerPos = Shader.PropertyToID("_PointerPos");
        id_Radius = Shader.PropertyToID("_Radius");
        id_Color = Shader.PropertyToID("_Color");
        id_BaseColor = Shader.PropertyToID("_BaseColor");
        id_TintColor = Shader.PropertyToID("_TintColor");
        id_FogColor = Shader.PropertyToID("_FogColor");

        FindAxesAndLabels();
        CreateGradientCore();

        ResetToZero();
    }

    void CreateGradientCore()
    {
        coreSpheres = new GameObject[coreLayers];
        coreMaterials = new Material[coreLayers];

        for (int i = 0; i < coreLayers; i++)
        {
            coreSpheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(coreSpheres[i].GetComponent<Collider>());

            // 綁定在外圍 Sphere 身上，確保縮放比例統一且不抖動
            if (fogRenderer != null)
            {
                coreSpheres[i].transform.SetParent(fogRenderer.transform, false);
                coreMaterials[i] = new Material(fogRenderer.material);
                coreSpheres[i].GetComponent<Renderer>().material = coreMaterials[i];
            }
            else
            {
                coreSpheres[i].transform.SetParent(transform, false);
            }
        }
    }

    void FindAxesAndLabels()
    {
        string[] names = { "Axis_X", "Axis_Y", "Axis_Z", "Label_X", "Label_Y", "Label_Z" };
        List<Transform> found = new List<Transform>();
        List<Vector3> scales = new List<Vector3>();

        Transform searchRoot = transform.parent != null ? transform.parent : transform;

        foreach (string n in names)
        {
            Transform t = searchRoot.Find(n);
            if (t != null)
            {
                found.Add(t);
                scales.Add(t.localScale);
            }
        }
        axesAndLabels = found.ToArray();
        originalAxesScales = scales.ToArray();
    }

    void Update()
    {
        if (pointerPivot == null) return;

        pointerPivot.localRotation = Quaternion.Slerp(pointerPivot.localRotation, targetRotation, Time.deltaTime * smoothSpeed);
        pointerPivot.localScale = Vector3.Lerp(pointerPivot.localScale, targetScale, Time.deltaTime * smoothSpeed);

        UpdateFogInteraction();
    }

    // ★ 效能優化 3：直接用快取的 ID 塞顏色，這對 VR 來說速度極快
    void SetMaterialColorFast(Material mat, Color targetColor)
    {
        if (mat == null) return;
        mat.color = targetColor;

        if (mat.HasProperty(id_Color)) mat.SetColor(id_Color, targetColor);
        if (mat.HasProperty(id_BaseColor)) mat.SetColor(id_BaseColor, targetColor);
        if (mat.HasProperty(id_TintColor)) mat.SetColor(id_TintColor, targetColor);
        if (mat.HasProperty(id_FogColor)) mat.SetColor(id_FogColor, targetColor);
    }

    void UpdateFogInteraction()
    {
        if (fogRenderer == null) return;

        float entanglement = 1.0f - pointerPivot.localScale.x;

        // 更新指針位置
        Vector3 tipPosition = pointerPivot.position + (pointerPivot.up * pointerLength * pointerPivot.localScale.y);
        if (fogRenderer.material.HasProperty(id_PointerPos))
            fogRenderer.material.SetVector(id_PointerPos, tipPosition);

        // 強制維持外層球不隱形 (解決 2.0 倍放大看不見的問題)
        if (fogRenderer.material.HasProperty(id_Radius))
            fogRenderer.material.SetFloat(id_Radius, 1.0f);

        // 1. 外層霧氣變色 
        Color currentOuterColor = Color.Lerp(pureColor, entangledColor, entanglement);
        SetMaterialColorFast(fogRenderer.material, currentOuterColor);

        // 2. 霧氣整體大小控制 (配合你的 2.0 倍巨大化設定)
        float breathe = (entanglement > 0.1f) ? Mathf.Sin(Time.time * breatheSpeed) * 0.05f * entanglement : 0f;
        float baseExpansion = Mathf.Lerp(1.0f, entangledSphereScale, entanglement);
        fogRenderer.transform.localScale = originalFogScale * (baseExpansion + breathe);

        // 3. 座標軸與標題文字消失邏輯
        for (int i = 0; i < axesAndLabels.Length; i++)
        {
            if (axesAndLabels[i] != null)
                axesAndLabels[i].localScale = Vector3.LerpUnclamped(originalAxesScales[i], Vector3.zero, entanglement);
        }
        if (titleLabel != null)
        {
            titleLabel.localScale = Vector3.LerpUnclamped(originalTitleScale, Vector3.zero, entanglement);
        }

        // 4. 漸層核心平滑生長 (極速版)
        if (coreSpheres != null)
        {
            for (int i = 0; i < coreLayers; i++)
            {
                float layerRatio = (i + 1f) / coreLayers;
                float currentScale = entanglement * maxCoreScale * layerRatio;
                coreSpheres[i].transform.localScale = Vector3.one * currentScale;

                float alphaRatio = 1.0f - ((float)i / coreLayers);
                Color c = coreColor;
                c.a = coreColor.a * entanglement * alphaRatio;
                SetMaterialColorFast(coreMaterials[i], c);
            }
        }
    }

    public void SetState(float x, float y, float z, float radius = 1.0f)
    {
        Vector3 targetLocalDir = new Vector3(y, z, x);
        if (targetLocalDir.sqrMagnitude > 0.001f)
            targetRotation = Quaternion.FromToRotation(Vector3.up, targetLocalDir.normalized);

        targetScale = new Vector3(radius, radius, radius);
    }

    public void ResetToZero()
    {
        SetState(0, 0, 1, 1.0f);
    }
}