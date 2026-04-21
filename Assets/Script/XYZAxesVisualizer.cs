using UnityEngine;
using TMPro;

[ExecuteAlways]
public class XYZAxesVisualizer : MonoBehaviour
{
    [Header("Appearance")]
    [Tooltip("開啟此選項，軸線會自動切齊布洛赫球的邊界")]
    public bool autoFitSphere = true;
    public float manualAxisLength = 0.5f;
    [Range(0, 0.4f)] public float hollowRadius = 0.15f;
    public float axisWidth = 0.005f;
    [Tooltip("是否顯示負向的軸線與量子態")]
    public bool showNegativeSide = true;

    [Header("Label - XYZ")]
    public bool showLabels = true;
    public float labelOffset = 0.05f;
    public float labelFontSize = 0.08f;

    [Header("Label - Quantum States")]
    public bool showQuantumLabels = true;
    [Tooltip("量子態標籤與邊緣(或XYZ標籤)的距離")]
    public float quantumLabelOffset = 0.06f;
    public float quantumFontSize = 0.07f;

    public Camera faceCamera;

    // 線段與標籤參照
    private LineRenderer lx_pos, lx_neg, ly_pos, ly_neg, lz_pos, lz_neg;
    private TextMeshPro tx_pos, ty_pos, tz_pos;
    private TextMeshPro tq_x_pos, tq_x_neg, tq_y_pos, tq_y_neg, tq_z_pos, tq_z_neg;
    private SphereCollider sphereCol;

    // 統一管理視覺物件的根節點
    private Transform visualRoot;

    private Color softRed = new Color(0.85f, 0.45f, 0.45f, 0.8f);
    private Color softGreen = new Color(0.45f, 0.75f, 0.45f, 0.8f);
    private Color softBlue = new Color(0.45f, 0.65f, 0.9f, 0.8f);

    void Awake()
    {
        sphereCol = GetComponent<SphereCollider>();
        Setup();
    }

    void Setup()
    {
        if (visualRoot == null)
        {
            Transform existingRoot = transform.Find("VisualElements");
            if (existingRoot == null)
            {
                GameObject rootObj = new GameObject("VisualElements");
                rootObj.transform.SetParent(transform, false);
                visualRoot = rootObj.transform;
            }
            else
            {
                visualRoot = existingRoot;
            }
        }

        EnsureLine(ref lx_pos, "Axis_X_Pos", softRed);
        EnsureLine(ref lx_neg, "Axis_X_Neg", softRed);
        EnsureLine(ref ly_pos, "Axis_Y_Pos", softGreen);
        EnsureLine(ref ly_neg, "Axis_Y_Neg", softGreen);
        EnsureLine(ref lz_pos, "Axis_Z_Pos", softBlue);
        EnsureLine(ref lz_neg, "Axis_Z_Neg", softBlue);

        EnsureLabel(ref tx_pos, "Label_X", "X", softRed, labelFontSize);
        EnsureLabel(ref ty_pos, "Label_Y", "Y", softGreen, labelFontSize);
        EnsureLabel(ref tz_pos, "Label_Z", "Z", softBlue, labelFontSize);

        // 修正：使用鍵盤標準的 > 符號，解決 TextMeshPro 預設字體不支援特殊符號導致破圖的問題
        EnsureLabel(ref tq_x_pos, "Label_Q_X_Pos", "|+>", softRed, quantumFontSize);
        EnsureLabel(ref tq_x_neg, "Label_Q_X_Neg", "|->", softRed, quantumFontSize);
        EnsureLabel(ref tq_y_pos, "Label_Q_Y_Pos", "|-i>", softGreen, quantumFontSize);
        EnsureLabel(ref tq_y_neg, "Label_Q_Y_Neg", "|i>", softGreen, quantumFontSize);
        EnsureLabel(ref tq_z_pos, "Label_Q_Z_Pos", "|0>", softBlue, quantumFontSize);
        EnsureLabel(ref tq_z_neg, "Label_Q_Z_Neg", "|1>", softBlue, quantumFontSize);
    }

    void Update()
    {
        if (visualRoot == null || lx_pos == null) Setup();
        var cam = faceCamera ? faceCamera : Camera.main;

        // ★ 修正：直接使用 Local Space 的半徑，讓 Unity 引擎自己去處理縮放，解決「雙重縮小」Bug
        float L = manualAxisLength;
        if (autoFitSphere && sphereCol != null)
        {
            L = sphereCol.radius;
        }

        float hR = Mathf.Min(hollowRadius, L - 0.05f);

        DrawAxis(lx_pos, Vector3.forward, hR, L, true);
        DrawAxis(ly_pos, Vector3.right, hR, L, true);
        DrawAxis(lz_pos, Vector3.up, hR, L, true);

        DrawAxis(lx_neg, -Vector3.forward, hR, L, showNegativeSide);
        DrawAxis(ly_neg, -Vector3.right, hR, L, showNegativeSide);
        DrawAxis(lz_neg, -Vector3.up, hR, L, showNegativeSide);

        UpdateLabel(tx_pos, Vector3.forward * (L + labelOffset), cam, showLabels);
        UpdateLabel(ty_pos, Vector3.right * (L + labelOffset), cam, showLabels);
        UpdateLabel(tz_pos, Vector3.up * (L + labelOffset), cam, showLabels);

        float qOffsetPos = L + labelOffset + (showLabels ? quantumLabelOffset : 0f);
        float qOffsetNeg = L + quantumLabelOffset;

        UpdateLabel(tq_x_pos, Vector3.forward * qOffsetPos, cam, showQuantumLabels);
        UpdateLabel(tq_x_neg, -Vector3.forward * qOffsetNeg, cam, showQuantumLabels && showNegativeSide);
        UpdateLabel(tq_y_pos, Vector3.right * qOffsetPos, cam, showQuantumLabels);
        UpdateLabel(tq_y_neg, -Vector3.right * qOffsetNeg, cam, showQuantumLabels && showNegativeSide);
        UpdateLabel(tq_z_pos, Vector3.up * qOffsetPos, cam, showQuantumLabels);
        UpdateLabel(tq_z_neg, -Vector3.up * qOffsetNeg, cam, showQuantumLabels && showNegativeSide);
    }

    void DrawAxis(LineRenderer lr, Vector3 dir, float start, float end, bool isVisible)
    {
        if (!lr) return;
        lr.gameObject.SetActive(isVisible);
        if (!isVisible) return;

        lr.SetPosition(0, dir * start);
        lr.SetPosition(1, dir * end);
        lr.startWidth = lr.endWidth = axisWidth;
    }

    void UpdateLabel(TextMeshPro t, Vector3 localPos, Camera cam, bool isVisible)
    {
        if (!t) return;
        t.gameObject.SetActive(isVisible);
        if (!isVisible) return;

        t.transform.localPosition = localPos;
        if (cam) t.transform.rotation = Quaternion.LookRotation(t.transform.position - cam.transform.position);
    }

    void EnsureLine(ref LineRenderer lr, string objName, Color c)
    {
        Transform child = visualRoot.Find(objName);
        GameObject go = child != null ? child.gameObject : new GameObject(objName);
        go.transform.SetParent(visualRoot, false);

        lr = go.GetComponent<LineRenderer>();
        if (lr == null) lr = go.AddComponent<LineRenderer>();

        if (lr.sharedMaterial == null) lr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        lr.useWorldSpace = false;
        lr.startColor = lr.endColor = c;
        lr.positionCount = 2;
    }

    void EnsureLabel(ref TextMeshPro t, string objName, string text, Color c, float fontSize)
    {
        Transform child = visualRoot.Find(objName);
        GameObject go = child != null ? child.gameObject : new GameObject(objName);
        go.transform.SetParent(visualRoot, false);

        t = go.GetComponent<TextMeshPro>();
        if (t == null) t = go.AddComponent<TextMeshPro>();

        t.alignment = TextAlignmentOptions.Center;
        t.color = c;
        t.text = text;
        t.fontSize = fontSize;
    }
}