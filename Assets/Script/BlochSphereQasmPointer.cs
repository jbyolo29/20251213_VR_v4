using UnityEngine;
using QuantumViz.Core; // 直接使用你原本的命名空間

[ExecuteAlways]
[RequireComponent(typeof(SphereCollider))]
public class BlochSphereQasmPointer : MonoBehaviour
{
    [Header("QASM Source")]
    [TextArea(10, 20)]
    public string qasmSource = "OPENQASM 3;\nqubit[1] q;\nh q[0];";

    public int showQubit = 0;

    [Header("Appearance")]
    public float pointerLengthScale = 1.0f;
    public float pointerWidth = 0.02f;
    public Material pointerMaterial;
    public NorthAxis northAxis = NorthAxis.Z;
    public enum NorthAxis { Y, Z }

    private LineRenderer pointer;
    private SphereCollider sphereCol;
    private QasmSimulator sim = new QasmSimulator(); // 使用你原本的模擬器

    // --- 修正 CS1061：補回 Editor 需要的函數，不改動原本邏輯 ---
    public void TestNorth() { DrawBlochVector(0, 0, 1); }
    public void TestEquatorX() { DrawBlochVector(1, 0, 0); }

    public void Run()
    {
        if (!sphereCol) sphereCol = GetComponent<SphereCollider>();
        SetupPointer();

        // 1. 直接執行你原本寫好的模擬器
        // 這裡預設模擬 4 個 Qubit，與你截圖中的 qreg q[4] 一致
        var result = sim.Run(qasmSource, 4, 4);

        // 2. 取得指定位元的密度矩陣
        var rho = result.state.ReducedRho1(showQubit);

        // 3. 透過你原本的 QuantumState 算出座標
        var b = QuantumState.BlochFromRho(rho);

        DrawBlochVector(b.x, b.y, b.z);
    }

    private void DrawBlochVector(double x, double y, double z)
    {
        Vector3 dir = new Vector3((float)x, (float)y, (float)z);
        // 根據你設定的座標軸慣例調整
        Vector3 dirLocal = (northAxis == NorthAxis.Z) ? new Vector3(dir.x, dir.z, dir.y) : dir;

        float r = sphereCol.radius * pointerLengthScale;
        pointer.SetPosition(0, transform.TransformPoint(sphereCol.center));
        pointer.SetPosition(1, transform.TransformPoint(sphereCol.center + dirLocal * r));
        pointer.enabled = true;
    }

    void SetupPointer()
    {
        Transform t = transform.Find("__BlochPointer");
        if (!t)
        {
            GameObject go = new GameObject("__BlochPointer");
            go.transform.SetParent(transform, false);
            pointer = go.AddComponent<LineRenderer>();
        }
        else pointer = t.GetComponent<LineRenderer>();

        pointer.positionCount = 2;
        pointer.useWorldSpace = true;
        pointer.startWidth = pointer.endWidth = pointerWidth;
        pointer.sharedMaterial = pointerMaterial ? pointerMaterial : new Material(Shader.Find("Sprites/Default"));
    }

    void OnValidate() { Run(); }
}