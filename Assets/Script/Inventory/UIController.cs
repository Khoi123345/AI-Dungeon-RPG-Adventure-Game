using UnityEngine;

public class UIController : MonoBehaviour
{
    // Kéo Panel_Inventory trong Hierarchy vào đây
    [SerializeField] private GameObject panelInventory;

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
            panelInventory.SetActive(!panelInventory.activeSelf);
        }
    }
}