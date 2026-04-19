using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TrashDropZone : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Trash Icon")]
    public Image trashImage;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 0.25f, 0.25f, 1f);

    void Start()
    {
        if (trashImage == null) trashImage = GetComponent<Image>();
        if (trashImage != null) trashImage.color = normalColor;
    }

    // เมื่อเอาเมาส์ชี้ถังขยะ (พร้อมถือของ) -> เปลี่ยนสีถังขยะ
    public void OnPointerEnter(PointerEventData eventData)
    {
        var drag = InventoryDragHandler.Instance;
        if (drag != null && drag.IsDragging && trashImage != null)
        {
            trashImage.color = hoverColor;
        }
    }

    // เมื่อเอาเมาส์ออก -> คืนสีเดิม
    public void OnPointerExit(PointerEventData eventData)
    {
        if (trashImage != null) trashImage.color = normalColor;
    }

    // เมื่อคลิกที่ถังขยะ
    public void OnPointerClick(PointerEventData eventData)
    {
        var drag = InventoryDragHandler.Instance;

        // ถ้าไม่ได้ถือของอะไรอยู่ ให้ข้ามไปเลย
        if (drag == null || !drag.IsDragging) return;

        // 1. เคลียร์ของออกจากกระเป๋าต้นทาง
        if (drag.draggedFromSlot != null) drag.draggedFromSlot.Clear();
        if (drag.draggedFromHotbar != null) drag.draggedFromHotbar.Clear();

        // 2. ปิดระบบ Drag ทันที! (รูปไอเทมที่ติดเมาส์จะหายไป 100%)
        drag.EndDrag();

        // 3. คืนสีถังขยะเป็นสีปกติ
        if (trashImage != null) trashImage.color = normalColor;

        // จบการทำงาน! ไอเทมหายไปทันทีแบบไม่มีดีเลย์
    }
}