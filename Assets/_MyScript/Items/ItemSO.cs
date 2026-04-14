using UnityEngine;

public enum ItemCategory { Tool, Seed, Consumable, CraftingMaterial, FarmHelper }
// [���] ���� Axe ����㹹��
public enum ToolAction { None, Hoe, Water, Axe }

[CreateAssetMenu(menuName = "Items/Item")]
public class ItemSO : ScriptableObject
{
    // ... (����������� ����ͧź) ...
    // ���� Enum ��ҧ����ͤ�Ѻ

    [Header("Info")]
    public string itemName;
    public Sprite icon;

    [Header("3D Visuals")]
    public GameObject equipmentPrefab;

    [Header("Effects (VFX & SFX)")]
    public GameObject actionVFX;
    public AudioClip actionSFX;
    public float sfxDuration = 0f;
    [Range(0.8f, 1.2f)] public float pitchRandomMultiplier = 1f;

    [Header("Energy")]
    public float energyCost = 0f;

    [Header("Stack")]
    public bool isStackable = true;
    [Min(1)] public int maxStack = 99;

    [Header("Gameplay")]
    public ItemCategory category = ItemCategory.Tool;
    public ToolAction toolAction = ToolAction.None;
    public CropSO seedCrop;

    [Header("Crafting")]
    [Tooltip("ถ้าเป็น FarmHelper → อ้างถึง FarmHelperSO")]
    public FarmHelperSO farmHelperData;

    [Header("Sell")]
    public bool sellable = true;
    public int sellPrice = 10;
}