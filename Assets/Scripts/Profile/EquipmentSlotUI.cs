using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameShared.DTOs.Character;

/// <summary>
/// Attach lên Prefab "EquipmentSlotUI" trong ProfileScene.
/// Hiển thị thông tin 1 slot trang bị (Weapon, Armor, v.v.)
/// 
/// PREFAB SETUP:
///   - Root: Image (background slot frame)
///   - txtSlotType: TextMeshProUGUI — tên slot (VD: "WEAPON")
///   - txtItemName: TextMeshProUGUI — tên item / "[Trống]"
///   - txtRarity:   TextMeshProUGUI — độ hiếm (màu theo rarity)
///   - txtStats:    TextMeshProUGUI — ATK+4 / DEF+0 v.v.
///   - panelEmpty:  GameObject — hiện khi slot trống
/// </summary>
public class EquipmentSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtSlotType;
    [SerializeField] private TextMeshProUGUI txtItemName;
    [SerializeField] private TextMeshProUGUI txtRarity;
    [SerializeField] private TextMeshProUGUI txtStats;
    [SerializeField] private GameObject panelEmpty;
    [SerializeField] private Image slotBackground;

    public void SetSlot(ProfileEquippedSlot slot)
    {
        if (slot == null) return;

        if (txtSlotType != null)
        {
            txtSlotType.text = slot.slotType.ToUpper();
        }

        if (slot.isEmpty)
        {
            SetEmpty();
            return;
        }

        if (panelEmpty != null) panelEmpty.SetActive(false);

        if (txtItemName != null) txtItemName.text = slot.itemName;

        if (txtRarity != null)
        {
            txtRarity.text  = slot.itemRarity;
            txtRarity.color = GetRarityColor(slot.itemRarity);
        }

        if (txtStats != null)
        {
            string statLine = BuildStatLine(slot);
            txtStats.text = statLine;
        }

        if (slotBackground != null)
        {
            slotBackground.color = GetRarityColor(slot.itemRarity) * 0.25f + Color.black * 0.75f;
        }
    }

    private void SetEmpty()
    {
        if (panelEmpty != null) panelEmpty.SetActive(true);
        if (txtItemName != null) txtItemName.text = "[Trống]";
        if (txtRarity   != null) { txtRarity.text = string.Empty; }
        if (txtStats    != null) { txtStats.text  = string.Empty; }
        if (slotBackground != null) slotBackground.color = new Color(0.15f, 0.15f, 0.18f);
    }

    private static string BuildStatLine(ProfileEquippedSlot slot)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        if (slot.attackBonus    != 0) sb.Append($"ATK +{slot.attackBonus}  ");
        if (slot.defenseBonus   != 0) sb.Append($"DEF +{slot.defenseBonus}  ");
        if (slot.hpBonus        != 0) sb.Append($"HP +{slot.hpBonus}  ");
        if (slot.criticalBonus  > 0f) sb.Append($"CRIT +{slot.criticalBonus * 100f:F1}%");
        return sb.Length > 0 ? sb.ToString().Trim() : "—";
    }

    private static Color GetRarityColor(string rarity)
    {
        return rarity switch
        {
            "Common"    => Color.white,
            "Uncommon"  => new Color(0.3f, 1f, 0.3f),
            "Rare"      => new Color(0.3f, 0.6f, 1f),
            "Epic"      => new Color(0.7f, 0.3f, 1f),
            "Legendary" => new Color(1f, 0.6f, 0f),
            _           => Color.white
        };
    }
}
