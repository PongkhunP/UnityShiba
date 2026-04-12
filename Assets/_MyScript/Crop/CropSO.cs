using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Farm/Crop")]
public class CropSO : SerializedScriptableObject
{
    [BoxGroup("Info"), LabelWidth(80)]
    public string cropName;

    [BoxGroup("Info"), PreviewField(64), HideLabel]
    public Sprite icon;

    [BoxGroup("Growth"), TableList]
    [InfoBox("ขนาดของ growthPrefabs และ stageDurations ควรเท่ากัน")]
    public GameObject[] growthPrefabs;

    [BoxGroup("Growth")]
    public float[] stageDurations;

    [BoxGroup("Growth")]
    public bool requiresWaterEachStage = true;

    [BoxGroup("Harvest"), InlineEditor]
    public ItemSO harvestItem;

    [BoxGroup("Harvest"), LabelWidth(80)]
    public Vector2Int yieldRange = new Vector2Int(1, 1);

    [BoxGroup("Harvest")]
    public bool destroyOnHarvest = true;
}
