using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameShared.DTOs.Character;

/// <summary>
/// Attach lên Prefab "TitleEntryUI" trong tab Danh Hiệu.
/// Hiển thị 1 danh hiệu: tên, mô tả, rarity badge, và trạng thái trang bị.
/// 
/// PREFAB SETUP:
///   - txtTitleName:    TextMeshProUGUI
///   - txtDescription:  TextMeshProUGUI
///   - txtRarity:       TextMeshProUGUI (badge)
///   - iconEquipped:    GameObject (hiện nếu isEquipped = true)
///   - txtEarnedDate:   TextMeshProUGUI
/// </summary>
public class TitleEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtTitleName;
    [SerializeField] private TextMeshProUGUI txtDescription;
    [SerializeField] private TextMeshProUGUI txtRarity;
    [SerializeField] private TextMeshProUGUI txtEarnedDate;
    [SerializeField] private GameObject iconEquipped;
    [SerializeField] private Image entryBackground;

    public void SetEntry(ProfileTitleEntry entry)
    {
        if (entry == null) return;

        if (txtTitleName  != null) txtTitleName.text  = entry.name;
        if (txtDescription != null) txtDescription.text = entry.description;

        if (txtRarity != null)
        {
            txtRarity.text  = entry.rarity;
            txtRarity.color = GetRarityColor(entry.rarity);
        }

        if (txtEarnedDate != null)
        {
            txtEarnedDate.text = entry.earnedAt == default
                ? string.Empty
                : $"Đạt được: {entry.earnedAt.ToLocalTime():dd/MM/yyyy}";
        }

        if (iconEquipped != null) iconEquipped.SetActive(entry.isEquipped);

        // Tô nền nhẹ theo rarity
        if (entryBackground != null)
        {
            Color rarityColor = GetRarityColor(entry.rarity);
            entryBackground.color = rarityColor * 0.15f + new Color(0.08f, 0.08f, 0.12f) * 0.85f;
        }
    }

    private static Color GetRarityColor(string rarity)
    {
        return rarity switch
        {
            "Common"    => Color.white,
            "Rare"      => new Color(0.3f, 0.6f, 1f),
            "Epic"      => new Color(0.7f, 0.3f, 1f),
            "Legendary" => new Color(1f, 0.6f, 0f),
            _           => Color.white
        };
    }
}
