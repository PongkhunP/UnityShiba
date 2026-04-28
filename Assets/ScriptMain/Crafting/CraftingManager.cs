using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class CraftingManager : NetworkBehaviour
{
    public static CraftingManager Instance { get; private set; }
    public event Action<int[]> OnRecipesUpdated;
    public event Action<CraftingRecipeSO> OnRecipeCrafted;
    public event Action<string> OnRecipeLearned;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestAvailableRecipesRpc(RecipeCategory category, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient))
        {
            var playerObject = networkClient.PlayerObject;
            if (playerObject == null)
            {
                Debug.LogError($"PlayerObject not found for client {clientId}");
                return;
            }
            Debug.Log($"Received recipe request from client {clientId} for category {category}. PlayerObject: {playerObject.name}");
            var statManager = playerObject.GetComponent<StatManager>();
            if(statManager == null)
            {
                Debug.LogError($"StatManager not found on player object for client {clientId}");
                return;
            }

            int actualLevel = statManager.GetLevelForCategory(category);

            List<CraftingRecipeSO> available = GameDataManager.Instance.craftRecipeDatabase.GetAvailableRecipes(actualLevel, category);

            int[] recipeIds = available.Select(r => r.recipeID).ToArray();

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            };

            Debug.Log($"Sending {available.Count} recipes to client {clientId} for category {category}");

            UpdateRecipeListClientRpc(recipeIds, clientRpcParams);
        }

    }

    [ClientRpc]
    private void UpdateRecipeListClientRpc(int[] recipeIds, ClientRpcParams clientRpcParams = default)
    {
        OnRecipesUpdated?.Invoke(recipeIds);
    }

}

public enum CraftResult
{
    Success,
    NotEnoughMaterials,
    NotLearned,
    InventoryFull,
    NotEnoughEnergy,
    InvalidRecipe,
    NoInventory,
}
