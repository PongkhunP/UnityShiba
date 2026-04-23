using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlotUIs : MonoBehaviour, IDropHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private InventoryItems itemIconPrefab; 

    [Header("Identification")]
    public int slotIndex;
    public int inventoryID; 

    [Header("Runtime Data")]
    public ItemSO currentItem;
    public int amount;
    private InventoryItems currentItemUI;

    private void Awake()
    {
        // Ensure we have a visual item component if one isn't there
        currentItemUI = GetComponentInChildren<InventoryItems>();
        if (currentItemUI != null)
        {
            currentItemUI.sourceSlot = this;
            amount = currentItemUI.amount;
        }
        amountText.text = amount.ToString();
    }

    /// <summary>
    /// Called by the Network System (e.g., OnChanged) to refresh the UI
    /// </summary>
    public void RefreshSlot(ItemSO newItem, int newAmount)
    {
        currentItem = newItem;
        amount = newAmount;

        if (newItem == null || newAmount <= 0)
        {
            ClearSlotVisuals();
            return;
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (currentItem == null) return;

        iconImage.sprite = currentItem.icon;
        iconImage.enabled = true;

        // Terraria-style: Tools show infinity or nothing, items show count
        if (amountText)
        {
            amountText.text = amount.ToString();
        }

        // Ensure the draggable component knows its current state
        if (currentItemUI == null)
        {
            currentItemUI = Instantiate(itemIconPrefab, transform);
            currentItemUI.sourceSlot = this;
        }

        currentItemUI.item = currentItem;
        currentItemUI.amount = amount;
    }

    /// <summary>
    /// Visual Deduction: Called by InventoryItems when the drag starts
    /// </summary>
    public void OnItemDraggedAway(int amountTaken)
    {
        amount -= amountTaken;

        if (amount <= 0)
        {
            iconImage.enabled = false;
            if (amountText) amountText.text = "";
        }
        else
        {
            if (amountText) amountText.text = amount.ToString();
        }
    }

    /// <summary>
    /// Called when an InventoryItems is dropped ONTO this slot
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject == null) return;

        InventoryItems draggedItem = droppedObject.GetComponent<InventoryItems>();

        if (draggedItem != null)
        {
            draggedItem.wasDroppedSuccessfully = true;
            draggedItem.transform.SetParent(transform);

            // Snap it to the dead center of the slot
            draggedItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            int fromInv = draggedItem.sourceSlot.inventoryID;
            int fromSlot = draggedItem.sourceSlot.slotIndex;
            int toInv = this.inventoryID;
            int toSlot = this.slotIndex;
            int moveAmount = draggedItem.amount;

            Debug.Log($"[Client] Requesting move: {moveAmount} of {draggedItem.item.itemName} " +
                      $"from Inv {fromInv}:Slot {fromSlot} to Inv {toInv}:Slot {toSlot}");

            // InventoryNetworkManager.Instance.SendMoveRequest(fromInv, fromSlot, toInv, toSlot, moveAmount);
            // --- MULTIPLAYER LOGIC END ---

            // Note: We don't snap the item into the slot here. 
            // We let OnEndDrag ReturnToSource, and wait for the Server to update the NetworkList.
        }
    }

    private void ClearSlotVisuals()
    {
        currentItem = null;
        amount = 0;
        iconImage.enabled = false;
        if (amountText) amountText.text = "";
    }
}