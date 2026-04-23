using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Signals/InventoryDataSignal")]
public class InventoryDataSignal : ScriptableObject
{
    public event Action<InventoryData> OnDataUpdate;

    public void UpdateInventoryData(InventoryData data) => OnDataUpdate?.Invoke(data);
}
