using UnityEngine;
using UnityEngine.SceneManagement;
using GameShared.DTOs.Character;

/// <summary>
/// Presenter cho ProfileScene (standalone scene — Option B).
/// Lấy data từ GameProgressService, build ProfileData, và truyền sang ProfileView để render.
/// Xử lý điều hướng quay lại scene trước.
/// 
/// SETUP:
///   1. Tạo ProfileScene trong Unity (File > New Scene).
///   2. Tạo một GameObject "ProfilePresenter" trong Scene.
///   3. Attach script này lên GameObject đó.
///   4. Kéo ProfileView component vào field [view].
/// </summary>
public class ProfilePresenter : MonoBehaviour
{
    [SerializeField] private ProfileView view;

    /// <summary>
    /// Tên scene để quay lại khi nhấn Close.
    /// Mặc định là "StoryScene". Có thể override từ ProfileSceneLoader.
    /// </summary>
    private static string previousSceneName = "Menu";

    /// <summary>
    /// Gọi từ ProfileSceneLoader trước khi load ProfileScene
    /// để lưu tên scene hiện tại, dùng khi Close.
    /// </summary>
    public static void SetPreviousScene(string sceneName)
    {
        previousSceneName = sceneName;
    }

    private void Start()
    {
        if (view == null)
        {
            view = FindFirstObjectByType<ProfileView>();
            Debug.Log($"[ProfilePresenter] Tìm thấy ProfileView bằng FindFirstObjectByType: {view != null}");
        }
        else
        {
            Debug.Log($"[ProfilePresenter] ProfileView được gán sẵn trong Inspector: {view != null}");
        }

        // Đảm bảo GameProgressService tồn tại
        GameProgressService.EnsureInstance();

        // Build và render Profile data
        LoadAndRenderProfile();

        // Gắn sự kiện Close
        if (view != null)
        {
            Debug.Log("[ProfilePresenter] Đang gọi view.BindClose...");
            view.BindClose(OnCloseClicked);
        }
        else
        {
            Debug.LogError("[ProfilePresenter] Không tìm thấy ProfileView trong Scene!");
        }
    }

    private void LoadAndRenderProfile()
    {
        if (GameProgressService.Instance == null)
        {
            Debug.LogWarning("[ProfilePresenter] GameProgressService.Instance is null — dùng mock data offline.");
            RenderOfflineFallback();
            return;
        }

        ProfileCharacterData dto = GameProgressService.Instance.BuildProfileData();
        ProfileData profileData  = ProfileData.FromDTO(dto);

        if (view != null)
        {
            view.Render(profileData);
        }
    }

    /// <summary>
    /// Fallback khi không có GameProgressService (test độc lập scene).
    /// </summary>
    private void RenderOfflineFallback()
    {
        ProfileData fallback = new ProfileData
        {
            overview = new ProfileOverviewData
            {
                characterName        = "Dungeon Rider",
                className            = "Adventurer",
                level                = 7,
                experience           = 240,
                experienceToNextLevel = 700,
                hp    = 84, maxHp = 120,
                mp    = 30, maxMp = 40,
                gold  = 120,
                status    = "Alive",
                locationId = "ruins_gate"
            },
            stats = new ProfileStatsData
            {
                attack       = 18,
                defense      = 8,
                criticalRate = 0.12f,
                luckyRate    = 0.08f,
                speed        = 12f,
                evasionRate  = 0.07f,
                magicResist  = 8f
            },
            equipment = new ProfileEquipmentData
            {
                slots = new System.Collections.Generic.List<ProfileEquippedSlot>
                {
                    new ProfileEquippedSlot
                    {
                        slotType     = "Weapon",
                        isEmpty      = false,
                        itemName     = "Rusty Sword",
                        itemRarity   = "Common",
                        itemDescription = "Thanh kiếm cũ nhưng vẫn còn hữu dụng.",
                        attackBonus  = 4
                    },
                    new ProfileEquippedSlot { slotType = "Armor",     isEmpty = true },
                    new ProfileEquippedSlot { slotType = "Accessory", isEmpty = true },
                    new ProfileEquippedSlot { slotType = "Ring",      isEmpty = true },
                    new ProfileEquippedSlot { slotType = "Helmet",    isEmpty = true },
                    new ProfileEquippedSlot { slotType = "Boots",     isEmpty = true }
                }
            },
            titles = new ProfileTitlesData
            {
                titles = new System.Collections.Generic.List<ProfileTitleEntry>
                {
                    new ProfileTitleEntry
                    {
                        titleId     = "t1",
                        name        = "Kẻ Tiêu Diệt Bóng Tối",
                        description = "Hạ gục Shadow Demon lần đầu tiên",
                        rarity      = "Rare",
                        isEquipped  = true
                    },
                    new ProfileTitleEntry
                    {
                        titleId     = "t2",
                        name        = "Kẻ Lạc Đường",
                        description = "Đặt chân vào Ancient Ruins",
                        rarity      = "Common",
                        isEquipped  = false
                    }
                }
            },
            history = new ProfileHistoryData
            {
                records = new System.Collections.Generic.List<AdventureRecord>
                {
                    new AdventureRecord
                    {
                        encounterId   = "e1",
                        bossName      = "Shadow Demon",
                        bossRarity    = "Rare",
                        bossLevel     = 15,
                        result        = "Victory",
                        expGained     = 75,
                        goldGained    = 100,
                        turnCount     = 4,
                        encounterTime = System.DateTime.UtcNow.AddHours(-2)
                    },
                    new AdventureRecord
                    {
                        encounterId   = "e2",
                        bossName      = "Shadow Demon",
                        bossRarity    = "Rare",
                        bossLevel     = 15,
                        result        = "Defeat",
                        expGained     = 0,
                        goldGained    = 0,
                        turnCount     = 6,
                        encounterTime = System.DateTime.UtcNow.AddHours(-4)
                    }
                }
            }
        };

        if (view != null)
        {
            view.Render(fallback);
        }
    }

    private void OnCloseClicked()
    {
        Debug.Log($"[ProfilePresenter] Quay lại scene: {previousSceneName}");
        SceneManager.LoadScene(previousSceneName);
    }
}
