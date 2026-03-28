using UnityEngine;

public class MouseRotator : MonoBehaviour
{
    [Header("滑鼠旋轉靈敏度")]
    public float rotationSpeed = 250.0f;

    [Header("雙擊判定時間")]
    public float doubleClickTime = 0.3f; // 0.3秒內點兩下算雙擊

    private bool isDragging = false;
    private Vector3 previousMousePosition;

    // 記錄初始狀態與點擊時間
    private Quaternion initialRotation;
    private float lastClickTime = 0f;

    void Start()
    {
        // 遊戲開始時，先記住球的預設角度
        initialRotation = transform.rotation;
    }

    void Update()
    {
        // 尋找攝影機 (防呆機制)
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();

        // 按下右鍵 (1) 時，先發射射線檢查有沒有點到「這顆球」
        if (Input.GetMouseButtonDown(1) && cam != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            // 檢查射線打到的 3D 物件
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // 如果打到的物件是自己，或是自己的子物件 (例如裡面的 Sphere)
                if (hit.transform.IsChildOf(this.transform) || hit.transform == this.transform)
                {
                    // 確定點到專屬的球了，才開始判定雙擊或拖曳
                    float timeSinceLastClick = Time.time - lastClickTime;

                    if (timeSinceLastClick <= doubleClickTime)
                    {
                        // 雙擊觸發！將旋轉角度歸位
                        transform.rotation = initialRotation;
                        isDragging = false;
                        Debug.Log($"[{gameObject.name}] 雙擊右鍵，座標已歸位！");
                    }
                    else
                    {
                        // 單擊：開始專屬拖曳
                        isDragging = true;
                        previousMousePosition = Input.mousePosition;
                    }

                    lastClickTime = Time.time;
                }
            }
        }

        // 放開右鍵，停止拖曳
        if (Input.GetMouseButtonUp(1))
        {
            isDragging = false;
        }

        // 拖曳旋轉 (現在只有被點到的那顆球 isDragging 會是 true)
        if (isDragging)
        {
            Vector3 deltaMouse = Input.mousePosition - previousMousePosition;

            float rotY = -deltaMouse.x * rotationSpeed * Time.deltaTime;
            float rotX = deltaMouse.y * rotationSpeed * Time.deltaTime;

            transform.Rotate(rotX, rotY, 0, Space.World);

            previousMousePosition = Input.mousePosition;
        }
    }
}