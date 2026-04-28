using System;
using System.Collections;
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
    void Awake()
    {
        if (recipeContainer == null)
        {
            // Find the container by name or tag if it lives in the UI Canvas
            GameObject containerObj = GameObject.Find("RecipeListContent");
            if (containerObj != null) recipeContainer = containerObj.transform;
        }
    }

    void Start()
    {
        CraftingManager.Instance.OnRecipeCrafted -= HandleRecipeCrafted;
        CraftingManager.Instance.OnRecipeLearned -= HandleRecipeLearned;

        CraftingManager.Instance.OnRecipeCrafted += HandleRecipeCrafted;
        CraftingManager.Instance.OnRecipeLearned += HandleRecipeLearned;

        foreach (CategoryButton cb in categoryButtons)
        {
            RecipeCategory captured = cb.category; 
            cb.button.onClick.AddListener(() => OnCategoryButtonClicked(captured));
        }
    }

    void OnEnable()
    {
        if (CraftingManager.Instance == null)
        {
            Debug.Log("CraftingManager instance is null when CraftingMainUI enabled. Cannot subscribe to events or request recipes.");
            return;
        }
        else
        {
            Debug.Log("CraftingManager instance found when CraftingMainUI enabled. Subscribing to events.");
        }

        if (!CraftingManager.Instance.IsSpawned)
        {
            Debug.LogWarning("CraftingManager is not spawned yet. Waiting...");
            StartCoroutine(WaitAndRequest());
            return;
        }

        CraftingManager.Instance.OnRecipesUpdated += RefreshDisplay;
        RequestData();
    }
    IEnumerator WaitAndRequest()
    {
        // Wait until the network says this object is officially in the game
        yield return new WaitUntil(() => CraftingManager.Instance.IsSpawned);
        RequestData();
    }

    public void OnCategoryButtonClicked(RecipeCategory category)
    {
        if (currentCategory == category) return;
        currentCategory = category;
        RequestData();
    }

    private void RequestData()
    {
        CraftingManager.Instance.RequestAvailableRecipesRpc(currentCategory);
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
        if (recipeItemPrefab == null)
        {
            Debug.Log("recipeItemPrefab is not assigned on " + gameObject.name, gameObject);
            return;
        }
        // 1. Clear the current Grid Layout children
        Debug.Log($"Refreshing recipe display with {recipeIds.Length} recipes. Clearing existing items.");
        if (recipeContainer == null)
        {
            Debug.Log("Recipe container reference is missing in CraftingMainUI. Cannot refresh display.");
            return;
        }
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

    // private string GetFullPath(Transform t)
    // {
    //     string path = t.name;
    //     while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
    //     return path;
    // }

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
