using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ShibaFarm/Shop Definition")]
public class ShopDefinition : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public ItemSO item;
        [Min(0)] public int price = 10;
        [Min(1)] public int maxPerClick = 1;

        [Header("Category")]
        public ShopCategory category = ShopCategory.Others; // << เพิ่มหมวด
    }

    public List<Entry> items = new List<Entry>();
}
