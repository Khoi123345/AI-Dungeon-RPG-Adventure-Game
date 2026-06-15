using UnityEngine;
using TMPro; // Thư viện TextMeshPro để hiển thị text đẹp hơn
using UnityEngine.UI; // Thư viện UI để sử dụng Image, Button, v.v.

public class BattleView : MonoBehaviour
{
    [Header("Player UI")]
    public TextMeshProUGUI txtPlayerName;
    public TextMeshProUGUI txtPlayerHP;
    public Image imgPlayerHPFill; // Thanh máu (Image Type: Filled)

    [Header("Boss UI")]
    public TextMeshProUGUI txtBossName;
    public TextMeshProUGUI txtBossHP;
    public Image imgBossHPFill;

    [Header("Battle Log")]
    public TextMeshProUGUI txtBattleLog;

    // Khởi tạo thông tin ban đầu
    public void SetupFighters(FighterStats player, FighterStats boss)
    {
        txtPlayerName.text = $"Lvl {player.level} {player.name}";
        txtBossName.text = $"Lvl {boss.level} {boss.name}";
        
        UpdateHP(true, player.currentHP, player.maxHP);
        UpdateHP(false, boss.currentHP, boss.maxHP);
        
        txtBattleLog.text = "Trận chiến bắt đầu!\n";
    }

    // Cập nhật thanh máu
    public void UpdateHP(bool isPlayer, int currentHP, int maxHP)
    {
        float fillAmount = (float)currentHP / maxHP;
        string hpText = $"{currentHP}/{maxHP}";

        if (isPlayer)
        {
            txtPlayerHP.text = hpText;
            imgPlayerHPFill.fillAmount = fillAmount;
        }
        else
        {
            txtBossHP.text = hpText;
            imgBossHPFill.fillAmount = fillAmount;
        }
    }

    // Thêm dòng log mới
    public void AppendLog(string message)
    {
        txtBattleLog.text += $"- {message}\n";
    }

    // Hiển thị kết quả (Tạm thời in ra Log, sau này gọi Panel Popup lên)
    public void ShowResult(bool isVictory)
    {
        if(isVictory)
            AppendLog("\n<color=green>VICTORY!</color>");
        else
            AppendLog("\n<color=red>DEFEAT...</color>");
    }
}
