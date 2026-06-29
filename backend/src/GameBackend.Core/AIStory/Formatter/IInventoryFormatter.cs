using System.Collections.Generic;
using GameShared.Models;

namespace GameBackend.Core.AIStory.Formatters.Interfaces
{
	public interface IInventoryFormatter
	{
		string Format(IEnumerable<Item> items);
	}
}
