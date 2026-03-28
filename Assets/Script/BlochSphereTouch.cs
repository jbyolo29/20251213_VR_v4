using UnityEngine;
using TMPro;
using Oculus.VR;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(LineRenderer))]
public class BlochSphereTouch : MonoBehaviour
{
    public OVRSkeleton leftHandSkeleton;
    public OVRSkeleton rightHandSkeleton;

    [Header("Detection")]
    [Tooltip("判定『碰到球面』的容許誤差（公尺）。")]
    public float touchSlack = 0.2f; // 2cm 容許帶

    [Tooltip("以哪個軸當北極。Bloch 物理常以 +Z 為北；若覺得 Y 才是『上』，可改 Y。")]
    public NorthAxis northAxis = NorthAxis.Z;

    [Header("UI")]
    public TextMeshProUGUI overlayText;      // 螢幕上的數值顯示（θ, φ）
    public TextMeshPro worldText;            // 世界空間文字(可選)
    public bool worldTextFollow = true;

    private SphereCollider sphereCol;
    private LineRenderer line;

    public enum NorthAxis { Y, Z }

    void Awake()
    {
        sphereCol = GetComponent<SphereCollider>();
        line = GetComponent<LineRenderer>();

        // LineRenderer 基本外觀
        line.positionCount = 2;
        line.enabled = false;
        line.useWorldSpace = true;
        line.startWidth = 0.005f;
        line.endWidth = 0.005f;
    }

    void Update()
    {
        // 取得左右手的食指指尖位置（假設使用手追蹤，手勢互動）
        Vector3 leftTipPos = Vector3.zero;
        bool leftValid = false;
        if (leftHandSkeleton != null && leftHandSkeleton.Bones.Count > 0 && leftHandSkeleton.IsDataValid)
        {
            leftTipPos = leftHandSkeleton.Bones[(int)OVRSkeleton.BoneId.Hand_IndexTip].Transform.position;
            leftValid = true;
        }

        Vector3 rightTipPos = Vector3.zero;
        bool rightValid = false;
        if (rightHandSkeleton != null && rightHandSkeleton.Bones.Count > 0 && rightHandSkeleton.IsDataValid)
        {
            rightTipPos = rightHandSkeleton.Bones[(int)OVRSkeleton.BoneId.Hand_IndexTip].Transform.position;
            rightValid = true;
        }

        // 取得最接近球面的手指指尖
        Vector3 contactL = Vector3.zero;
        Vector3 contactR = Vector3.zero;
        float distErrL = float.MaxValue;
        float distErrR = float.MaxValue;

        bool hasHit = leftValid && TryGetContact(leftTipPos, out contactL, out distErrL);
        bool hasHitR = rightValid && TryGetContact(rightTipPos, out contactR, out distErrR);

        Vector3 contactPoint;
        if (hasHit && hasHitR)
        {
            // 兩手都接近時，取更貼近球面的那個
            if (Mathf.Abs(distErrL) < Mathf.Abs(distErrR)) { contactPoint = contactL; }
            else { contactPoint = contactR; }
        }
        else if (hasHit) { contactPoint = contactL; }
        else if (hasHitR) { contactPoint = contactR; }
        else
        {
            // 沒接觸：隱藏線與世界文字
            line.enabled = false;
            if (worldText) worldText.gameObject.SetActive(false);
            if (overlayText) overlayText.text = "θ: —   φ: —";
            return;
        }

        // 畫從球心到接觸點的線
        Vector3 centerWorld = transform.TransformPoint(sphereCol.center);
        line.SetPosition(0, centerWorld);
        line.SetPosition(1, contactPoint);
        line.enabled = true;

        // 計算 θ、φ（在球的「本地空間」下計算方向，避免球體旋轉造成混亂）
        Vector3 contactLocal = transform.InverseTransformPoint(contactPoint);
        Vector3 centerLocal = sphereCol.center;
        Vector3 dirLocal = (contactLocal - centerLocal).normalized; // 指向接觸點的單位向量

        float thetaDeg, phiDeg;
        ComputeAngles(dirLocal, out thetaDeg, out phiDeg);

        // 顯示 UI（物理慣用：Z 為上，顯示物理 XYZ 對應 Unity YZX）
        if (overlayText)
            overlayText.text = $"θ: {thetaDeg:0.0}°   φ: {phiDeg:0.0}°\n" +
                $"X: {dirLocal.z:+0.000;-0.000;0.000}\n" +
                $"Y: {dirLocal.x:+0.000;-0.000;0.000}\n" +
                $"Z: {dirLocal.y:+0.000;-0.000;0.000}";

        if (worldText)
        {
            worldText.text = $"θ {thetaDeg:0.0}°\nφ {phiDeg:0.0}°";
            worldText.gameObject.SetActive(true);
            if (worldTextFollow)
            {
                // 世界文字貼在接觸點外一點
                Vector3 nudge = (contactPoint - centerWorld).normalized * 0.02f;
                worldText.transform.position = contactPoint + nudge;
                // 讓文字面向主相機
                if (Camera.main)
                    worldText.transform.rotation = Quaternion.LookRotation(worldText.transform.position - Camera.main.transform.position);
            }
        }
    }

    /// <summary>
    /// 嘗試取得『手指指尖最近的球面接觸點』。當距離球面在容許誤差內即視為接觸。
    /// 回傳 distErr：正值=在球外、負值=在球內，數字大小=離球面的距離。
    /// </summary>
    private bool TryGetContact(Vector3 tipPos, out Vector3 contactWorld, out float distErr)
    {
        contactWorld = Vector3.zero;
        distErr = float.MaxValue;

        // 本地空間計算半徑與向量（避免非等比縮放誤差；建議球體維持等比 scale）
        Vector3 centerLocal = sphereCol.center;
        float radiusLocal = sphereCol.radius;

        Vector3 tipLocal = transform.InverseTransformPoint(tipPos);
        Vector3 vLocal = tipLocal - centerLocal;
        float distToCenterLocal = vLocal.magnitude;

        if (distToCenterLocal < 1e-6f) return false; // 太靠近中心，無法判定方向

        // 最近球面點（本地）
        Vector3 contactLocal = centerLocal + vLocal.normalized * radiusLocal;
        contactWorld = transform.TransformPoint(contactLocal);

        // 『距離球面』的誤差（用本地距離即可）
        distErr = distToCenterLocal - radiusLocal; // 修改為帶正負號的誤差

        // 判定是否「接觸」（在容許誤差內）
        return Mathf.Abs(distErr) <= touchSlack;
    }

    /// <summary>
    /// 由本地方向向量算 θ、φ。
    /// northAxis=Y ：θ 從 +Y 向下量，φ 從 +X 朝 +Z（atan2(z,x)）。
    /// northAxis=Z ：θ 從 +Z 向下量（Bloch 常用），φ 從 +X 朝 +Y（atan2(y,x)）。
    /// φ 回傳為 [0, 360) 度。
    /// 在極點（θ=0 或 180）時，φ 沒有物理意義；這裡仍會給 0。
    /// </summary>
    private void ComputeAngles(Vector3 dirLocal, out float thetaDeg, out float phiDeg)
    {
        dirLocal.Normalize();
        float eps = 1e-7f;

        if (northAxis == NorthAxis.Y)
        {
            // θ = arccos(y)
            thetaDeg = Mathf.Acos(Mathf.Clamp(dirLocal.y, -1f, 1f)) * Mathf.Rad2Deg;

            // φ = atan2(z, x)
            float phi = Mathf.Atan2(dirLocal.z, dirLocal.x) * Mathf.Rad2Deg;
            phiDeg = (phi % 360f + 360f) % 360f;

            if (Mathf.Abs(dirLocal.x) < eps && Mathf.Abs(dirLocal.z) < eps)
                phiDeg = 0f;
        }
        else // NorthAxis.Z（Bloch 慣用：+Z 為北）
        {
            // θ = arccos(z)
            thetaDeg = Mathf.Acos(Mathf.Clamp(dirLocal.y, -1f, 1f)) * Mathf.Rad2Deg;

            // φ = atan2(y, x)
            float phi = Mathf.Atan2(dirLocal.x, dirLocal.z) * Mathf.Rad2Deg;
            phiDeg = (phi % 360f + 360f) % 360f;

            if (Mathf.Abs(dirLocal.z) < eps && Mathf.Abs(dirLocal.x) < eps)
                phiDeg = 0f;
        }
    }
}