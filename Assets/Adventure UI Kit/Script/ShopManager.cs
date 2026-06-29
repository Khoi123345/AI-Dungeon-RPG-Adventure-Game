using UnityEngine;
using UnityEngine.UI; // Thư viện chứa UI Text và Button Legacy

public class ShopManager : MonoBehaviour
{
    [Header("Detail UI Panel")]
    public Text detailNameText;        // Chỉnh từ TextMeshProUGUI thành Text Legacy
    public Text detailDescriptionText; // Chỉnh từ TextMeshProUGUI thành Text Legacy

    [Header("Shop Buttons")]
    public Button buyButton; 

    private ShopSlot currentlySelectedSlot; 

    void Start()
    {
        buyButton.interactable = false;
        ClearDetailWindow();
    }

    public void SelectNewItem(ShopSlot clickedSlot)
    {
        if (currentlySelectedSlot != null)
        {
            currentlySelectedSlot.selectBorder.SetActive(false);
        }

        currentlySelectedSlot = clickedSlot;
        currentlySelectedSlot.selectBorder.SetActive(true);

        ShopItemData data = clickedSlot.shopItemData;
        detailNameText.text = data.itemName;
        detailDescriptionText.text = $"{data.itemDescription}\n\nPrice: {data.price} Gold";

        buyButton.interactable = true;
    }

    public void OnBuyButtonClicked()
    {
        if (currentlySelectedSlot != null)
        {
            Debug.Log($"Purchased successfully: {currentlySelectedSlot.shopItemData.itemName}!");
        }
    }

    void ClearDetailWindow()
    {
        detailNameText.text = "NOT SELECTED";
        detailDescriptionText.text = "Please select an item in the shop to view details.";
    }
}