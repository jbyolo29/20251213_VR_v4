using UnityEngine;

public class LaserPointerController : MonoBehaviour
{
    [Header("必要組件")]
    public LineRenderer lineRenderer;
    public Transform laserDot;

    [Header("射線設定")]
    public float maxDistance = 10.0f; // ★ 調長到10，確保能碰到遠處的面板
    public LayerMask interactableLayer;

    [Header("顏色設定 (按板機變色)")]
    public Color normalColor = Color.red;     // 沒按下的顏色 (紅色)
    public Color pressedColor = Color.green;  // 按下板機的顏色 (綠色)

    void Update()
    {
        if (lineRenderer == null) return;

        // ★ 核心魔法：偵測 Quest 右手食指板機 (PrimaryIndexTrigger) 是否被按下
        // 使用 RTouch 指定為右手控制器
        bool isPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

        // 根據按壓狀態切換顏色
        Color currentColor = isPressed ? pressedColor : normalColor;
        lineRenderer.startColor = currentColor;
        lineRenderer.endColor = currentColor;

        // 如果光點有材質，也一起變色
        if (laserDot != null)
        {
            var dotRenderer = laserDot.GetComponent<Renderer>();
            if (dotRenderer != null) dotRenderer.material.color = currentColor;
        }

        // --- 以下為雷射長度與落點計算 ---
        lineRenderer.SetPosition(0, transform.position);

        RaycastHit hit;
        // 發射物理射線
        if (Physics.Raycast(transform.position, transform.forward, out hit, maxDistance, interactableLayer))
        {
            // 射到東西：線停在物體上
            lineRenderer.SetPosition(1, hit.point);

            if (laserDot != null)
            {
                laserDot.position = hit.point + hit.normal * 0.01f;
                laserDot.rotation = Quaternion.LookRotation(hit.normal);
                laserDot.gameObject.SetActive(true);
            }
        }
        else
        {
            // 沒射到東西：射向遠方
            lineRenderer.SetPosition(1, transform.position + transform.forward * maxDistance);

            if (laserDot != null)
            {
                laserDot.gameObject.SetActive(false);
            }
        }
    }
}