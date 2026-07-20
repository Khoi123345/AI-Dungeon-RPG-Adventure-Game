using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameShared.DTOs.Character;

/// <summary>
/// Attach lên Prefab "HistoryEntryUI" trong tab Lịch Sử Phiêu Lưu.
/// Hiển thị kết quả 1 lần chạm trán boss.
/// 
/// PREFAB SETUP:
///   - txtBossName:   TextMeshProUGUI
///   - txtResult:     TextMeshProUGUI — "CHIẾN THẮNG" / "THẤT BẠI"
///   - txtReward:     TextMeshProUGUI — "EXP +75  Gold +100"
///   - txtDate:       TextMeshProUGUI
///   - txtTurnCount:  TextMeshProUGUI
///   - imgResultBadge: Image — màu xanh/đỏ theo kết quả
/// </summary>
public class HistoryEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtBossName;
    [SerializeField] private TextMeshProUGUI txtResult;
    [SerializeField] private TextMeshProUGUI txtReward;
    [SerializeField] private TextMeshProUGUI txtDate;
    [SerializeField] private TextMeshProUGUI txtTurnCount;
    [SerializeField] private Image imgResultBadge;
    [SerializeField] private Image entryBackground;

    private static readonly Color ColorVictory = new Color(0.2f, 0.8f, 0.4f);
    private static readonly Color ColorDefeat  = new Color(0.9f, 0.3f, 0.3f);

    public void SetRecord(AdventureRecord record)
    {
        if (record == null) return;

        bool isVictory = record.result == "Victory";

        if (txtBossName != null)
        {
            txtBossName.text = $"{record.bossName}  [Lv.{record.bossLevel} · {record.bossRarity}]";
        }

        if (txtResult != null)
        {
            txtResult.text  = isVictory ? "CHIẾN THẮNG" : "THẤT BẠI";
            txtResult.color = isVictory ? ColorVictory : ColorDefeat;
        }

        if (imgResultBadge != null)
        {
            imgResultBadge.color = isVictory ? ColorVictory : ColorDefeat;
        }

        if (txtReward != null)
        {
            txtReward.text = isVictory
                ? $"EXP +{record.expGained}   Gold +{record.goldGained}"
                : "—";
        }

        if (txtTurnCount != null)
        {
            txtTurnCount.text = $"{record.turnCount} lượt";
        }

        if (txtDate != null)
        {
            txtDate.text = record.encounterTime == default
                ? string.Empty
                : record.encounterTime.ToLocalTime().ToString("HH:mm  dd/MM/yyyy");
        }

        // Tô nền theo kết quả
        if (entryBackground != null)
        {
            Color baseColor = isVictory ? ColorVictory : ColorDefeat;
            entryBackground.color = baseColor * 0.15f + new Color(0.05f, 0.05f, 0.08f) * 0.85f;
        }
    }
}
