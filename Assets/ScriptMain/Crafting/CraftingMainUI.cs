using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CraftingMainUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("Detail Panel References")]
    [SerializeField] private TextMeshProUGUI detailName;
    [SerializeField] private TextMeshProUGUI detailDesc;
    [SerializeField] private Image detailIcon;
    [SerializeField] private Transform ingredientListParent;
    [SerializeField] private Transform recipeContainer;
    [SerializeField] private GameObject recipeItemPrefab;
    [Header("Buttons")]
    [SerializeField] private Button craftButton;
    [SerializeField] private List<CategoryButton> categoryButtons;
    [Serializable]
    public struct CategoryButton
    {
        public RecipeCategory category;
        public Button button;
    }

    private RecipeCategory currentCategory = RecipeCategory.Tools;

    void Start()
    {
        CraftingManager.Instance.OnRecipeCrafted -= HandleRecipeCrafted;
        CraftingManager.Instance.OnRecipeLearned -= HandleRecipeLearned;

        CraftingManager.Instance.OnRecipeCrafted += HandleRecipeCrafted;
        CraftingManager.Instance.OnRecipeLearned += HandleRecipeLearned;
    }

    void OnEnable()
    {
        if(CraftingManager.Instance == null)
        {
            Debug.Log("CraftingManager instance is null when CraftingMainUI enabled. Cannot subscribe to events or request recipes.");
            return;
        }
        else
        {
            Debug.Log("CraftingManager instance found when CraftingMainUI enabled. Subscribing to events.");
        }
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                CraftingManager.Instance.OnRecipesUpdated += RefreshDisplay;
                Debug.Log("Requesting available recipes for category: " + currentCategory);
            }
            else
            {
                Debug.LogWarning("PlayerObject is null when CraftingMainUI enabled. Recipes will not be requested.");
            }
        }
        else
        {
            Debug.LogWarning("NetworkManager is not ready when CraftingMainUI enabled. Recipes will not be requested.");
        }
        Debug.Log("CraftingMainUI Enabled. Subscribing to CraftingManager events and requesting recipes if possible.");
    }
    void OnDisable()
    {
        if (CraftingManager.Instance != null)
            CraftingManager.Instance.OnRecipesUpdated -= RefreshDisplay;
    }

    private void HandleRecipeLearned(string obj)
    {
        throw new NotImplementedException();
    }

    private void HandleRecipeCrafted(CraftingRecipeSO sO)
    {
        throw new NotImplementedException();
    }

    public void RefreshDisplay(int[] recipeIds)
    {
        // 1. Clear the current Grid Layout children
        foreach (Transform child in recipeContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Spawn new items
        foreach (int id in recipeIds)
        {
            CraftingRecipeSO recipe = GameDataManager.Instance.craftRecipeDatabase.GetRecipeByID(id);
            if (recipe != null)
            {
                // Instantiate your Item UI prefab and set its data
                GameObject item = Instantiate(recipeItemPrefab, recipeContainer);
                item.GetComponent<RecipeUIItem>().Setup(recipe);
            }
        }
    }

    public void SelectRecipe(CraftingRecipeSO recipe)
    {
        detailName.text = recipe.recipeName;
        detailDesc.text = recipe.description;
        detailIcon.sprite = recipe.icon;

        // Refresh the ingredients needed
        UpdateIngredientDisplay(recipe);
    }

    private void UpdateIngredientDisplay(CraftingRecipeSO recipe)
    {
        throw new NotImplementedException();
    }
}
