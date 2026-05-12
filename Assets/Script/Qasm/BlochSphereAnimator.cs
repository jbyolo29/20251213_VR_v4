using UnityEngine;
using System.Collections.Generic;

public class BlochSphereAnimator : MonoBehaviour
{
    public Transform pointerPivot;
    public Renderer fogRenderer;

    public float smoothSpeed = 3.0f;
    public float pointerLength = 1.2f;

    [Header("Title Label")]
    public Transform titleLabel;
    private Vector3 originalTitleScale;

    [Header("Entanglement Settings")]
    public Color pureColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
    public Color entangledColor = new Color(0.1f, 0.2f, 0.7f, 0.95f);
    public float entangledSphereScale = 2.0f;
    public float breatheSpeed = 2.0f;

    [Header("Dark Core Settings")]
    public Color coreColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
    public float maxCoreScale = 0.8f;

    private int coreLayers = 2;
    private Vector3 originalFogScale;
    private Transform[] axesAndLabels;
    private Vector3[] originalAxesScales;

    private GameObject[] coreSpheres;
    private Material[] coreMaterials;

    private int id_PointerPos, id_Radius, id_Color, id_BaseColor, id_TintColor, id_FogColor;

    public System.Action onTargetReached;

    private bool isMoving = false;
    private Vector3 animStartDir;
    private Vector3 animTargetDir;
    private Vector3 animStartScale;
    private Vector3 animTargetScale;
    private Vector3 animRotAxis;
    private bool is180Flip;
    private float animProgress;

    // 絕對靜止的布洛赫球座標系 (抓取父物件)
    private Transform SphereRoot => transform.parent != null ? transform.parent : transform;
    private Vector3 Q_X_Axis => SphereRoot.forward;
    private Vector3 Q_Y_Axis => SphereRoot.right;
    private Vector3 Q_Z_Axis => SphereRoot.up;

    void Start()
    {
        if (pointerPivot == null) pointerPivot = transform;
        if (fogRenderer != null) originalFogScale = fogRenderer.transform.localScale;
        if (titleLabel != null) originalTitleScale = titleLabel.localScale;

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
        foreach (string n in names)
        {
            Transform t = SphereRoot.Find(n);
            if (t != null) found.Add(t); scales.Add(t != null ? t.localScale : Vector3.one);
        }
        axesAndLabels = found.ToArray();
        originalAxesScales = scales.ToArray();
    }

    void Update()
    {
        UpdateFogInteraction();

        if (isMoving)
        {
            animProgress += Time.deltaTime * smoothSpeed;
            if (animProgress >= 1f)
            {
                animProgress = 1f;
                isMoving = false;
            }

            float easeProgress = Mathf.SmoothStep(0f, 1f, animProgress);
            pointerPivot.localScale = Vector3.Lerp(animStartScale, animTargetScale, animProgress);

            // 【最乾淨的動畫運算】：
            // 如果是 180 度翻轉，嚴格沿著物理軸向轉 180 度；其他一律走最短弧線
            if (is180Flip)
            {
                pointerPivot.up = Quaternion.AngleAxis(180f * easeProgress, animRotAxis) * animStartDir;
            }
            else
            {
                pointerPivot.up = Vector3.Slerp(animStartDir, animTargetDir, easeProgress);
            }

            if (!isMoving)
            {
                pointerPivot.up = animTargetDir;
                pointerPivot.localScale = animTargetScale;
                onTargetReached?.Invoke();
            }
        }
    }

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
        if (fogRenderer == null || pointerPivot == null) return;
        float entanglement = 1.0f - pointerPivot.localScale.x;
        Vector3 tipPosition = pointerPivot.position + (pointerPivot.up * pointerLength * pointerPivot.localScale.y);
        if (fogRenderer.material.HasProperty(id_PointerPos)) fogRenderer.material.SetVector(id_PointerPos, tipPosition);
        if (fogRenderer.material.HasProperty(id_Radius)) fogRenderer.material.SetFloat(id_Radius, 1.0f);
        Color currentOuterColor = Color.Lerp(pureColor, entangledColor, entanglement);
        SetMaterialColorFast(fogRenderer.material, currentOuterColor);
        float breathe = (entanglement > 0.1f) ? Mathf.Sin(Time.time * breatheSpeed) * 0.05f * entanglement : 0f;
        float baseExpansion = Mathf.Lerp(1.0f, entangledSphereScale, entanglement);
        fogRenderer.transform.localScale = originalFogScale * (baseExpansion + breathe);

        for (int i = 0; i < axesAndLabels.Length; i++)
        {
            if (axesAndLabels[i] != null)
                axesAndLabels[i].localScale = Vector3.LerpUnclamped(originalAxesScales[i], Vector3.zero, entanglement);
        }
        if (titleLabel != null) titleLabel.localScale = Vector3.LerpUnclamped(originalTitleScale, Vector3.zero, entanglement);

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

    // 提供給外部腳本呼叫，用來取得絕對精準的世界方向 (修正光環位置錯誤)
    public Vector3 GetWorldDirection(float qX, float qY, float qZ)
    {
        Vector3 dir = (Q_X_Axis * qX) + (Q_Y_Axis * qY) + (Q_Z_Axis * qZ);
        if (dir.sqrMagnitude < 0.001f) dir = Q_Z_Axis;
        return dir.normalized;
    }

    public void SetState(float qX, float qY, float qZ, float radius = 1.0f, string gate = "")
    {
        if (isMoving) return;

        animTargetDir = GetWorldDirection(qX, qY, qZ);
        animTargetScale = new Vector3(radius, radius, radius);
        animStartDir = pointerPivot.up;
        animStartScale = pointerPivot.localScale;
        gate = string.IsNullOrEmpty(gate) ? "" : gate.ToLower().Trim();

        // 已經抵達終點，直接生成光環
        if (Vector3.Distance(animStartDir, animTargetDir) < 0.01f)
        {
            pointerPivot.up = animTargetDir;
            pointerPivot.localScale = animTargetScale;
            onTargetReached?.Invoke();
            return;
        }

        // 判斷是否為 180 度翻轉，如果是，鎖定對應的旋轉軸
        is180Flip = Vector3.Dot(animStartDir, animTargetDir) < -0.98f;

        if (is180Flip)
        {
            if (gate == "x" || gate == "rx") animRotAxis = Q_X_Axis;
            else if (gate == "y" || gate == "ry") animRotAxis = Q_Y_Axis;
            else if (gate == "z" || gate == "rz") animRotAxis = Q_Z_Axis;
            else animRotAxis = Q_Z_Axis; // 預設防呆
        }

        animProgress = 0f;
        isMoving = true;
    }

    public void ResetToZero()
    {
        SetState(0, 0, 1, 1.0f, "");
    }
}