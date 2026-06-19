using System.Collections.Generic;
using GameShared.Models;

namespace GameBackend.Core.AIStory.Formatters.Interfaces
{
	public interface IRecentTurnsFormatter
	{
		string Format(IEnumerable<StoryAction> actions);
	}
}
