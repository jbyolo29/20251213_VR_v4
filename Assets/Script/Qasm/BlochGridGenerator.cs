using UnityEngine;

public class BlochGridGenerator : MonoBehaviour
{
    [Header("球體設定")]
    public float radius = 0.5f;

    [Header("低調水平環設定")]
    public float lineWidth = 0.001f;
    public Color lineColor = new Color(1f, 1f, 1f, 0.12f); // 非常淡的半透明白

    private int segments = 100; // 提高段數讓細線更平滑

    void Start()
    {
        // 依照要求：只畫三條水平環（中間、中間上方、中間下方）
        // 緯度：0度 (赤道), 30度 (上), -30度 (下)
        float[] latitudes = { 0f, 35f, -35f };

        foreach (float angle in latitudes)
        {
            DrawHorizontalCircle(angle);
        }
    }

    void DrawHorizontalCircle(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float y = radius * Mathf.Sin(rad);
        float currentRadius = radius * Mathf.Cos(rad);

        GameObject go = new GameObject("Grid_Lat_" + angleDegrees);
        go.transform.SetParent(this.transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = lineColor;
        lr.startWidth = lr.endWidth = lineWidth;
        lr.loop = true;
        lr.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float theta = (float)i / segments * Mathf.PI * 2f;
            float x = currentRadius * Mathf.Cos(theta);
            float z = currentRadius * Mathf.Sin(theta);
            lr.SetPosition(i, new Vector3(x, y, z));
        }
    }
}