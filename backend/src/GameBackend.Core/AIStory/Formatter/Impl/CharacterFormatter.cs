using System.Text;
using GameShared.Models;
using GameBackend.Core.AIStory.Formatters.Interfaces;

namespace GameBackend.Core.AIStory.Formatters;

public class CharacterFormatter : ICharacterFormatter
{
    public string Format(Character character)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Name: {character.Name}");
        sb.AppendLine($"Level: {character.Level}");
        sb.AppendLine($"HP: {character.HP}");
        sb.AppendLine($"Attack: {character.Attack}");
        sb.AppendLine($"Defense: {character.Defense}");
        sb.AppendLine($"Critical Rate: {character.CriticalRate}");
        sb.AppendLine($"Lucky Rate: {character.LuckyRate}");
        sb.AppendLine($"Gold: {character.Gold}");

        return sb.ToString();
    }
}