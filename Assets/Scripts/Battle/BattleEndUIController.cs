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
    [Header("UI Panels & Buttons")]
    [SerializeField] private Image overlay;                // Nền tối mờ phía sau các bảng pop-up
    [SerializeField] private GameObject victoryPanel;      // Bảng chiến thắng (Victory Panel)
    [SerializeField] private GameObject defeatPanel;       // Bảng thất bại (Defeat Panel)
    [SerializeField] private Button confirmButton;         // Nút Confirm chiến thắng

    [Header("Defeat UI Buttons (Màn Thua Cuộc)")]
    [Tooltip("Nút Continue - Hồi sinh bằng 50 Vàng và quay lại câu chuyện trước khi đánh boss")]
    [SerializeField] private Button btnContinueRevive;     // Nút Continue
    [Tooltip("Nút Back to Menu - Chấp nhận chết luôn (chơi lại từ đầu) và về Menu chính")]
    [SerializeField] private Button btnBackToMenuReset;    // Nút Back to menu

    [Header("Item Slots")]
    [Tooltip("Danh sách chứa 3 ô hiển thị hình ảnh vật phẩm rơi ra khi chiến thắng")]
    [SerializeField] private List<InventorySlotUI> itemSlots = new List<InventorySlotUI>();

    [Header("Databases (Cơ sở dữ liệu hỗ trợ)")]
    [Tooltip("Danh sách chứa tất cả ItemData mẫu để đối chiếu và lấy hình ảnh hiển thị dựa trên itemId")]
    [SerializeField] private List<ItemData> itemDatabase = new List<ItemData>();

    private List<LootDrop> currentBattleDrops; // Lưu trữ danh sách vật phẩm rơi để gửi API khi bấm Confirm
    private int lastGoldEarned = 0;
    private int lastExpEarned = 0;
    private bool isConfirmProcessed = false; // Guard chống nút Confirm bị kích hoạt 2 lần trong 1 lần click
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
    public void TriggerVictory(List<LootDrop> droppedItems, int goldEarned = 0, int expEarned = 0)
    {
        isConfirmProcessed = false; // Reset cờ bảo vệ khi màn Victory xuất hiện

        // Stop any running animations to avoid conflicts
        StopAllCoroutines();

        currentBattleDrops = droppedItems ?? new List<LootDrop>();
        lastGoldEarned = goldEarned;
        lastExpEarned = expEarned;

        Debug.Log($"🎉 [BATTLE VICTORY] CHIẾN THẮNG TRẬN ĐẤU!\n" +
                  $"💰 Vàng nhận được: +{goldEarned} Gold\n" +
                  $"⭐ EXP nhận được: +{expEarned} XP\n" +
                  $"🎁 Số lượng Vật phẩm rơi ra: {currentBattleDrops.Count}");

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
        PopulateLootItems(currentBattleDrops);
    }

    /// <summary>
    /// Kích hoạt màn hình Thất Bại và gán tự động sự kiện cho 2 nút bấm Continue & Back to menu.
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

            // Tự động tìm 2 nút bấm trong defeatPanel nếu chưa kéo thả thủ công
            Button[] buttons = defeatPanel.GetComponentsInChildren<Button>(true);
            if (btnContinueRevive == null && buttons.Length > 0)
            {
                btnContinueRevive = buttons[0];
            }
            if (btnBackToMenuReset == null && buttons.Length > 1)
            {
                btnBackToMenuReset = buttons[1];
            }

            // Gán sự kiện Click
            if (btnContinueRevive != null)
            {
                btnContinueRevive.onClick.RemoveAllListeners();
                btnContinueRevive.onClick.AddListener(OnReviveWithGold);
            }

            if (btnBackToMenuReset != null)
            {
                btnBackToMenuReset.onClick.RemoveAllListeners();
                btnBackToMenuReset.onClick.AddListener(OnGiveUpAndResetToMenu);
            }
        }
    }
    #endregion

    #region REGION 4: INTERACTION BUTTONS (SỰ KIỆN NÚT BẤM)
    /// <summary>
    /// Sự kiện gán cho Nút 1: "Continue" (Hồi sinh bằng Vàng).
    /// Tốn 50 Vàng, hồi sinh 100% HP và quay lại câu chuyện trước khi đánh Boss để chơi tiếp.
    /// </summary>
    public void OnReviveWithGold()
    {
        int cost = GameShared.Config.GameConstants.InstantReviveCost; // 50 Gold
        var character = GameProgressService.Instance?.CurrentCharacter;

        if (character == null)
        {
            Debug.LogWarning("[BattleEndUI] Không tìm thấy dữ liệu Nhân vật.");
            SceneManager.LoadScene("StoryScene");
            return;
        }

        if (character.gold < cost)
        {
            Debug.LogWarning($"⚠️ [REVIATION FAILED] Không đủ Vàng để hồi sinh! (Yêu cầu: {cost} Gold, Hiện có: {character.gold} Gold).");
            // Người chơi không đủ tiền -> Buộc phải chọn Chết luôn để reset game
            return;
        }

        bool revived = GameProgressService.Instance.ReviveCharacterWithGold(cost);
        if (revived)
        {
            // ✅ Fix: Reset currentNodeId từ "boss_room" về location hiện tại
            // để khi resume, backend không tự động trigger battle lại ngay lập tức.
            var session = GameProgressService.Instance?.CurrentStorySession;
            if (session != null && string.Equals(session.currentNodeId, "boss_room", System.StringComparison.OrdinalIgnoreCase))
            {
                string fallbackNode = session.currentLocation ?? "ancient_cave";
                Debug.Log($"[BattleEndUI] Revive: Reset currentNodeId từ 'boss_room' → '{fallbackNode}' để tránh re-trigger battle ngay.");
                GameProgressService.Instance.SetCurrentStorySession(session.sessionId, fallbackNode, session.currentLocation);
            }

            Debug.Log($"✨ [REVIATION SUCCESS] Đã trừ {cost} Gold. Nhân vật {character.name} được hồi sinh 100% HP!");
            Debug.Log("🚗 [SCENE TRANSITION] Quay trở lại StoryScene.unity trước khi đánh Boss để tiếp tục hành trình...");
            SceneManager.LoadScene("StoryScene");
        }
    }

    /// <summary>
    /// Sự kiện gán cho Nút 2: "Back to menu" (Chấp nhận chết luôn - Chơi lại từ đầu).
    /// Không mất tiền hồi sinh, reset toàn bộ tiến trình game về Chương 1 và quay lại Menu chính.
    /// </summary>
    public void OnGiveUpAndResetToMenu()
    {
        Debug.Log("[BattleEndUI] Người chơi chọn Chết luôn (Chấp nhận thua) -> Không tốn tiền hồi sinh, Reset game mới & Về Menu chính...");
        if (GameProgressService.Instance != null)
        {
            GameProgressService.Instance.ResetGameProgressToStartNew();
        }
        SceneManager.LoadScene("Menu");
    }

    /// <summary>
    /// Hàm xử lý sự kiện bấm nút "Return to main menu" cũ (Bảo lưu tương thích).
    /// </summary>
    public void OnReturnToMainMenu()
    {
        OnGiveUpAndResetToMenu();
    }

    /// <summary>
    /// Hàm xử lý sự kiện bấm nút "Xác nhận" (Confirm) ở Victory Panel.
    /// Gửi thông tin lên Backend để lưu vật phẩm vào túi đồ và chuyển Scene.
    /// </summary>
    public void OnConfirmVictory()
    {
        if (isConfirmProcessed)
        {
            Debug.LogWarning("⚠️ [CONFIRM SKIPPED] Sự kiện Confirm đã xử lý rồi, bỏ qua lần gọi trùng lặp.");
            return;
        }
        isConfirmProcessed = true;

        Debug.Log("▶️ [CONFIRM CLICKED] Người chơi nhấn nút Confirm (Xác nhận nhận phần thưởng).");

        // Lấy toàn bộ tất cả vật phẩm chiến lợi phẩm thu thập được từ trận đấu (Take All Mode)
        List<LootDrop> selectedDrops = currentBattleDrops != null ? new List<LootDrop>(currentBattleDrops) : new List<LootDrop>();

        Debug.Log($"🎒 [INVENTORY UPDATE] Nhận toàn bộ {selectedDrops.Count} vật phẩm chiến lợi phẩm để thêm vào CSDL và Túi đồ.");
        foreach (var drop in selectedDrops)
        {
            if (GameProgressService.Instance != null)
            {
                GameProgressService.Instance.AddItemToInventory(drop.itemId, drop.quantity, false);
            }
            Debug.Log($"✨ [ITEM ADDED TO INVENTORY] +{drop.quantity} Vật phẩm '{drop.itemId}' đã được lưu chính thức vào Túi đồ!");
        }

        Debug.Log($"💰 [REWARD UPDATE] Thêm +{lastGoldEarned} Gold | ⭐ +{lastExpEarned} EXP vào tài khoản Nhân vật.");

        if (GameProgressService.Instance != null && GameProgressService.Instance.CurrentCharacter != null)
        {
            var character = GameProgressService.Instance.CurrentCharacter;
            int oldLevel = character.level;
            character.gold += lastGoldEarned;
            character.experience += lastExpEarned;

            // Kiểm tra thăng cấp đơn giản (Mỗi 100 EXP = +1 Level)
            int requiredXp = character.level * 100;
            while (character.experience >= requiredXp)
            {
                character.experience -= requiredXp;
                character.level++;
                character.maxHp += 12;
                character.hp = character.maxHp;
                character.attack += 3;
                character.defense += 2;
                requiredXp = character.level * 100;
            }

            if (character.level > oldLevel)
            {
                Debug.Log($"🎉 [LEVEL UP!] CHÚC MỪNG! Nhân vật đã thăng cấp từ Lv.{oldLevel} ➔ Lv.{character.level}! HP Max = {character.maxHp}, Attack = {character.attack}, Defense = {character.defense}");
            }
            else
            {
                Debug.Log($"📊 [CHARACTER STATUS] Cấp độ hiện tại: Lv.{character.level} ({character.experience}/{requiredXp} EXP) | Vàng: {character.gold} Gold");
            }
        }

        Debug.Log("🚗 [SCENE TRANSITION] Quay trở lại StoryScene.unity để tiếp tục hành trình...");
        SceneManager.LoadScene("StoryScene");
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

        // Tự động tìm kiếm và gán sự kiện onClick cho Nút Confirm nếu chưa kéo vào Inspector
        if (confirmButton == null)
        {
            Transform btnTr = transform.Find("btn_Confirm");
            if (btnTr == null && victoryPanel != null) btnTr = victoryPanel.transform.Find("btn_Confirm");
            if (btnTr == null)
            {
                Button[] btns = GetComponentsInChildren<Button>(true);
                foreach (var b in btns)
                {
                    if (b.name.Equals("btn_Confirm", StringComparison.OrdinalIgnoreCase) || b.name.Contains("Confirm"))
                    {
                        confirmButton = b;
                        break;
                    }
                }
            }
            else
            {
                confirmButton = btnTr.GetComponent<Button>();
            }
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmVictory);
            confirmButton.onClick.AddListener(OnConfirmVictory);
            Debug.Log("[BattleEndUI] Tự động gán thành công sự kiện OnConfirmVictory cho nút Confirm!");
        }

        // Tự động tìm kiếm nút Return/Menu (dành cho màn hình Defeat)
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        Button returnBtn = null;
        foreach (var b in allButtons)
        {
            // Bỏ qua nút Confirm ở trên
            if (b == confirmButton) continue;
            
            if (b.name.Contains("Return") || b.name.Contains("Menu") || b.name.Contains("Defeat"))
            {
                returnBtn = b;
                break;
            }
        }

        if (returnBtn != null)
        {
            returnBtn.onClick.RemoveListener(OnReturnToMainMenu);
            returnBtn.onClick.AddListener(OnReturnToMainMenu);
            Debug.Log("[BattleEndUI] Tự động gán thành công sự kiện OnReturnToMainMenu cho nút " + returnBtn.name);
        }

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
        if (droppedItems == null) droppedItems = new List<LootDrop>();

        // 1. Loại bỏ các phần tử null trong danh sách itemSlots nếu có
        if (itemSlots != null)
        {
            itemSlots.RemoveAll(x => x == null);
        }

        // 2. Tự động tìm kiếm các ô InventorySlotUI dưới victoryPanel nếu chưa kéo vào Inspector
        if (itemSlots == null || itemSlots.Count == 0)
        {
            if (victoryPanel != null)
            {
                InventorySlotUI[] foundSlots = victoryPanel.GetComponentsInChildren<InventorySlotUI>(true);
                if (foundSlots != null && foundSlots.Length > 0)
                {
                    itemSlots = new List<InventorySlotUI>(foundSlots);
                }
                else
                {
                    // Tự động tìm container "ItemContainer" và gắn script InventorySlotUI cho các ô Slot 1, Slot 2, Slot 3
                    Transform container = victoryPanel.transform.Find("ItemContainer");
                    if (container == null) container = victoryPanel.transform;

                    itemSlots = new List<InventorySlotUI>();
                    foreach (Transform child in container)
                    {
                        if (child.name.StartsWith("Slot", StringComparison.OrdinalIgnoreCase))
                        {
                            InventorySlotUI slotScript = child.GetComponent<InventorySlotUI>();
                            if (slotScript == null)
                            {
                                slotScript = child.gameObject.AddComponent<InventorySlotUI>();
                            }
                            itemSlots.Add(slotScript);
                        }
                    }
                }
                Debug.Log($"[BattleEndUI] Tự động quét và gán thành công {itemSlots?.Count ?? 0} ô InventorySlotUI từ victoryPanel.");
            }
        }

        Debug.Log($"🎁 [LOOT DROP LOG] Tiến hành đổ {droppedItems.Count} vật phẩm vào {itemSlots?.Count ?? 0} ô Loot UI...");

        if (itemSlots == null || itemSlots.Count == 0)
        {
            Debug.LogWarning("[BattleEndUI] ⚠️ Không tìm thấy ô InventorySlotUI nào trong victoryPanel! Hãy kiểm tra Unity Inspector.");
            return;
        }

        for (int i = 0; i < itemSlots.Count; i++)
        {
            if (itemSlots[i] == null) continue;

            int slotNum = i + 1;
            itemSlots[i].onSlotClicked = (clickedSlot) =>
            {
                if (clickedSlot.hasItem && clickedSlot.itemData != null)
                {
                    Debug.Log($"🎯 [LOOT ITEM CLICKED] Người chơi xem thông tin chiến lợi phẩm: '{clickedSlot.itemData.itemName}' ở Slot {slotNum}!");
                }
            };

            if (i < droppedItems.Count && droppedItems[i] != null)
            {
                LootDrop drop = droppedItems[i];
                ItemData matchData = null;
                if (itemDatabase != null)
                {
                    matchData = itemDatabase.Find(x => x != null && 
                        !string.IsNullOrEmpty(x.itemName) &&
                        x.itemName.Equals(drop.itemId, StringComparison.OrdinalIgnoreCase));
                }

                if (matchData != null)
                {
                    itemSlots[i].AddItemToSlot(matchData, drop.quantity);
                    itemSlots[i].gameObject.SetActive(true);
                    Debug.Log($"🎁 [LOOT DROP LOG] Slot {slotNum}: Thêm thành công '{matchData.itemName}' (ID: {drop.itemId}) x{drop.quantity}");
                }
                else
                {
                    Debug.LogWarning($"[BattleEndUI] Chưa gán ItemData cho itemId '{drop.itemId}' trong itemDatabase Inspector. Đang tự động tạo dữ liệu tạm.");
                    ItemData fallbackData = new ItemData
                    {
                        itemName = string.IsNullOrEmpty(drop.itemId) ? "Loot Item" : drop.itemId,
                        itemType = ItemData.GetItemTypeFromId(drop.itemId)
                    };
                    itemSlots[i].AddItemToSlot(fallbackData, drop.quantity);
                    itemSlots[i].gameObject.SetActive(true);
                    Debug.Log($"🎁 [LOOT DROP LOG] Slot {slotNum}: Đã kích hoạt hiển thị tạm '{fallbackData.itemName}' x{drop.quantity}");
                }

                // Tự động Highlight tất cả các ô vật phẩm rớt ra (Chế độ Nhận Tất Cả / Take All Mode)
                itemSlots[i].SetSelected(true);
            }
            else
            {
                // Ẩn ô thừa nếu số lượng vật phẩm rớt ít hơn số lượng slot UI
                itemSlots[i].ClearSlot();
                itemSlots[i].gameObject.SetActive(false);
            }
        }
    }
    #endregion
}


