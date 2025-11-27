using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ShopUIManager : MonoBehaviour
{
    [Header("Currency Display")]
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI gemText;

    [Header("Shop Item UI")]
    public GameObject shopItemPrefab; // A prefab for displaying a shop item
    public Transform gemPacksContentParent;
    public Transform coinPacksContentParent;

    void Start()
    {
        UpdateCurrencyDisplay();
        PopulateShop();
    }

    // Updates the coin and gem balance text on the screen
    public void UpdateCurrencyDisplay()
    {
        if (coinText != null && CurrencyManager.instance != null)
        {
            coinText.text = CurrencyManager.instance.GetCoinBalance().ToString();
        }
        if (gemText != null && GemManager.instance != null)
        {
            gemText.text = GemManager.instance.GetGemBalance().ToString();
        }
    }

    // Dynamically creates the shop item buttons
    private void PopulateShop()
    {
        if (shopItemPrefab == null)
        {
            UnityEngine.Debug.LogError("ShopItemPrefab is not assigned in the ShopUIManager.");
            return;
        }

        // Populate Gem Packs
        if (gemPacksContentParent != null && ShopManager.instance != null)
        {
            foreach (var pack in ShopManager.instance.gemPacks)
            {
                GameObject itemGO = Instantiate(shopItemPrefab, gemPacksContentParent);
                // Assuming the prefab has a script like ShopItemUI to set its values
                ShopItemUI itemUI = itemGO.GetComponent<ShopItemUI>();
                if (itemUI != null)
                {
                    string description = $"{pack.gemsAmount} (+{pack.bonusGems}) Gems";
                    itemUI.SetupGemItem(description, pack.priceString, () => {
                        ShopManager.instance.BuyGemPack(pack.id);
                        // After purchase, update the display
                        UpdateCurrencyDisplay();
                    });
                }
            }
        }

        // Populate Coin Packs
        if (coinPacksContentParent != null && ShopManager.instance != null)
        {
            foreach (var pack in ShopManager.instance.coinPacks)
            {
                GameObject itemGO = Instantiate(shopItemPrefab, coinPacksContentParent);
                ShopItemUI itemUI = itemGO.GetComponent<ShopItemUI>();
                if (itemUI != null)
                {
                    string description = $"{pack.coinsAmount} Coins";
                    string price = $"{pack.gemCost} Gems";
                    itemUI.SetupCoinItem(description, price, () => {
                        ShopManager.instance.BuyCoinPack(pack.id);
                        // After purchase, update the display
                        UpdateCurrencyDisplay();
                    });
                }
            }
        }
    }
}
