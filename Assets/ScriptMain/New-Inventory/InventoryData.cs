using Unity.Netcode;
using UnityEngine;

public class InventoryData : NetworkBehaviour
{
    [SerializeField] private int inventorySize = 20;
    [SerializeField] private InventoryDataSignal connectionSignal;
    public NetworkList<NetworkItems> InventoryItems;

    void Awake()
    {
        InventoryItems = new NetworkList<NetworkItems>();
    }

    public override void OnNetworkSpawn()
    {
        // CRITICAL: Only the Local Player (you) should trigger the UI.
        // We don't want Player B's spawn to open Player A's inventory!
        if (IsServer)
        {
            InitializeInventory();
        }
        if (IsOwner)
        {
            connectionSignal.UpdateInventoryData(this);
        }
    }

    private void InitializeInventory()
    {
        // 1. First, ensure the list has exactly 16 slots.
        // This turns a "count 0" list into a "count 16" list of empty items.
        if (InventoryItems.Count == 0)
        {
            for (int i = 0; i < 16; i++)
            {
                InventoryItems.Add(new NetworkItems { ItemID = 0, Amount = 0 });
            }
        }

        // 2. Now, inject your MOCK DATA for testing.
        // Since we now have 16 slots, we use the [index] to replace them.
        InventoryItems[0] = new NetworkItems { ItemID = 1, Amount = 1 }; // e.g. Sword
        InventoryItems[1] = new NetworkItems { ItemID = 2, Amount = 5 }; // e.g. Potions

        Debug.Log("Server: Inventory Initialized with Mock Data.");
    }

    [ServerRpc]
    public void RequestAddItemServerRpc(int id, int amount)
    {
        // 1. Logic: Check if we already have this item to stack it
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].ItemID == id)
            {
                var updatedItem = InventoryItems[i];
                updatedItem.Amount += amount;
                InventoryItems[i] = updatedItem; // Syncs the change
                return;
            }
        }

        if (InventoryItems.Count >= inventorySize)
        {
            Debug.LogWarning("Inventory is full!");
            return;
        }

        // 2. Logic: If not found, add a new entry
        InventoryItems.Add(new NetworkItems
        {
            ItemID = id,
            Amount = amount,
        });
    }

    [ServerRpc]
    public void RequestDropItemServerRpc(int index)
    {
        if (index < InventoryItems.Count)
        {
            // Logic: Spawn the item in the 3D world here before removing
            InventoryItems.RemoveAt(index);
        }
    }
}
