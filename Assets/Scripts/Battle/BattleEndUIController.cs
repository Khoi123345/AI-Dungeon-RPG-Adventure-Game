using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GameShared.Models; // DTO/Models dùng chung từ shared/Models/
using GameShared.DTOs.Inventory; // Import LootDropDTO dùng chung từ shared/DTOs/

public class BattleEndUIController : MonoBehaviour
{
    #region REGION 1: UI REFERENCES (CÁC THAM CHIẾU GIAO DIỆN)
    [Header("UI Panels")]
    [SerializeField] private Image overlay;                // Nền tối mờ phía sau các bảng pop-up
    [SerializeField] private GameObject victoryPanel;      // Bảng chiến thắng (Victory Panel)
    [SerializeField] private GameObject defeatPanel;       // Bảng thất bại (Defeat Panel)

    [Header("Item Slots")]
    [Tooltip("Danh sách chứa 3 ô hiển thị hình ảnh vật phẩm rơi ra khi chiến thắng")]
    [SerializeField] private List<InventorySlotUI> itemSlots = new List<InventorySlotUI>();

    [Header("Databases (Cơ sở dữ liệu hỗ trợ)")]
    [Tooltip("Danh sách chứa tất cả ItemData mẫu để đối chiếu và lấy hình ảnh hiển thị dựa trên itemId")]
    [SerializeField] private List<ItemData> itemDatabase = new List<ItemData>();

    private List<LootDrop> currentBattleDrops; // Lưu trữ danh sách vật phẩm rơi để gửi API khi bấm Confirm
    #endregion

    #region REGION 2: UNITY LIFE CYCLE (VÒNG ĐỜI UNITY)
    private void Start()
    {
        // Ẩn tất cả bảng và nền mờ khi trận đấu bắt đầu để đảm bảo giao diện sạch
        InitializeUI();
    }
    #endregion

    #region REGION 3: UI TRIGGER METHODS (CÁC HÀM KÍCH HOẠT GIAO DIỆN)
    /// <summary>
    /// Kích hoạt màn hình Chiến Thắng và hiển thị vật phẩm rơi ra.
    /// </summary>
    /// <param name="droppedItems">Danh sách vật phẩm rơi ra nhận về từ backend</param>
    public void TriggerVictory(List<LootDrop> droppedItems)
    {
        // Stop any running animations to avoid conflicts
        StopAllCoroutines();

        currentBattleDrops = droppedItems; // Lưu lại danh sách vật phẩm rơi để sử dụng khi người chơi ấn Xác Nhận

        // 1. Hiển thị và chạy hiệu ứng làm mờ nền tối
        if (overlay != null)
        {
            overlay.gameObject.SetActive(true);
            StartCoroutine(FadeOverlay(0f, 0.75f, 0.5f));
        }

        // 2. Hiển thị Victory Panel với hiệu ứng Pop-up Native
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            StartCoroutine(PopupPanel(victoryPanel, 0.6f));
        }

        // 3. Đổ dữ liệu vật phẩm vào các slot UI
        PopulateLootItems(droppedItems);
    }

    /// <summary>
    /// Kích hoạt màn hình Thất Bại.
    /// </summary>
    public void TriggerDefeat()
    {
        StopAllCoroutines();

        // 1. Hiển thị và chạy hiệu ứng làm mờ nền tối
        if (overlay != null)
        {
            overlay.gameObject.SetActive(true);
            StartCoroutine(FadeOverlay(0f, 0.75f, 0.5f));
        }

        // 2. Hiển thị Defeat Panel với hiệu ứng Pop-up Native
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(true);
            StartCoroutine(PopupPanel(defeatPanel, 0.6f));
        }
    }
    #endregion

    #region REGION 4: INTERACTION BUTTONS (SỰ KIỆN NÚT BẤM)
    /// <summary>
    /// Hàm xử lý sự kiện bấm nút "Xác nhận" (Confirm) ở Victory Panel.
    /// Gửi thông tin lên Backend để lưu vật phẩm vào túi đồ và chuyển Scene.
    /// </summary>
    public async void OnConfirmVictory()
    {
        Debug.Log("[BattleEndUI] Người chơi xác nhận kết quả. Đang đồng bộ phần thưởng với Backend...");

        if (currentBattleDrops == null || currentBattleDrops.Count == 0)
        {
            Debug.LogWarning("[BattleEndUI] Không tìm thấy danh sách vật phẩm rơi ra để đồng bộ.");
            SceneManager.LoadScene("StoryScene");
            return;
        }

        try
        {
            // 1. Lấy ID người chơi và ID trận đấu từ danh sách vật phẩm rơi
            string charId = "mock-player-id";
            if (GameProgressService.Instance != null && GameProgressService.Instance.CurrentCharacter != null)
            {
                charId = GameProgressService.Instance.CurrentCharacter.characterId;
            }

            string bId = "mock-battle-id";
            if (currentBattleDrops.Count > 0 && currentBattleDrops[0] != null)
            {
                bId = currentBattleDrops[0].battleId;
            }

            // 2. Chuyển đổi sang danh sách các item kèm số lượng tương ứng với bảng LootDrop trong CSDL
            List<LootItemDTO> itemsPayload = new List<LootItemDTO>();
            foreach (var drop in currentBattleDrops)
            {
                if (drop != null && !string.IsNullOrEmpty(drop.itemId))
                {
                    itemsPayload.Add(new LootItemDTO
                    {
                        itemId = drop.itemId,
                        quantity = drop.quantity
                    });
                }
            }

            // 3. Chuẩn bị payload DTO đồng bộ đầy đủ các trường của CSDL (LootDrop & Inventory)
            LootDropDTO payload = new LootDropDTO
            {
                playerId = charId,
                battleId = bId,
                items = itemsPayload
            };

            Debug.Log($"[BattleEndUI] Gửi API POST /api/inventory/add-loot cho Player: {charId}, Battle: {bId}");

            // 4. Gọi API gửi dạng POST RAW JSON
            string jsonPayload = JsonUtility.ToJson(payload);
            string responseJson = await ApiClient.Instance.PostRawAsync("/api/inventory/add-loot", jsonPayload);

            if (responseJson != null)
            {
                Debug.Log($"[BattleEndUI] Gửi API thành công! Phản hồi từ server: {responseJson}");
            }
            else
            {
                Debug.LogError("[BattleEndUI] Gửi API thất bại hoặc server không phản hồi kết quả thành công.");
            }
        }
        catch (Exception ex)
        {
            // Xử lý lỗi nếu server sập hoặc mất mạng
            Debug.LogError($"[BattleEndUI] Lỗi kết nối API trong quá trình đồng bộ: {ex.Message}");
        }

        // Chuyển scene về StoryScene
        Debug.Log("[BattleEndUI] Đang chuyển hướng về StoryScene.unity...");
        SceneManager.LoadScene("StoryScene");
    }

    /// <summary>
    /// Hàm xử lý sự kiện bấm nút "Return to main menu" ở Defeat Panel.
    /// Đưa người chơi quay lại màn hình Menu chính.
    /// </summary>
    public void OnReturnToMainMenu()
    {
        Debug.Log("[BattleEndUI] Quay trở lại Menu chính...");
        SceneManager.LoadScene("DemoMenu");
    }
    #endregion

    #region REGION 5: NATIVE ANIMATIONS (HIỆU ỨNG COROUTINE TỰ NHIÊN)
    /// <summary>
    /// Coroutine xử lý Fade mờ nền tối.
    /// </summary>
    private IEnumerator FadeOverlay(float startAlpha, float targetAlpha, float duration)
    {
        float elapsed = 0f;
        Color color = overlay.color;
        color.a = startAlpha;
        overlay.color = color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);
            overlay.color = color;
            yield return null;
        }

        color.a = targetAlpha;
        overlay.color = color;
    }

    /// <summary>
    /// Coroutine tạo hiệu ứng Pop-up nảy (EaseOutBack) không dùng thư viện ngoài.
    /// </summary>
    private IEnumerator PopupPanel(GameObject panel, float duration)
    {
        float elapsed = 0f;
        panel.transform.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float scaleFactor = EaseOutBack(normalizedTime);
            panel.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            yield return null;
        }

        panel.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Hàm nội suy toán học cho EaseOutBack.
    /// </summary>
    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float tMinus1 = t - 1f;
        return 1f + c3 * tMinus1 * tMinus1 * tMinus1 + c1 * tMinus1 * tMinus1;
    }
    #endregion

    #region REGION 6: HELPER METHODS (HÀM BỔ TRỢ HỆ THỐNG)
    /// <summary>
    /// Khởi tạo trạng thái ban đầu của UI.
    /// </summary>
    private void InitializeUI()
    {
        if (overlay != null) overlay.gameObject.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);

        foreach (var slot in itemSlots)
        {
            if (slot != null)
            {
                slot.ClearSlot();
                slot.gameObject.SetActive(false);
            }
        }
    }

    private void PopulateLootItems(List<LootDrop> droppedItems)
    {
        for (int i = 0; i < itemSlots.Count; i++)
        {
            // Kiểm tra an toàn tránh NullReferenceException nếu ô Slot UI chưa được gán trong Inspector
            if (itemSlots[i] == null) continue;

            if (i < droppedItems.Count && droppedItems[i] != null)
            {
                LootDrop drop = droppedItems[i];
                // Tìm kiếm ItemData mẫu (Thêm kiểm tra x != null để tránh lỗi nếu danh sách database chứa phần tử rỗng/None)
                ItemData matchData = itemDatabase.Find(x => x != null && (x.name == drop.itemId || x.itemName == drop.itemId));

                if (matchData != null)
                {
                    itemSlots[i].AddItemToSlot(matchData, drop.quantity);
                    itemSlots[i].gameObject.SetActive(true);

                    Debug.Log($"[BattleEndUI] Đã thêm hình ảnh vật phẩm '{matchData.itemName}' x{drop.quantity} vào ô hiển thị UI.");
                }
                else
                {
                    Debug.LogWarning($"[BattleEndUI] Không tìm thấy ItemData mẫu cho itemId '{drop.itemId}' trong Database.");
                    itemSlots[i].ClearSlot();
                    itemSlots[i].gameObject.SetActive(false);
                }
            }
            else
            {
                // Ẩn ô thừa nếu số lượng vật phẩm rơi ra ít hơn 3
                itemSlots[i].ClearSlot();
                itemSlots[i].gameObject.SetActive(false);
            }
        }
    }
    #endregion
}


