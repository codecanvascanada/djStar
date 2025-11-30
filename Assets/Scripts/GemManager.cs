using UnityEngine;
using System;

public class GemManager : MonoBehaviour
{
    public static GemManager instance;

    private const string GemBalanceKey = "GemBalance";

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

    // Get current Gem balance
    public int GetGemBalance()
    {
        return PlayerPrefs.GetInt(GemBalanceKey, 0);
    }

    // Add Gems to the player's balance
    public void AddGems(int amount)
    {
        if (amount < 0)
        {
            UnityEngine.Debug.LogWarning("Attempted to add negative Gems. Use SpendGems for spending.");
            return;
        }
        int currentBalance = GetGemBalance();
        PlayerPrefs.SetInt(GemBalanceKey, currentBalance + amount);
        PlayerPrefs.Save();
                    UnityEngine.Debug.Log(string.Format("Added {0} Gems. New balance: {1}", amount, GetGemBalance()));    }

    // Spend Gems from the player's balance
    public bool SpendGems(int amount)
    {
        if (amount < 0)
        {
            UnityEngine.Debug.LogWarning("Attempted to spend negative Gems. Use AddGems for adding.");
            return false;
        }
        int currentBalance = GetGemBalance();
        if (currentBalance >= amount)
        {
            PlayerPrefs.SetInt(GemBalanceKey, currentBalance - amount);
            PlayerPrefs.Save();
            UnityEngine.Debug.Log(string.Format("Spent {0} Gems. New balance: {1}", amount, GetGemBalance()));
            return true;
        }
        else
        {
            UnityEngine.Debug.Log("Not enough Gems to spend.");
            return false;
        }
    }

    // For debugging/testing: Reset Gem balance
    public void ResetGems()
    {
        PlayerPrefs.SetInt(GemBalanceKey, 0);
        PlayerPrefs.Save();
        UnityEngine.Debug.Log("Gem balance reset to 0.");
    }
}
