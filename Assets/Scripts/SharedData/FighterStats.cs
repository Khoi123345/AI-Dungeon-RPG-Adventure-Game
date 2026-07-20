using System;

/// <summary>
/// Thông số chiến đấu của 1 fighter (player hoặc boss).
/// Dùng chung cho cả BattleSystem lẫn UI hiển thị.
/// File này nằm trong SharedData vì được dùng xuyên suốt game.
/// </summary>
[Serializable]
public class FighterStats
{
    public string name;
    public int level;
    public int maxHP;
    public int currentHP;
    public int attack;
    public int defense;
    public float criticalRate;
    public float luckyRate;
}
