using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public class GateDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Info")]
    public string op = "h";
    public bool isToolboxItem = false;

    [HideInInspector] public bool isFromCircuitBoard = false;
    [HideInInspector] public int originalRow = -1;
    [HideInInspector] public int originalCol = -1;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isToolboxItem)
        {
            GameObject clone = Instantiate(gameObject, transform.parent);
            clone.transform.SetSiblingIndex(transform.GetSiblingIndex());
            clone.name = gameObject.name;

            var cloneScript = clone.GetComponent<GateDragItem>();
            cloneScript.isToolboxItem = true;
            cloneScript.isFromCircuitBoard = false;

            this.isToolboxItem = false;
            this.isFromCircuitBoard = false;
        }

        if (rootCanvas != null) transform.SetParent(rootCanvas.transform, true);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rootCanvas == null) return;

        // 1. 首要路徑：若雷射/滑鼠確實打在實體 UI (如電路板或隱形牆) 上，直接取其 3D 座標
        if (eventData.pointerCurrentRaycast.isValid && eventData.pointerCurrentRaycast.worldPosition != Vector3.zero)
        {
            transform.position = eventData.pointerCurrentRaycast.worldPosition;
            return;
        }

        // 2. 備用路徑：若指標脫離了實體範圍，建立一個數學平面來承接它
        Camera cam = eventData.pressEventCamera;
        if (cam == null) cam = rootCanvas.worldCamera;
        if (cam == null) cam = Camera.main;

        if (cam != null)
        {
            Plane plane = new Plane(rootCanvas.transform.forward, rootCanvas.transform.position);
            Ray ray = cam.ScreenPointToRay(eventData.position);

            if (plane.Raycast(ray, out float distance))
            {
                transform.position = ray.GetPoint(distance);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        GameObject hitObject = eventData.pointerCurrentRaycast.gameObject;
        GateDropSlot dropSlot = null;

        if (hitObject != null)
        {
            dropSlot = hitObject.GetComponent<GateDropSlot>();
            if (dropSlot == null) dropSlot = hitObject.GetComponentInParent<GateDropSlot>();
        }

        if (dropSlot == null)
        {
            if (isFromCircuitBoard)
            {
                var controller = FindObjectOfType<CircuitGridController>();
                if (controller != null) controller.DeleteGate(originalRow, originalCol);
            }
            Destroy(gameObject);
        }
    }
}