using UnityEngine;
using System;

public class ShopManager : MonoBehaviour
{
    public static ShopManager instance;

    // Gem Pack Prices (Example based on previous discussion)
    // In a real game, these would come from a server or a ScriptableObject for easier balancing.
    [Serializable]
    public class GemPack
    {
        public string id;
        public int gemsAmount;
        public int bonusGems;
        public float realMoneyPrice; // e.g., 1.2f for ₩1,200
        public string priceString; // e.g., "₩1,200"
    }

    public GemPack[] gemPacks = new GemPack[]
    {
        new GemPack { id = "gem_pack_small", gemsAmount = 120, bonusGems = 0, realMoneyPrice = 1.2f, priceString = "$1.20" },
        new GemPack { id = "gem_pack_medium", gemsAmount = 600, bonusGems = 60, realMoneyPrice = 5.9f, priceString = "$5.90" },
        new GemPack { id = "gem_pack_large", gemsAmount = 1200, bonusGems = 180, realMoneyPrice = 11.9f, priceString = "$11.90" },
        new GemPack { id = "gem_pack_xl", gemsAmount = 3000, bonusGems = 600, realMoneyPrice = 29.0f, priceString = "$29.00" },
        new GemPack { id = "gem_pack_xxl", gemsAmount = 6000, bonusGems = 1500, realMoneyPrice = 59.0f, priceString = "$59.00" },
        new GemPack { id = "gem_pack_mega", gemsAmount = 12000, bonusGems = 4000, realMoneyPrice = 119.0f, priceString = "$119.00" }
    };

    // Coin Pack Prices (Example)
    [Serializable]
    public class CoinPack
    {
        public string id;
        public int coinsAmount;
        public int gemCost;
    }

    public CoinPack[] coinPacks = new CoinPack[]
    {
        new CoinPack { id = "coin_pack_small", coinsAmount = 1000, gemCost = 100 }, // 10 plays
        new CoinPack { id = "coin_pack_medium", coinsAmount = 3000, gemCost = 280 }, // 30 plays, slight discount
        new CoinPack { id = "coin_pack_large", coinsAmount = 6000, gemCost = 500 } // 60 plays, better discount
    };


    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- Gem Purchase Functions ---
    // In a real game, these would integrate with an IAP (In-App Purchase) system.
    // For now, we'll simulate a successful purchase.
    public void BuyGemPack(string packId)
    {
        GemPack pack = Array.Find(gemPacks, p => p.id == packId);
        if (pack != null)
        {
            // Simulate IAP purchase success
            Debug.Log($"Simulating purchase of {pack.id} for {pack.priceString}.");
            GemManager.instance.AddGems(pack.gemsAmount + pack.bonusGems);
            Debug.Log($"Successfully purchased {pack.gemsAmount + pack.bonusGems} Gems!");
            // Here you would typically trigger a UI update for Gem balance
        }
        else
        {
            Debug.LogError($"Gem pack with ID '{packId}' not found.");
        }
    }

    // --- Coin Purchase Functions ---
    public void BuyCoinPack(string packId)
    {
        CoinPack pack = Array.Find(coinPacks, p => p.id == packId);
        if (pack != null)
        {
            if (GemManager.instance.SpendGems(pack.gemCost))
            {
                // Call AddCoins with bypassCap set to true
                CurrencyManager.instance.AddCoins(pack.coinsAmount, true); 
                Debug.Log($"Successfully purchased {pack.coinsAmount} Coins for {pack.gemCost} Gems!");
                // Here you would typically trigger a UI update for Coin and Gem balances
            }
            else
            {
                Debug.Log("Not enough Gems to buy this Coin pack.");
                // Here you would typically show a UI message to the user
            }
        }
        else
        {
            Debug.LogError($"Coin pack with ID '{packId}' not found.");
        }
    }

    // --- Other Shop Functions (DLC, Gacha, etc.) would go here ---
    // For example:
    // public void BuySong(string songId) { ... }
    // public void SpinGacha(string gachaId) { ... }
}
