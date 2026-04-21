using UnityEngine;
using UnityEngine.EventSystems;

public class GateDropSlot : MonoBehaviour, IDropHandler
{
    [Header("Coordinate")]
    public int row;
    public int col;

    // 保留原本的 UI 事件接口 (接收我們剛剛在 GateDragItem 裡寫的偽裝訊號)
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData != null && eventData.pointerDrag != null)
        {
            GateDragItem gateItem = eventData.pointerDrag.GetComponent<GateDragItem>();
            ProcessGateDrop(gateItem);
        }
    }

    // 將核心處理邏輯獨立出來
    public void ProcessGateDrop(GateDragItem gateItem)
    {
        if (gateItem == null) return;

        GameObject droppedObj = gateItem.gameObject;
        var controller = FindObjectOfType<CircuitGridController>();

        // 1. 覆蓋邏輯：如果格子裡已經有別的閘，先把它殺掉
        if (transform.childCount > 0)
        {
            foreach (Transform child in transform)
            {
                if (child.gameObject != droppedObj)
                    Destroy(child.gameObject);
            }
        }

        // 2. 移動邏輯：從別格拖來，刪除舊數據
        if (gateItem.isFromCircuitBoard)
        {
            if (controller != null)
            {
                controller.DeleteGate(gateItem.originalRow, gateItem.originalCol);
            }
        }

        // 3. 視覺處理：吸附進來
        droppedObj.transform.SetParent(transform);
        droppedObj.transform.localPosition = Vector3.zero;
        // ★ 關鍵 VR 修正：強制旋轉歸零，確保邏輯閘完美貼平電路板，不會因為手部旋轉而歪斜
        droppedObj.transform.localRotation = Quaternion.identity;
        droppedObj.transform.localScale = Vector3.one;

        // 4. 更新閘的身份
        gateItem.isFromCircuitBoard = true;
        gateItem.originalRow = row;
        gateItem.originalCol = col;

        // 5. 寫入新數據 & 運算
        if (controller != null)
        {
            controller.PlaceGate(row, col, gateItem.op);
        }
    }
}