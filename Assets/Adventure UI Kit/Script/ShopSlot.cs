using UnityEngine;
using UnityEngine.UI; // Thư viện này chứa UI Text Legacy

public class ShopSlot : MonoBehaviour
{
    public ShopItemData shopItemData; 
    
    [Header("UI References")]
    public Image itemIconImage;
    public Text priceText; // Chỉnh từ TextMeshProUGUI thành Text Legacy

    public GameObject selectBorder; 
    private ShopManager shopManager;

    void Start()
    {
        shopManager = FindFirstObjectByType<ShopManager>();
        GetComponent<Button>().onClick.AddListener(OnSlotClicked);
        UpdateSlotUI();
    }

    public void UpdateSlotUI()
    {
        if (shopItemData != null && priceText != null)
        {
            itemIconImage.sprite = shopItemData.icon;
            priceText.text = shopItemData.price.ToString(); // Gán chữ cho Text Legacy vẫn y hệt
        }
    }

    void OnSlotClicked()
    {
        shopManager.SelectNewItem(this);
    }
}