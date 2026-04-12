using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemView : MonoBehaviour
{
    [Header("UI Refs")]
    public Image icon;
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI priceLabel;
    public TMP_InputField amountInput;
    public Button minusBtn;
    public Button plusBtn;
    public Button buyBtn;

    ItemSO _item;
    int _priceEach;
    int _maxPerClick = 99;
    Action<ItemSO, int, int> _onBuy;
    const int MIN_AMOUNT = 1;

    void Awake()
    {
        if (amountInput)
            amountInput.onEndEdit.AddListener(OnAmountEdited);
        if (minusBtn) minusBtn.onClick.AddListener(() => ChangeAmount(-1));
        if (plusBtn) plusBtn.onClick.AddListener(() => ChangeAmount(+1));
        if (buyBtn) buyBtn.onClick.AddListener(BuyNow);
    }

    public void Setup(ItemSO item, int priceEach, int maxPerClick, Action<ItemSO, int, int> onBuy)
    {
        _item = item;
        _priceEach = Mathf.Max(0, priceEach);
        _maxPerClick = Mathf.Max(1, maxPerClick);
        _onBuy = onBuy;

        if (icon) icon.sprite = item ? item.icon : null;
        if (nameLabel) nameLabel.text = item ? item.itemName : "?";
        if (priceLabel) priceLabel.text = _priceEach.ToString();
        if (amountInput) amountInput.text = MIN_AMOUNT.ToString();
    }

    void OnAmountEdited(string s)
    {
        if (!int.TryParse(s, out int v)) v = MIN_AMOUNT;
        v = Mathf.Clamp(v, MIN_AMOUNT, _maxPerClick);
        if (amountInput) amountInput.text = v.ToString();
    }

    void ChangeAmount(int delta)
    {
        int v = MIN_AMOUNT;
        if (amountInput && int.TryParse(amountInput.text, out int cur)) v = cur;
        v = Mathf.Clamp(v + delta, MIN_AMOUNT, _maxPerClick);
        if (amountInput) amountInput.text = v.ToString();
    }

    void BuyNow()
    {
        int amt = MIN_AMOUNT;
        if (amountInput && int.TryParse(amountInput.text, out int cur)) amt = cur;
        amt = Mathf.Clamp(amt, MIN_AMOUNT, _maxPerClick);
        _onBuy?.Invoke(_item, _priceEach, amt);
    }
}
