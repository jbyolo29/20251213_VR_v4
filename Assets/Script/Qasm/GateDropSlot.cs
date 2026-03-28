using UnityEngine;
using UnityEngine.EventSystems;

public class GateDropSlot : MonoBehaviour, IDropHandler
{
    [Header("Coordinate")]
    public int row;
    public int col;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            GameObject droppedObj = eventData.pointerDrag;
            GateDragItem gateItem = droppedObj.GetComponent<GateDragItem>();

            if (gateItem != null)
            {
                var controller = FindObjectOfType<CircuitGridController>();

                // 1. ★ 覆蓋邏輯：如果格子裡已經有別的閘，先把它殺掉
                if (transform.childCount > 0)
                {
                    foreach (Transform child in transform)
                    {
                        // 避免刪到自己 (雖然理論上還沒變 parent，但保險起見)
                        if (child.gameObject != droppedObj)
                            Destroy(child.gameObject);
                    }
                }

                // 2. ★ 移動邏輯：如果是從別的格子拖過來的，要先刪除舊位置的數據！
                // (避免 A 格拖到 B 格後，A 格的數據還在)
                if (gateItem.isFromCircuitBoard)
                {
                    if (controller != null)
                    {
                        // 刪除舊位置數據 (注意：這不會觸發重算，因為馬上就要寫入新數據了)
                        // 為了效能，我們可以暫時不重算，等 PlaceGate 再算
                        // 但為了保險，這裡呼叫 DeleteGate 是安全的
                        controller.DeleteGate(gateItem.originalRow, gateItem.originalCol);
                    }
                }

                // 3. 視覺處理：吸附進來
                droppedObj.transform.SetParent(transform);
                droppedObj.transform.localPosition = Vector3.zero;
                droppedObj.transform.localScale = Vector3.one;

                // 4. 更新閘的身份
                gateItem.isFromCircuitBoard = true;
                gateItem.originalRow = row;
                gateItem.originalCol = col;

                // ★ 關鍵改動：不要 Destroy(gateItem)！
                // 讓它保持活著，這樣你下次才能把它拖走。

                // 5. 寫入新數據 & 運算
                if (controller != null)
                {
                    controller.PlaceGate(row, col, gateItem.op);
                }
            }
        }
    }
}