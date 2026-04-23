using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class InventoryItems : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;
    public ItemSO item;
    public int amount;
    public InventorySlotUIs sourceSlot;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector2 originalPosition;
    public bool wasDroppedSuccessfully;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        amountText.text = amount.ToString();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Calculate amount (Terraria Style: Right Click = 1)
        int amountToMove = (eventData.button == PointerEventData.InputButton.Right) ? amount / 2 : amount;
        wasDroppedSuccessfully = false;

        // Visual feedback: If we split the stack, the source slot stays visible with less
        // If we move all, the source slot looks empty.
        sourceSlot.OnItemDraggedAway(amountToMove);

        // UI Layering: Move to a 'Drag Layer' so it's above all other UI
        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;
        transform.SetParent(transform.root); // Move to topmost canvas

        canvasGroup.blocksRaycasts = false; // Essential for IDropHandler to work
        canvasGroup.alpha = 0.7f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Simple position follow
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // If we weren't dropped on a valid IDropHandler slot, return home
        if (!wasDroppedSuccessfully)
        {
            ReturnToSource();
        }
    }

    public void ReturnToSource()
    {
        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = Vector2.zero;
    }
}