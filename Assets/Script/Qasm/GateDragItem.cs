using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(CanvasGroup), typeof(Collider), typeof(Rigidbody))]
public class GateDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("量子屬性")]
    public string op = "h";
    public bool isToolboxItem = false;

    [Header("視覺狀態")]
    public GameObject diskVisual;
    public bool isInteractable = false;

    [Header("吸附設定")]
    public float snapRange = 0.15f;

    [HideInInspector] public bool isFromCircuitBoard = false;
    [HideInInspector] public int originalRow = -1;
    [HideInInspector] public int originalCol = -1;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Rigidbody rb;
    private Canvas itemCanvas;
    private bool isGrabbed = false;
    private Transform activeGrabbingPoint = null;
    private Vector3 grabOffset;

    public static GateDragItem currentlyGrabbedItem = null;
    private OVRHand[] vrHands;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        itemCanvas = gameObject.GetComponent<Canvas>();
        if (itemCanvas == null) itemCanvas = gameObject.AddComponent<Canvas>();
        itemCanvas.overrideSorting = true;
        itemCanvas.sortingOrder = 10;

        if (GetComponent<Collider>() != null) GetComponent<Collider>().isTrigger = true;
    }

    void Start() => vrHands = FindObjectsOfType<OVRHand>(true);

    public void SetGateVisibility(bool visible)
    {
        isInteractable = visible;
        canvasGroup.alpha = visible ? 1f : 0.8f;
        canvasGroup.blocksRaycasts = visible;
        if (diskVisual != null) diskVisual.SetActive(!visible);
    }

    void Update()
    {
        CheckPinchAction();
        if (isGrabbed && activeGrabbingPoint != null)
        {
            transform.position = activeGrabbingPoint.position + grabOffset;
            Canvas root = GetComponentInParent<Canvas>()?.rootCanvas;
            if (root != null) transform.rotation = root.transform.rotation;
        }
    }

    void CheckPinchAction()
    {
        if (!isToolboxItem && !isInteractable && !isGrabbed) return;
        if (currentlyGrabbedItem != null && currentlyGrabbedItem != this) return;

        bool isPinching = false;
        Transform potentialGrabPoint = null;
        GameObject[] fingerTips = GameObject.FindGameObjectsWithTag("PlayerHand");

        foreach (var tip in fingerTips)
        {
            if (Vector3.Distance(tip.transform.position, transform.position) < 0.08f)
            {
                foreach (var hand in vrHands)
                {
                    if (hand != null && hand.gameObject.activeInHierarchy && hand.GetFingerIsPinching(OVRHand.HandFinger.Index))
                    {
                        isPinching = true;
                        potentialGrabPoint = tip.transform;
                        break;
                    }
                }
            }
            if (isPinching) break;
        }

        if (isPinching && !isGrabbed && currentlyGrabbedItem == null)
        {
            isGrabbed = true;
            currentlyGrabbedItem = this;
            activeGrabbingPoint = potentialGrabPoint;
            grabOffset = transform.position - activeGrabbingPoint.position;
            if (isToolboxItem) DuplicateItem();
            canvasGroup.blocksRaycasts = false;
            itemCanvas.sortingOrder = 20;
            Canvas root = GetComponentInParent<Canvas>().rootCanvas;
            if (root != null) transform.SetParent(root.transform, true);
        }
        else if (!isPinching && isGrabbed)
        {
            isGrabbed = false;
            currentlyGrabbedItem = null;
            canvasGroup.blocksRaycasts = true;
            itemCanvas.sortingOrder = 10;
            FinalizeDrop();
        }
    }

    void DuplicateItem()
    {
        int index = transform.GetSiblingIndex();
        GameObject clone = Instantiate(gameObject, transform.parent);
        clone.name = gameObject.name;
        clone.transform.SetSiblingIndex(index);
        clone.GetComponent<GateDragItem>().isToolboxItem = true;
        this.isToolboxItem = false;
    }

    void FinalizeDrop()
    {
        GateDropSlot targetSlot = null;
        GateDropSlot[] allSlots = FindObjectsOfType<GateDropSlot>();

        // ★ 核心修復：不管格子裡有沒有 UI 背景，只檢查裡面有沒有「其他邏輯閘」
        var best = allSlots.Select(s => new { s, d = Vector3.Distance(transform.position, s.transform.position) })
                           .Where(x => x.d < snapRange)
                           .Where(x => x.s.GetComponentsInChildren<GateDragItem>().All(g => g == this)) // 確保沒有別人佔用
                           .OrderBy(x => x.d).FirstOrDefault();

        if (best != null) targetSlot = best.s;

        if (targetSlot == null)
        {
            if (isFromCircuitBoard) FindObjectOfType<CircuitGridController>()?.DeleteGate(originalRow, originalCol);
            Destroy(gameObject);
        }
        else
        {
            transform.SetParent(targetSlot.transform, true);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            isFromCircuitBoard = true;
            originalRow = targetSlot.row;
            originalCol = targetSlot.col;
            FindObjectOfType<CircuitGridController>()?.PlaceGate(originalRow, originalCol, op);
        }
    }

    public void OnBeginDrag(PointerEventData eventData) { }
    public void OnDrag(PointerEventData eventData) { }
    public void OnEndDrag(PointerEventData eventData) { if (!isGrabbed) FinalizeDrop(); }
}