using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameShared.Models; // Sử dụng LootDrop từ Models

public class BattlePresenter : MonoBehaviour
{
    public BattleView view;
    
    [Header("UI kết thúc trận đấu")]
    [SerializeField] private BattleEndUIController endUIController; // Kéo thả BattleEndUIController từ Scene
    
    // Tốc độ phát lại trận đấu (thời gian chờ giữa các lượt)
    public float turnDelay = 1.2f; 

    private List<LootDrop> apiDroppedItems = new List<LootDrop>();

    private async void Start()
    {
        GameProgressService.EnsureInstance();

        GameConfigSO config = Resources.Load<GameConfigSO>("GameConfig");
        bool isMock = config != null && config.useMockMode;

        if (isMock)
        {
            Debug.Log("[BattleService] Đang chạy chế độ Mock Mode...");
            BattleData mockData = GameProgressService.Instance != null
                ? GameProgressService.Instance.CreateBattleDemoData()
                : CreateMockData();
            StartPlayback(mockData);
        }
        else
        {
            // Đảm bảo ApiClient có token trước khi gọi bất kỳ API nào.
            // Kiểm tra ApiClient.IsAuthenticated thay vì AuthManager.IsLoggedIn
            // vì AuthManager có thể bị race condition khi Start() chạy song song.
            ApiClient.EnsureInstance();
            if (!ApiClient.Instance.IsAuthenticated)
            {
                Debug.Log("[BattleService] ApiClient chưa có token, đang khôi phục từ PlayerPrefs...");
                bool restored = await new RealAuthService().TryRestoreSessionAsync();
                if (!restored)
                {
                    Debug.LogWarning("[BattleService] Không khôi phục được session. Chuyển về Mock Mode.");
                    StartPlayback(GameProgressService.Instance != null
                        ? GameProgressService.Instance.CreateBattleDemoData()
                        : CreateMockData());
                    return;
                }
                Debug.Log("[BattleService] Token đã được nạp vào ApiClient thành công.");
            }
            else
            {
                Debug.Log("[BattleService] ApiClient đã có token hợp lệ.");
            }

            Debug.Log("[BattleService] Đang gọi API lấy dữ liệu Battle...");
            await LoadRealBattleDataAsync();
        }
    }


    private async System.Threading.Tasks.Task LoadRealBattleDataAsync()
    {
        string charId = "mock-id";
        string sessionId = "mock-session";
        if (GameProgressService.Instance != null && GameProgressService.Instance.CurrentCharacter != null)
        {
            charId = GameProgressService.Instance.CurrentCharacter.characterId;
            if (GameProgressService.Instance.CurrentStorySession != null)
            {
                sessionId = GameProgressService.Instance.CurrentStorySession.sessionId;
            }
        }

        // Nếu charId chưa là ID thật trên DynamoDB, tự động tạo nhân vật mặc định trên AWS
        if ((charId == "mock-id" || string.IsNullOrEmpty(charId)) && GameConfigSO.Instance != null && !GameConfigSO.Instance.useMockMode)
        {
            try
            {
                var charApi = new CharacterApiService();
                string userId = GameProgressService.Instance?.CurrentUser?.userId ?? "default_user";
                string displayName = GameProgressService.Instance?.CurrentUser?.displayName ?? "Default Hero";
                string charJson = await charApi.CreateCharacterAsync(userId, displayName, "Adventurer");
                if (!string.IsNullOrEmpty(charJson))
                {
                    var container = JsonUtility.FromJson<BattleResponseContainer<GameShared.DTOs.Character.CharacterResponse>>(charJson);
                    if (container != null && container.success && container.data != null && !string.IsNullOrEmpty(container.data.characterId))
                    {
                        var model = MapResponseToModel(container.data);
                        charId = model.characterId;
                        GameProgressService.Instance?.SetCurrentCharacter(model);
                        Debug.Log($"[BattleService] Tự động tạo nhân vật mặc định trên AWS DynamoDB thành công: {model.name} (id={charId})");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BattleService] Không thể tạo tự động nhân vật mặc định trên AWS: {ex.Message}");
            }
        }

        var spawnReq = new GameShared.DTOs.Battle.BossSpawnRequest { characterId = charId, sessionId = sessionId };
        var spawnRes = await ApiClient.Instance.PostAsync<GameShared.DTOs.Battle.BossSpawnResponse>("battle/spawn-boss", spawnReq);

        if (spawnRes == null || string.IsNullOrEmpty(spawnRes.encounterId))
        {
            Debug.LogError("[BattleService] Lỗi API Spawn Boss (Không tìm thấy Boss/Character hoặc payload lỗi). Tự động chuyển về Mock Mode.");
            StartPlayback(CreateMockData());
            return;
        }

        Debug.Log($"[BOSS SPAWN LOG]\n" +
                  $"👹 Boss Name: {spawnRes.bossName} (Lv.{spawnRes.bossLevel}, Rarity: {spawnRes.bossRarity})\n" +
                  $"❤️ Boss HP: {spawnRes.bossHp} | ⚔️ Attack: {spawnRes.bossAttack} | 🛡️ Defense: {spawnRes.bossDefense}");

        var resolveReq = new GameShared.DTOs.Battle.BattleResolveRequest { characterId = charId, encounterId = spawnRes.encounterId };
        var resolveRes = await ApiClient.Instance.PostAsync<GameShared.DTOs.Battle.BattleResolveResponse>("battle/resolve", resolveReq);

        if (resolveRes == null)
        {
            Debug.LogError("[BattleService] Lỗi API Resolve Battle. Tự động chuyển về Mock Mode.");
            StartPlayback(CreateMockData());
            return;
        }

        string luckyText = (resolveRes.luckyEffects != null && resolveRes.luckyEffects.Count > 0)
            ? string.Join(", ", resolveRes.luckyEffects)
            : "None";

        Debug.Log($"[BATTLE RESULT STATS LOG]\n" +
                  $"⚔️ Player Power (Sức mạnh người chơi): {resolveRes.playerPower:F1}\n" +
                  $"👹 Boss Power (Sức mạnh Trùm): {resolveRes.bossPower:F1}\n" +
                  $"🍀 Lucky Effects (Hiệu ứng may mắn): {luckyText}\n" +
                  $"📊 Battle Score (Điểm kết quả = PP - BP + Lucky): {resolveRes.battleScore:F1}\n" +
                  $"🏆 Kết quả trận đấu: {(resolveRes.isPlayerVictory ? "CHIẾN THẮNG 🎉" : "THẤT BẠI 💀")}");

        // Chuyển đổi dữ liệu Backend về định dạng UI
        BattleData realData = new BattleData();
        realData.isPlayerVictory = resolveRes.isPlayerVictory;

        realData.player = new FighterStats {
            name = GameProgressService.Instance?.CurrentCharacter?.name ?? "Player",
            level = GameProgressService.Instance?.CurrentCharacter?.level ?? 1,
            maxHP = GameProgressService.Instance?.CurrentCharacter?.maxHp ?? 100,
            currentHP = GameProgressService.Instance?.CurrentCharacter?.hp ?? 100
        };

        realData.boss = new FighterStats {
            name = spawnRes.bossName,
            level = spawnRes.bossLevel,
            maxHP = spawnRes.bossHp,
            currentHP = spawnRes.bossHp
        };

        realData.turns = new List<BattleTurn>();
        foreach (var t in resolveRes.turns)
        {
            realData.turns.Add(new BattleTurn {
                logMessage = t.logMessage,
                playerHPRemaining = t.playerHpRemaining,
                bossHPRemaining = t.bossHpRemaining,
                isCritical = t.isCritical
            });
        }

        apiDroppedItems.Clear();
        if (resolveRes.rewards != null && resolveRes.rewards.lootItems != null)
        {
            foreach (var loot in resolveRes.rewards.lootItems)
            {
                apiDroppedItems.Add(new LootDrop {
                    itemId = loot.itemId,
                    quantity = loot.quantity,
                    battleId = resolveRes.battleId
                });
            }
        }

        StartPlayback(realData);
    }

    // Bắt đầu luồng hiển thị
    public void StartPlayback(BattleData data)
    {
        // 1. Khởi tạo giao diện ban đầu
        view.SetupFighters(data.player, data.boss);

        // 2. Chạy Coroutine để từ từ đọc các lượt đánh
        StartCoroutine(PlaybackRoutine(data));
    }

    // Hàm Queue/Playback chạy ngầm
    private IEnumerator PlaybackRoutine(BattleData data)
    {
        // Đợi một chút cho người chơi nhìn rõ UI trước khi đánh
        yield return new WaitForSeconds(1.0f); 

        // Duyệt qua từng lượt đánh trong danh sách
        foreach (BattleTurn turn in data.turns)
        {
            // Cập nhật Log
            view.AppendLog(turn.logMessage);

            // Cập nhật Máu
            view.UpdateHP(true, turn.playerHPRemaining, data.player.maxHP);
            view.UpdateHP(false, turn.bossHPRemaining, data.boss.maxHP);

            // Đợi X giây rồi mới chạy lượt tiếp theo
            yield return new WaitForSeconds(turnDelay);
        }

        // Đợi thêm chút rồi hiện kết quả
        yield return new WaitForSeconds(0.5f);
        view.ShowResult(data.isPlayerVictory);

        List<LootDrop> droppedItems = new List<LootDrop>();
        GameConfigSO config = Resources.Load<GameConfigSO>("GameConfig");
        bool isMock = config != null && config.useMockMode;

        if (isMock)
        {
            if (GameProgressService.Instance != null)
            {
                droppedItems = GameProgressService.Instance.RecordBattleResult(data, data.isPlayerVictory);
            }
            else if (data.isPlayerVictory)
            {
                droppedItems.Add(new LootDrop { itemId = "Rusty Sword", quantity = 1 });
            }
        }
        else
        {
            // Nếu là Online Mode, lấy phần thưởng trực tiếp từ API đã lưu lúc tải trận đấu
            droppedItems = this.apiDroppedItems;
        }

        // Kích hoạt giao diện kết quả trận đấu sau khi log kết thúc
        if (endUIController != null)
        {
            if (data.isPlayerVictory)
            {
                endUIController.TriggerVictory(droppedItems);
            }
            else
            {
                endUIController.TriggerDefeat();
            }
        }
    }

    // --- HÀM TẠO DỮ LIỆU GIẢ ĐỂ TEST UI TRƯỚC KHI CÓ BACKEND ---
    private BattleData CreateMockData()
    {
        BattleData mock = new BattleData();
        
        mock.player = new FighterStats { name = "Hiệp sĩ", level = 10, maxHP = 100, currentHP = 100 };
        mock.boss = new FighterStats { name = "Shadow Demon", level = 45, maxHP = 200, currentHP = 200 };
        mock.isPlayerVictory = true;

        mock.turns = new List<BattleTurn>
        {
            new BattleTurn { logMessage = "Hiệp sĩ chém Demon 50 sát thương!", playerHPRemaining = 100, bossHPRemaining = 150 },
            new BattleTurn { logMessage = "Demon phun lửa đáp trả (30 DMG)!", playerHPRemaining = 70, bossHPRemaining = 150 },
            new BattleTurn { logMessage = "Hiệp sĩ dùng kỹ năng chém đôi (100 DMG)!", playerHPRemaining = 70, bossHPRemaining = 50 },
            new BattleTurn { logMessage = "Demon tung đòn hiểm (60 DMG)!", playerHPRemaining = 10, bossHPRemaining = 50 },
            new BattleTurn { logMessage = "Hiệp sĩ tung đòn chí mạng kết liễu!", playerHPRemaining = 10, bossHPRemaining = 0 }
        };

        return mock;
    }

    private GameShared.Models.Character MapResponseToModel(GameShared.DTOs.Character.CharacterResponse res)
    {
        if (res == null) return null;
        return new GameShared.Models.Character
        {
            characterId = res.characterId,
            name = res.name,
            level = res.level,
            experience = res.experience,
            hp = res.hp,
            maxHp = res.maxHp,
            attack = res.attack,
            defense = res.defense,
            criticalRate = res.criticalRate,
            luckyRate = res.luckyRate,
            gold = res.gold,
            className = res.className,
            status = res.status,
            currentLocationId = res.currentLocationId
        };
    }

    [System.Serializable]
    private class BattleResponseContainer<T> where T : class
    {
        public bool success;
        public string message;
        public string errorCode;
        public T data;
    }
}