using UnityEngine;
using System.Collections.Generic;
using System;



[Serializable]
public class BattleTurn
{
    public string logMessage;     // VD: "Người chơi chém trúng đích!"
    public int playerHPRemaining; // Máu người chơi sau lượt này
    public int bossHPRemaining;   // Máu Boss sau lượt này
    public bool isCritical;       // (Tùy chọn) Để View biết đường bật hiệu ứng rung màn hình
}

[Serializable]
public class BattleData
{
    public FighterStats player;
    public FighterStats boss;
    public List<BattleTurn> turns; // Danh sách toàn bộ diễn biến trận đấu
    public bool isPlayerVictory;   // Kết quả cuối cùng
}
