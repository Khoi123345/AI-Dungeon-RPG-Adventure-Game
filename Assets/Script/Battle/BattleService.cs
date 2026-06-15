using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattlePresenter : MonoBehaviour
{
    public BattleView view;
    
    // Tốc độ phát lại trận đấu (thời gian chờ giữa các lượt)
    public float turnDelay = 1.2f; 

    private void Start()
    {
        GameProgressService.EnsureInstance();

        // Khi Scene bắt đầu, lấy data từ runtime service mô phỏng CSDL.
        BattleData mockData = GameProgressService.Instance != null
            ? GameProgressService.Instance.CreateBattleDemoData()
            : CreateMockData();

        StartPlayback(mockData);
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

        if (GameProgressService.Instance != null)
        {
            GameProgressService.Instance.RecordBattleResult(data, data.isPlayerVictory);
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