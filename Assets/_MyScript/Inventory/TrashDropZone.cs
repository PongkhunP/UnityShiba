using System.Collections;
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

    [Header("Animation Settings")]
    [Tooltip("ระยะเวลาที่ใช้ในการกลืนขยะ (วินาที)")]
    public float swallowDuration = 0.4f;

    void Start()
    {
        if (trashImage == null) trashImage = GetComponent<Image>();
        if (trashImage != null) trashImage.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var drag = InventoryDragHandler.Instance;
        if (drag != null && drag.IsDragging && trashImage != null)
        {
            trashImage.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (trashImage != null) trashImage.color = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        var drag = InventoryDragHandler.Instance;
        if (drag == null || !drag.IsDragging) return;

        Sprite itemSprite = drag.dragItem != null ? drag.dragItem.icon : null;

        // เคลียร์ของออก
        if (drag.draggedFromSlot != null) drag.draggedFromSlot.Clear();
        if (drag.draggedFromHotbar != null) drag.draggedFromHotbar.Clear();

        // ปิดการลาก (ซ่อนไอคอนที่ติดเมาส์)
        drag.EndDrag();
        if (trashImage != null) trashImage.color = normalColor;

        // เรียกใช้งานแอนิเมชัน
        if (itemSprite != null)
        {
            StartCoroutine(ShrinkAndSwallowAnimation(itemSprite, eventData.position));
        }
    }

    // =========================================================
    // แอนิเมชัน: ค่อยๆ หดเล็กลง + ดูดเข้าถัง (รองรับตอน Pause เกม)
    // =========================================================
    IEnumerator ShrinkAndSwallowAnimation(Sprite sprite, Vector2 screenPos)
    {
        GameObject tempGO = new GameObject("_SwallowIcon");
        tempGO.transform.SetParent(transform, false);

        RectTransform rect = tempGO.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(60, 60);
        rect.position = screenPos;

        Image img = tempGO.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;

        Vector3 startPos = rect.position;
        Vector3 targetPos = transform.position;

        float elapsed = 0f;

        while (elapsed < swallowDuration)
        {
            // === แก้ไขตรงนี้: ใช้ unscaledDeltaTime เพื่อให้แอนิเมชันทำงานตอน Pause เกมได้ ===
            elapsed += Time.unscaledDeltaTime;

            float t = elapsed / swallowDuration;
            float ease = t * t;

            // เลื่อนตำแหน่ง + หดขนาด + หมุน + โปร่งใส
            rect.position = Vector3.Lerp(startPos, targetPos, t);
            rect.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, ease);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -180f, ease));
            img.color = new Color(1f, 1f, 1f, 1f - ease);

            yield return null;
        }

        // เมื่อแอนิเมชันจบ ให้ทำลายทิ้ง (คราวนี้ทำลายได้แน่นอน!)
        Destroy(tempGO);
    }
}