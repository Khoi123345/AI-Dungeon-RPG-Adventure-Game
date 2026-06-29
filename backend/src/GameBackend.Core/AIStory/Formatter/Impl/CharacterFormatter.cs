using System.Text;
using GameShared.Models;
using GameBackend.Core.AIStory.Formatters.Interfaces;

namespace GameBackend.Core.AIStory.Formatters;

public class CharacterFormatter : ICharacterFormatter
{
    public string Format(Character character)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Name: {character.name}");
        sb.AppendLine($"Level: {character.level}");
        sb.AppendLine($"HP: {character.hp}");
        sb.AppendLine($"Attack: {character.attack}");
        sb.AppendLine($"Defense: {character.defense}");
        sb.AppendLine($"Critical Rate: {character.criticalRate}");
        sb.AppendLine($"Lucky Rate: {character.luckyRate}");
        sb.AppendLine($"Gold: {character.gold}");

        return sb.ToString();
    }
}