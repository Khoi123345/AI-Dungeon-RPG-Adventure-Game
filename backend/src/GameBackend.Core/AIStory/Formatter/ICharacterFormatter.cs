using GameShared.Models;

namespace GameBackend.Core.AIStory.Formatters.Interfaces
{
	public interface ICharacterFormatter
	{
		string Format(Character character);
	}
}
