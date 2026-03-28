using UnityEngine;
using TMPro;

[ExecuteAlways]
public class XYZAxesVisualizer : MonoBehaviour
{
    [Header("Appearance")]
    public float axisLength = 0.5f;
    public float axisWidth = 0.006f;
    public bool showNegativeSide = true;
    public bool showLabels = true;
    public Vector3 centerLocalOverride;

    [Header("Label")]
    public float labelOffset = 0.03f;
    public float labelFontSize = 0.1f;
    public Camera faceCamera;

    LineRenderer lx, ly, lz;
    TextMeshPro tx, ty, tz;
    SphereCollider sphereCol;

    // ★ 定義低彩度(柔和)的顏色
    private Color softRed = new Color(0.85f, 0.45f, 0.45f);
    private Color softGreen = new Color(0.45f, 0.75f, 0.45f);
    private Color softBlue = new Color(0.45f, 0.65f, 0.9f);

    void Awake()
    {
        sphereCol = GetComponent<SphereCollider>();

        // ★ 使用新的低彩度顏色
        EnsureLine(ref lx, "Axis_X", softRed);
        EnsureLine(ref ly, "Axis_Y", softGreen);
        EnsureLine(ref lz, "Axis_Z", softBlue);

        if (showLabels)
        {
            EnsureLabel(ref tx, "Label_X", "X", softRed);
            EnsureLabel(ref ty, "Label_Y", "Y", softGreen);
            EnsureLabel(ref tz, "Label_Z", "Z", softBlue);
        }
    }

    void EnsureLine(ref LineRenderer lr, string name, Color c)
    {
        if (lr == null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            lr = go.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.numCapVertices = 4;
            lr.numCornerVertices = 4;
        }

        lr.useWorldSpace = false;
        lr.startWidth = lr.endWidth = axisWidth;
        lr.startColor = lr.endColor = c;
        lr.positionCount = 2;
    }

    void EnsureLabel(ref TextMeshPro t, string name, string text, Color c)
    {
        var exist = transform.Find(name);
        if (t == null)
        {
            GameObject go = exist ? exist.gameObject : new GameObject(name);
            go.transform.SetParent(transform, false);
            if (!go.TryGetComponent(out t)) t = go.AddComponent<TextMeshPro>();
            t.alignment = TextAlignmentOptions.Center;
            t.color = c;
            t.text = text;
        }
        t.fontSize = labelFontSize;
    }

    Vector3 CenterLocal()
    {
        if (sphereCol) return sphereCol.center;
        return centerLocalOverride;
    }

    void Update()
    {
        if (lx == null || ly == null || lz == null) Awake();

        var cam = faceCamera ? faceCamera : Camera.main;
        Vector3 cL = CenterLocal();
        float L = Mathf.Max(0f, axisLength);

        Vector3 xPosL = cL + Vector3.forward * L;
        Vector3 xNegL = cL + Vector3.forward * -L;
        Vector3 yPosL = cL + Vector3.right * L;
        Vector3 yNegL = cL + Vector3.right * -L;
        Vector3 zPosL = cL + Vector3.up * L;
        Vector3 zNegL = cL + Vector3.up * -L;

        if (showNegativeSide)
        {
            lx.SetPosition(0, xNegL); lx.SetPosition(1, xPosL);
            ly.SetPosition(0, yNegL); ly.SetPosition(1, yPosL);
            lz.SetPosition(0, zNegL); lz.SetPosition(1, zPosL);
        }
        else
        {
            lx.SetPosition(0, cL); lx.SetPosition(1, xPosL);
            ly.SetPosition(0, cL); ly.SetPosition(1, yPosL);
            lz.SetPosition(0, cL); lz.SetPosition(1, zPosL);
        }

        Vector3 CW = transform.TransformPoint(cL);
        Vector3 xPosW = transform.TransformPoint(xPosL);
        Vector3 yPosW = transform.TransformPoint(yPosL);
        Vector3 zPosW = transform.TransformPoint(zPosL);

        if (showLabels)
        {
            tx.fontSize = labelFontSize;
            ty.fontSize = labelFontSize;
            tz.fontSize = labelFontSize;

            PlaceLabel(tx, xPosW, (xPosW - CW).normalized, cam);
            PlaceLabel(ty, yPosW, (yPosW - CW).normalized, cam);
            PlaceLabel(tz, zPosW, (zPosW - CW).normalized, cam);
        }
        else
        {
            if (tx) tx.gameObject.SetActive(false);
            if (ty) ty.gameObject.SetActive(false);
            if (tz) tz.gameObject.SetActive(false);
        }

        SetWidth(lx); SetWidth(ly); SetWidth(lz);
    }

    void PlaceLabel(TextMeshPro t, Vector3 endPos, Vector3 dir, Camera cam)
    {
        if (!t) return;
        t.gameObject.SetActive(true);
        t.transform.position = endPos + dir * labelOffset;
        if (cam) t.transform.rotation = Quaternion.LookRotation(t.transform.position - cam.transform.position);
        t.rectTransform.sizeDelta = new Vector2(0.3f, 0.15f);
    }

    void SetWidth(LineRenderer lr)
    {
        if (!lr) return;
        lr.startWidth = lr.endWidth = axisWidth;
    }

    void OnDisable()
    {
        ClearAxesAndLabels();
    }

    void OnDestroy()
    {
        ClearAxesAndLabels();
    }

    void ClearAxesAndLabels()
    {
        string[] names = { "Axis_X", "Axis_Y", "Axis_Z", "Label_X", "Label_Y", "Label_Z" };
        foreach (var n in names)
        {
            var child = transform.Find(n);
            if (child) DestroyImmediate(child.gameObject);
        }
    }
}