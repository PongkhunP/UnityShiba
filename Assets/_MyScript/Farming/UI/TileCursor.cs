using UnityEngine;

public class TileCursor : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public Transform player;
    public GameObject cursorVisual;

    [Header("Layer Settings")]
    public LayerMask soilMask;
    public LayerMask treeMask;
    [Tooltip("ใส่ Layer ของพื้นดินปกติ (เช่น Default หรือ Terrain)")]
    public LayerMask groundMask; // [เพิ่มใหม่] เลเยอร์พื้นดินปกติ

    [Header("Cursor Settings")]
    public float interactRange = 4f;
    public Vector3 visualOffset = new Vector3(0, 0.05f, 0);

    [Header("Grid Snapping (แบบที่ 2)")]
    public bool snapToGrid = true;
    [Tooltip("ขนาดของช่องกริด (ปกติแปลงดินน่าจะ 1x1 เมตร)")]
    public float gridSize = 1f;

    [Header("Debug")]
    public bool showDebugRay = true;

    private void Start()
    {
        if (!cam) cam = Camera.main;
        if (!player) { var p = GameObject.FindGameObjectWithTag("Player"); if (p) player = p.transform; }
        if (cursorVisual) cursorVisual.SetActive(false);
    }

    private void Update()
    {
        if (InventoryUI.IsOpen) { if (cursorVisual) cursorVisual.SetActive(false); return; }
        UpdateCursorPosition();
    }

    void UpdateCursorPosition()
    {
        if (!cursorVisual || !player || !cam) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        bool foundTarget = false;
        Vector3 targetPos = Vector3.zero;

        if (showDebugRay) Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red);

        // 1. เช็คเป้าหมายที่เป็น "ต้นไม้" ก่อน
        if (Physics.Raycast(ray, out hit, 100f, treeMask))
        {
            if (Vector3.Distance(FlatPos(player.position), FlatPos(hit.point)) <= interactRange)
            {
                foundTarget = true;
                targetPos = hit.transform.position + visualOffset;
            }
        }

        // 2. ถ้าไม่เจอต้นไม้ ให้เช็คเป้าหมายที่เป็น "แปลงดิน (SoilTile)" ที่สับจอบไว้แล้ว
        if (!foundTarget && Physics.Raycast(ray, out hit, 100f, soilMask))
        {
            SoilTile tile = hit.collider.GetComponentInParent<SoilTile>();
            if (tile != null)
            {
                if (Vector3.Distance(FlatPos(player.position), FlatPos(hit.point)) <= interactRange)
                {
                    foundTarget = true;
                    targetPos = tile.transform.position + visualOffset;
                }
            }
        }

        // 3. [ใหม่ล่าสุด!] ถ้าไม่เจออะไรเลย ให้หา "พื้นดินปกติ" เพื่อโชว์กรอบเตรียมสับจอบ
        if (!foundTarget && Physics.Raycast(ray, out hit, 100f, groundMask))
        {
            if (Vector3.Distance(FlatPos(player.position), FlatPos(hit.point)) <= interactRange)
            {
                foundTarget = true;

                // ล็อคพิกัดให้เป็นช่องตาราง (Grid Snapping)
                if (snapToGrid)
                {
                    float snapX = Mathf.Round(hit.point.x / gridSize) * gridSize;
                    float snapZ = Mathf.Round(hit.point.z / gridSize) * gridSize;
                    // ให้ความสูง (Y) แนบไปกับพื้นผิวที่เลเซอร์ยิงชน
                    targetPos = new Vector3(snapX, hit.point.y, snapZ) + visualOffset;
                }
                else
                {
                    targetPos = hit.point + visualOffset;
                }
            }
        }

        // 4. แสดงผลกรอบสีเขียว
        if (foundTarget)
        {
            cursorVisual.SetActive(true);
            cursorVisual.transform.position = Vector3.Lerp(cursorVisual.transform.position, targetPos, 25f * Time.deltaTime);
        }
        else
        {
            cursorVisual.SetActive(false);
        }
    }

    private Vector3 FlatPos(Vector3 pos)
    {
        return new Vector3(pos.x, 0, pos.z);
    }
}