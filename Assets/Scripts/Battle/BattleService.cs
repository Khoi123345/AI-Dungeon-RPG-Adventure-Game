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

        var spawnReq = new GameShared.DTOs.Battle.BossSpawnRequest { characterId = charId, sessionId = sessionId };
        var spawnRes = await ApiClient.Instance.PostAsync<GameShared.DTOs.Battle.BossSpawnResponse>("battle/spawn-boss", spawnReq);

        if (spawnRes == null)
        {
            Debug.LogError("[BattleService] Lỗi API Spawn Boss. Tự động chuyển về Mock Mode.");
            StartPlayback(CreateMockData());
            return;
        }

        var resolveReq = new GameShared.DTOs.Battle.BattleResolveRequest { characterId = charId, encounterId = spawnRes.encounterId };
        var resolveRes = await ApiClient.Instance.PostAsync<GameShared.DTOs.Battle.BattleResolveResponse>("battle/resolve", resolveReq);

        if (resolveRes == null)
        {
            Debug.LogError("[BattleService] Lỗi API Resolve Battle. Tự động chuyển về Mock Mode.");
            StartPlayback(CreateMockData());
            return;
        }

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
}