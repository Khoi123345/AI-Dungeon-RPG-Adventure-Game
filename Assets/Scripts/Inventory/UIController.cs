using UnityEngine;

public class UIController : MonoBehaviour
{
    // Kéo Panel_Inventory trong Hierarchy vào đây
    [SerializeField] private GameObject panelInventory;

    [Header("Panels to hide when Inventory opens")]
    [SerializeField] private GameObject[] panelsToHide;
    private bool[] savedStates;

    void Start()
    {
        // Khi vừa vào game, ẩn bảng Inventory đi để người chơi không thấy
        if (panelInventory != null)
        {
            panelInventory.SetActive(false);
        }
    }

    // Hàm mở/đóng Inventory (dùng cho nút Balo và nút Close)
    public void ToggleInventory()
    {
        if (panelInventory != null)
        {
            // Trạng thái ngược lại với trạng thái hiện tại (Đang bật -> Tắt, Đang tắt -> Bật)
            bool isActive = !panelInventory.activeSelf;
            panelInventory.SetActive(isActive);

            if (isActive)
            {
                // Mở inventory -> Lưu trạng thái và tắt các panel khác
                if (panelsToHide != null && panelsToHide.Length > 0)
                {
                    savedStates = new bool[panelsToHide.Length];
                    for (int i = 0; i < panelsToHide.Length; i++)
                    {
                        if (panelsToHide[i] != null)
                        {
                            savedStates[i] = panelsToHide[i].activeSelf;
                            panelsToHide[i].SetActive(false);
                        }
                    }
                }

                InventoryManager invManager = FindObjectOfType<InventoryManager>();
                if (invManager != null)
                {
                    invManager.RefreshInventoryUI();
                }
            }
            else
            {
                // Đóng inventory -> Khôi phục trạng thái
                RestorePanelStates();
            }
        }
    }

    // Hàm đóng Inventory (dùng riêng cho nút X / nút Đóng bên trong Panel_Inventory)
    public void CloseInventory()
    {
        if (panelInventory != null && panelInventory.activeSelf)
        {
            panelInventory.SetActive(false);
            RestorePanelStates();
        }
    }

    private void RestorePanelStates()
    {
        if (panelsToHide != null && savedStates != null && panelsToHide.Length == savedStates.Length)
        {
            for (int i = 0; i < panelsToHide.Length; i++)
            {
                if (panelsToHide[i] != null)
                {
                    panelsToHide[i].SetActive(savedStates[i]);
                }
            }
        }
    }
}
