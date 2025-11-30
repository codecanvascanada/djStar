using UnityEngine;
using System;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager instance;

    private const string CoinBalanceKey = "CoinBalance";
    private const string LastCoinGrantTimeKey = "LastCoinGrantTime";
    private const int InitialCoins = 1200; // Initial coins given to new players
    private const int HourlyCoinGrant = 100; // Coins granted per hour
    private const int CoinCap = 1200; // Maximum coins a player can hold

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCoins();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        GrantHourlyCoins();
    }

    private void InitializeCoins()
    {
        if (!PlayerPrefs.HasKey(CoinBalanceKey))
        {
            PlayerPrefs.SetInt(CoinBalanceKey, InitialCoins);
            PlayerPrefs.SetString(LastCoinGrantTimeKey, DateTime.UtcNow.ToString());
            PlayerPrefs.Save();
        }
    }

    private void GrantHourlyCoins()
    {
        if (!PlayerPrefs.HasKey(LastCoinGrantTimeKey))
        {
            // This should not happen if InitializeCoins is called correctly, but as a safeguard
            PlayerPrefs.SetString(LastCoinGrantTimeKey, DateTime.UtcNow.ToString());
            PlayerPrefs.Save();
            return;
        }

        DateTime lastGrantTime = DateTime.Parse(PlayerPrefs.GetString(LastCoinGrantTimeKey));
        TimeSpan timeSinceLastGrant = DateTime.UtcNow - lastGrantTime;

        int hoursPassed = (int)timeSinceLastGrant.TotalHours;

        if (hoursPassed > 0)
        {
            int currentBalance = GetCoinBalance();
            int potentialGrant = hoursPassed * HourlyCoinGrant;
            int actualGrant = 0;

            if (currentBalance + potentialGrant > CoinCap)
            {
                actualGrant = CoinCap - currentBalance;
                if (actualGrant < 0) actualGrant = 0; // Already over cap
            }
            else
            {
                actualGrant = potentialGrant;
            }

            if (actualGrant > 0)
            {
                AddCoins(actualGrant);
            }
        }
        // Update last grant time to now, regardless of whether coins were granted
        PlayerPrefs.SetString(LastCoinGrantTimeKey, DateTime.UtcNow.ToString());
        PlayerPrefs.Save();
    }

    public int GetCoinBalance()
    {
        return PlayerPrefs.GetInt(CoinBalanceKey, 0);
    }

    public int GetCoinCap()
    {
        return CoinCap;
    }

    public void AddCoins(int amount, bool bypassCap = false)
    {
        if (amount < 0)
        {
            return;
        }
        int currentBalance = GetCoinBalance();
        int newBalance = currentBalance + amount;

        if (!bypassCap)
        {
            newBalance = Mathf.Min(newBalance, CoinCap); // Enforce cap only if not bypassed
        }

        PlayerPrefs.SetInt(CoinBalanceKey, newBalance);
        PlayerPrefs.Save();
    }

    public bool SpendCoins(int amount)
    {
        if (amount < 0)
        {
            return false;
        }
        int currentBalance = GetCoinBalance();
        if (currentBalance >= amount)
        {
            PlayerPrefs.SetInt(CoinBalanceKey, currentBalance - amount);
            PlayerPrefs.Save();
            return true;
        }
        else
        {
            return false;
        }
    }

    // For debugging/testing: Reset Coin balance
    public void ResetCoins()
    {
        PlayerPrefs.SetInt(CoinBalanceKey, 0);
        PlayerPrefs.SetString(LastCoinGrantTimeKey, DateTime.UtcNow.ToString());
        PlayerPrefs.Save();
    }
}
