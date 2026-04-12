using UnityEngine;

public class PlayerPickup : MonoBehaviour
{
    public float pickupRadius = 2f;
    public LayerMask pickupLayer;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryPickup();
        }
    }

    void TryPickup()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius, pickupLayer);
        foreach (var hit in hits)
        {
            Pickupable pickupable = hit.GetComponent<Pickupable>();
            if (pickupable != null)
            {
                bool added = InventoryUI.Instance.AddItemToInventory(pickupable.itemData);
                if (added)
                {
                    Destroy(hit.gameObject); // เก็บสำเร็จ
                    break;
                }
            }
        }
    }

}
