using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI priceText;
    public Button purchaseButton;

    // Setup for Gem items (purchased with real money)
    public void SetupGemItem(string description, string price, UnityAction purchaseAction)
    {
        if (descriptionText != null) descriptionText.text = description;
        if (priceText != null) priceText.text = price;
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(purchaseAction);
        }
    }

    // Setup for Coin items (purchased with Gems)
    public void SetupCoinItem(string description, string price, UnityAction purchaseAction)
    {
        if (descriptionText != null) descriptionText.text = description;
        if (priceText != null) priceText.text = price;
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(purchaseAction);
        }
    }
}
