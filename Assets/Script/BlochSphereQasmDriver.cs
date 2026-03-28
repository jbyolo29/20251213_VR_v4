using UnityEngine;
using TMPro;
using QuantumViz.Core;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(LineRenderer))]
public class BlochSphereQasmDriver : MonoBehaviour
{
    [Header("Target Qubit")]
    // ★ 加入這個，讓你可以在 Inspector 直接填 0 或 1
    public int qubitIndex = 0;

    [Header("UI (Optional)")]
    public TMP_InputField qasmInput;
    public TMP_Dropdown qubitDropdown; // 如果有指定 qubitIndex，這個選單會被無視
    public TextMeshProUGUI statusText;

    [Header("Visuals")]
    public TextMeshProUGUI overlayText;
    public TextMeshPro worldText;
    public bool worldTextFollow = true;

    public enum NorthAxis { Y, Z }
    public NorthAxis northAxis = NorthAxis.Z;

    private SphereCollider sphereCol;
    private LineRenderer line;
    private BlochSphereTouch touchComp;
    private QasmSimulator sim = new QasmSimulator();

    void Awake()
    {
        sphereCol = GetComponent<SphereCollider>();
        line = GetComponent<LineRenderer>();
        touchComp = GetComponent<BlochSphereTouch>();

        if (line != null)
        {
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = 0.005f;
            line.endWidth = 0.005f;
            if (line.material == null)
                line.material = new Material(Shader.Find("Sprites/Default"));
        }
    }

    public void RunFromUI()
    {
        string src = qasmInput ? qasmInput.text : "";
        int nq = GuessSize(src, "qubit", 4);
        int nc = GuessSize(src, "bit", nq);

        // ★ 邏輯修正：優先使用手動填寫的 qubitIndex
        int k = qubitIndex;

        // 如果你還是想用 Dropdown 控制，就維持原本邏輯（但我們現在並列兩顆球，通常手動填比較好）
        if (qubitDropdown != null)
        {
            k = Mathf.Clamp(qubitDropdown.value, 0, nq - 1);
        }

        var result = sim.Run(src, nq, nc);
        var rho = result.state.ReducedRho1(k);
        var bloch = QuantumState.BlochFromRho(rho);
        DrawBlochVector(bloch.x, bloch.y, bloch.z);

        if (statusText)
        {
            string cbitsStr = string.Join("", result.cbits);
            statusText.text = "qubits:" + nq + "  show q[" + k + "]  cbits:" + cbitsStr;
        }
    }

    public void DrawBlochVector(double x, double y, double z)
    {
        if (touchComp && touchComp.enabled) touchComp.enabled = false;

        Vector3 dirLocalPhys = new Vector3((float)x, (float)y, (float)z);
        if (dirLocalPhys.sqrMagnitude < 1e-8f) dirLocalPhys = Vector3.forward;

        Vector3 dirLocal = (northAxis == NorthAxis.Z)
            ? dirLocalPhys.normalized
            : new Vector3(dirLocalPhys.x, dirLocalPhys.z, dirLocalPhys.y).normalized;

        Vector3 centerLocal = sphereCol.center;
        float r = sphereCol.radius;
        Vector3 contactLocal = centerLocal + dirLocal * r;

        Vector3 centerWorld = transform.TransformPoint(centerLocal);
        Vector3 contactWorld = transform.TransformPoint(contactLocal);

        if (line)
        {
            line.enabled = true;
            line.SetPosition(0, centerWorld);
            line.SetPosition(1, contactWorld);
        }

        ComputeAngles(dirLocal, out float thetaDeg, out float phiDeg);

        if (overlayText)
        {
            overlayText.text =
                "θ: " + thetaDeg.ToString("0.0") + "°   φ: " + phiDeg.ToString("0.0") + "°\n" +
                "X: " + dirLocal.z.ToString("+0.000;-0.000;0.000") + "\n" +
                "Y: " + dirLocal.x.ToString("+0.000;-0.000;0.000") + "\n" +
                "Z: " + dirLocal.y.ToString("+0.000;-0.000;0.000");
        }

        if (worldText)
        {
            worldText.text = "θ " + thetaDeg.ToString("0.0") + "°\nφ " + phiDeg.ToString("0.0") + "°";
            worldText.gameObject.SetActive(true);
            if (worldTextFollow)
            {
                Vector3 nudge = (contactWorld - centerWorld).normalized * 0.02f;
                worldText.transform.position = contactWorld + nudge;
                if (Camera.main)
                {
                    worldText.transform.rotation =
                        Quaternion.LookRotation(Camera.main.transform.position - worldText.transform.position);
                }
            }
        }
    }

    void ComputeAngles(Vector3 dirLocal, out float thetaDeg, out float phiDeg)
    {
        dirLocal.Normalize();
        const float eps = 1e-7f;

        if (northAxis == NorthAxis.Y)
        {
            thetaDeg = Mathf.Acos(Mathf.Clamp(dirLocal.y, -1f, 1f)) * Mathf.Rad2Deg;
            float phi = Mathf.Atan2(dirLocal.z, dirLocal.x) * Mathf.Rad2Deg;
            phiDeg = (phi % 360f + 360f) % 360f;
            if (Mathf.Abs(dirLocal.x) < eps && Mathf.Abs(dirLocal.z) < eps) phiDeg = 0f;
        }
        else
        {
            thetaDeg = Mathf.Acos(Mathf.Clamp(dirLocal.z, -1f, 1f)) * Mathf.Rad2Deg;
            float phi = Mathf.Atan2(dirLocal.y, dirLocal.x) * Mathf.Rad2Deg;
            phiDeg = (phi % 360f + 360f) % 360f;
            if (Mathf.Abs(dirLocal.x) < eps && Mathf.Abs(dirLocal.y) < eps) phiDeg = 0f;
        }
    }

    int GuessSize(string src, string key, int def)
    {
        if (string.IsNullOrEmpty(src)) return def;
        var lines = src.Split('\n');
        for (int idx = 0; idx < lines.Length; idx++)
        {
            string s = lines[idx].Trim().Replace(" ", "");
            int i = s.IndexOf(key + "[");
            if (i >= 0)
            {
                int j = s.IndexOf(']', i + key.Length + 1);
                if (j > i)
                {
                    string num = s.Substring(i + key.Length + 1, j - (i + key.Length + 1));
                    if (int.TryParse(num, out int n))
                        return Mathf.Clamp(n, 1, 10);
                }
            }
        }
        return def;
    }
}