using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ... (Header เดิม) ...
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;

    [Header("References")]
    public Transform cameraTransform;
    public FarmingSystem farmingSystem;

    [Header("Input")]
    public KeyCode runKey = KeyCode.LeftShift;
    public int primaryMouseButton = 0;
    public int harvestMouseButton = 1;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isRunning;
    private bool isBusyAction;
    private bool isSitting;
    private Transform currentSitPoint;

    // Cache
    private ItemSO _cachedItem;
    private SoilTile _cachedTile;

    // [อัปเดตชื่อคลาสตรงนี้]
    private ChoppableCut_Tree _cachedTree;

    private bool _hasCachedSoilAction;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (HotbarUI.Instance != null) HotbarUI.Instance.IsInputLocked = isBusyAction;
        if (InventoryUI.IsOpen) return;

        if (isSitting) { HandleSitInput(); return; }

        UpdateHoldStateFromHotbar();

        if (isBusyAction) return;

        HandleMovement();
        HandleActionInput();
        // เพิ่มบรรทัดนี้ไว้บนสุด
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            // สั่งให้หยุดเดินอนิเมชั่น (ถ้ามี)
            // animator.SetFloat("Speed", 0); 
            return;
        }
    }

    // ... (HandleMovement เหมือนเดิม) ...
    private void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0f) velocity.y = -2f;
        float h = Input.GetAxisRaw("Horizontal"); float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0f, v).normalized;
        isRunning = Input.GetKey(runKey);
        float targetSpeed = isRunning ? runSpeed : walkSpeed;
        Vector3 moveDir = Vector3.zero;
        if (inputDir.magnitude >= 0.1f)
        {
            Vector3 camForward = cameraTransform.forward; Vector3 camRight = cameraTransform.right; camForward.y = 0f; camRight.y = 0f; camForward.Normalize(); camRight.Normalize();
            moveDir = camForward * inputDir.z + camRight * inputDir.x;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), rotationSpeed * Time.deltaTime);
        }
        controller.Move(moveDir * targetSpeed * Time.deltaTime);
        velocity.y += gravity * Time.deltaTime; controller.Move(velocity * Time.deltaTime);
        animator.SetFloat("Speed", moveDir.magnitude * (isRunning ? 1f : 0.5f)); animator.SetBool("IsRunning", isRunning);
    }

    private void HandleActionInput()
    {
        if (!farmingSystem || !HotbarUI.Instance) return;

        if (Input.GetMouseButtonDown(primaryMouseButton))
        {
            var item = HotbarUI.Instance.GetSelectedItem();
            if (!item) return;

            // 1. ถ้าเป็นขวาน -> หาต้นไม้
            if (item.category == ItemCategory.Tool && item.toolAction == ToolAction.Axe)
            {
                if (farmingSystem.TryGetTargetTree(out var tree))
                {
                    _cachedItem = item;
                    _cachedTree = tree;
                    _cachedTile = null;
                    _hasCachedSoilAction = true;

                    FaceTo(tree.transform.position);
                    StartActionTrigger("Chop");
                }
                return;
            }

            // 2. ถ้าเป็นอย่างอื่น -> หาดิน
            if (farmingSystem.TryGetTargetSoil(out var tile))
            {
                _cachedItem = item;
                _cachedTile = tile;
                _cachedTree = null;
                _hasCachedSoilAction = true;

                FaceTo(tile.transform.position);

                if (item.category == ItemCategory.Tool)
                {
                    if (item.toolAction == ToolAction.Hoe) StartActionTrigger("Dig");
                    else if (item.toolAction == ToolAction.Water) StartActionTrigger("Water");
                    else StartActionTrigger("PlaceItem");
                }
                else if (item.category == ItemCategory.Seed) { StartActionTrigger("PlaceItem"); }
            }
        }

        if (Input.GetMouseButtonDown(harvestMouseButton))
        {
            if (farmingSystem.TryGetTargetSoil(out var tile))
            {
                if (tile.crop != null && tile.stageIndex >= tile.crop.growthPrefabs.Length - 1)
                {
                    _cachedTile = tile; _cachedItem = null; _hasCachedSoilAction = true;
                    FaceTo(tile.transform.position); StartActionTrigger("Harvest");
                }
            }
        }
    }

    private void StartActionTrigger(string triggerName) { isBusyAction = true; animator.ResetTrigger(triggerName); animator.SetTrigger(triggerName); }
    private void FaceTo(Vector3 worldPos) { Vector3 dir = worldPos - transform.position; dir.y = 0f; if (dir.sqrMagnitude < 0.001f) return; transform.rotation = Quaternion.LookRotation(dir.normalized); }

    public void OnActionImpact()
    {
        if (!_hasCachedSoilAction) return;
        if (!farmingSystem) return;

        if (_cachedItem != null)
        {
            if (_cachedTree != null)
            {
                farmingSystem.ChopTree(_cachedItem, _cachedTree);
            }
            else if (_cachedTile != null)
            {
                farmingSystem.ApplyItemOnTile(_cachedItem, _cachedTile);
            }
        }
        else
        {
            farmingSystem.TryHarvestExternal(_cachedTile);
        }
    }

    public void OnActionAnimationFinished()
    {
        isBusyAction = false;
        _hasCachedSoilAction = false;
        _cachedItem = null;
        _cachedTile = null;
        _cachedTree = null;
    }

    // ... (ส่วน Hold/Sit/Fish เหมือนเดิม) ...
    private void UpdateHoldStateFromHotbar() { if (!HotbarUI.Instance) { animator.SetBool("HoldItem", false); return; } var item = HotbarUI.Instance.GetSelectedItem(); var slot = HotbarUI.Instance.GetSelectedSlot(); bool shouldHold = item != null && (item.category == ItemCategory.Tool || (slot != null && slot.amount > 0)); animator.SetBool("HoldItem", shouldHold); }
    public void Sit(Transform sitPoint) { if (isSitting) return; currentSitPoint = sitPoint; isSitting = true; isBusyAction = false; controller.enabled = false; transform.position = sitPoint.position; transform.rotation = sitPoint.rotation; animator.SetBool("Sit", true); }
    private void HandleSitInput() { if (Input.GetKeyDown(KeyCode.E)) StandUpFromSit(); }
    public void StandUpFromSit() { if (!isSitting) return; isSitting = false; animator.SetBool("Sit", false); controller.enabled = true; }
    public void StartFishing(Transform fishPoint) { if (isBusyAction) return; transform.position = fishPoint.position; transform.rotation = fishPoint.rotation; isBusyAction = true; animator.SetTrigger("Fish"); }
    public void OnFishingAnimationFinished() { isBusyAction = false; }
}