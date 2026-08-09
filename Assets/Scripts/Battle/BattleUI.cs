using UnityEngine;
using TMPro; // Thư viện TextMeshPro để hiển thị text đẹp hơn
using UnityEngine.UI; // Thư viện UI để sử dụng Image, Button, v.v.

public class BattleView : MonoBehaviour
{
    [Header("Player UI")]
    public TextMeshProUGUI txtPlayerName;
    public TextMeshProUGUI txtPlayerHP;
    public Image imgPlayerHPFill; // Thanh máu (Image Type: Filled)

    [Header("Player Stats Details")]
    public TextMeshProUGUI txtPlayerAttack;
    public TextMeshProUGUI txtPlayerDefense;
    public TextMeshProUGUI txtPlayerLucky;

    [Header("Boss UI")]
    public TextMeshProUGUI txtBossName;
    public TextMeshProUGUI txtBossHP;
    public Image imgBossHPFill;

    [Header("Boss Stats Details")]
    public TextMeshProUGUI txtBossAttack;
    public TextMeshProUGUI txtBossDefense;
    public TextMeshProUGUI txtBossLucky;

    [Header("Battle Log")]
    public TextMeshProUGUI txtBattleLog;
    public UnityEngine.UI.ScrollRect logScrollRect;

    private void Awake()
    {
        AutoFindStatsIfNeeded();
    }

    private void AutoFindStatsIfNeeded()
    {
        // 1. Tự động tìm Panel_PlayerStats nếu chưa được kéo vào Inspector
        Transform playerPanel = transform.Find("Panel_Visualization/Panel_PlayerStats");
        if (playerPanel == null) playerPanel = transform.Find("Panel_PlayerStats");
        if (playerPanel == null && transform.parent != null) playerPanel = transform.parent.Find("Panel_Visualization/Panel_PlayerStats");
        if (playerPanel != null)
        {
            if (txtPlayerAttack == null) { var t = playerPanel.Find("txt_Attack"); if (t != null) txtPlayerAttack = t.GetComponent<TextMeshProUGUI>(); }
            if (txtPlayerDefense == null) { var t = playerPanel.Find("txt_Defense"); if (t != null) txtPlayerDefense = t.GetComponent<TextMeshProUGUI>(); }
            if (txtPlayerLucky == null) { var t = playerPanel.Find("txt_Lucky"); if (t != null) txtPlayerLucky = t.GetComponent<TextMeshProUGUI>(); }
        }

        // 2. Tự động tìm Panel_BossStats nếu chưa được kéo vào Inspector
        Transform bossPanel = transform.Find("Panel_Visualization/Panel_BossStats");
        if (bossPanel == null) bossPanel = transform.Find("Panel_BossStats");
        if (bossPanel == null && transform.parent != null) bossPanel = transform.parent.Find("Panel_Visualization/Panel_BossStats");
        if (bossPanel != null)
        {
            if (txtBossAttack == null) { var t = bossPanel.Find("txt_Attack"); if (t != null) txtBossAttack = t.GetComponent<TextMeshProUGUI>(); }
            if (txtBossDefense == null) { var t = bossPanel.Find("txt_Defense"); if (t != null) txtBossDefense = t.GetComponent<TextMeshProUGUI>(); }
            if (txtBossLucky == null) { var t = bossPanel.Find("txt_Lucky"); if (t != null) txtBossLucky = t.GetComponent<TextMeshProUGUI>(); }
        }
    }

    // Khởi tạo thông tin ban đầu
    public void SetupFighters(FighterStats player, FighterStats boss)
    {
        AutoFindStatsIfNeeded();

        if (txtPlayerName != null) txtPlayerName.text = $"Lvl {player.level} {player.name}";
        if (txtBossName != null) txtBossName.text = $"Lvl {boss.level} {boss.name}";
        
        // Hiển thị chỉ số chi tiết của Người chơi
        if (txtPlayerAttack != null) txtPlayerAttack.text = $"⚔️ Atk: {player.attack}";
        if (txtPlayerDefense != null) txtPlayerDefense.text = $"🛡️ Def: {player.defense}";
        if (txtPlayerLucky != null) txtPlayerLucky.text = $"🍀 Lucky: Normal";

        // Hiển thị chỉ số chi tiết của Boss
        if (txtBossAttack != null) txtBossAttack.text = $"⚔️ Atk: {boss.attack}";
        if (txtBossDefense != null) txtBossDefense.text = $"🛡️ Def: {boss.defense}";
        if (txtBossLucky != null) txtBossLucky.text = $"🍀 Crit: {(boss.criticalRate * 100):F0}%";

        UpdateHP(true, player.currentHP, player.maxHP);
        UpdateHP(false, boss.currentHP, boss.maxHP);
        
        if (txtBattleLog != null) txtBattleLog.text = "Trận chiến bắt đầu!\n";
    }

    // Cập nhật thanh máu
    public void UpdateHP(bool isPlayer, int currentHP, int maxHP)
    {
        float fillAmount = (float)currentHP / maxHP;
        string hpText = $"{currentHP}/{maxHP}";

        if (isPlayer)
        {
            if (txtPlayerHP != null) txtPlayerHP.text = hpText;
            if (imgPlayerHPFill != null) imgPlayerHPFill.fillAmount = fillAmount;
        }
        else
        {
            if (txtBossHP != null) txtBossHP.text = hpText;
            if (imgBossHPFill != null) imgBossHPFill.fillAmount = fillAmount;
        }
    }

    // Thêm dòng log mới
    public void AppendLog(string message)
    {
        if (txtBattleLog != null) 
        {
            txtBattleLog.text += $"- {message}\n";
            
            // Ép Unity tính toán lại layout ngay lập tức
            Canvas.ForceUpdateCanvases();
            
            // Tự động kéo thanh cuộn xuống vị trí dưới cùng (0f)
            if (logScrollRect != null)
            {
                logScrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }

    // Hiển thị kết quả
    public void ShowResult(bool isVictory)
    {
        if (isVictory)
            AppendLog("\n<color=green>VICTORY!</color>");
        else
            AppendLog("\n<color=red>DEFEAT...</color>");
    }
}
