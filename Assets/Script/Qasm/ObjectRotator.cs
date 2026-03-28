using UnityEngine;

public class ObjectRotator : MonoBehaviour
{
    [Header("旋轉速度")]
    public float rotationSpeed = 100.0f;

    void Update()
    {
        float rotX = 0;
        float rotY = 0;

        // --- 1. VR 搖桿輸入 (右手) ---
        // 抓取右手搖桿的推動數值
        Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        // --- 2. 鍵盤輸入 (備用，方便在電腦測試) ---
        // 也可以用鍵盤上下左右鍵來轉
        if (Input.GetKey(KeyCode.LeftArrow)) thumbstick.x = -1;
        if (Input.GetKey(KeyCode.RightArrow)) thumbstick.x = 1;
        if (Input.GetKey(KeyCode.UpArrow)) thumbstick.y = 1;
        if (Input.GetKey(KeyCode.DownArrow)) thumbstick.y = -1;

        // 如果有輸入 (數值不為 0)
        if (thumbstick.magnitude > 0.1f)
        {
            // 水平推 -> 沿著 Y 軸轉 (左右轉)
            rotY = -thumbstick.x * rotationSpeed * Time.deltaTime;

            // 垂直推 -> 沿著 X 軸轉 (上下翻)
            rotX = thumbstick.y * rotationSpeed * Time.deltaTime;

            // 執行旋轉
            transform.Rotate(rotX, rotY, 0, Space.World);
        }
    }
}