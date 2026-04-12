using UnityEngine;

public class InventoryToggle : MonoBehaviour
{
    public GameObject inventoryPanel; // Drag ช่อง InventoryPanel มาวางใน Inspector

    void Start()
    {
        inventoryPanel.SetActive(false); // ซ่อนไว้ก่อน
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(!inventoryPanel.activeSelf);
            }
        }
    }
}
