using System.Collections.Generic;
using UIInfoSuite2Alt.Compatibility;
using UIInfoSuite2Alt.Infrastructure.Helpers;

namespace UIInfoSuite2Alt.Infrastructure.Extensions;

internal static class CustomBushExtensions
{
  public static List<PossibleDroppedItem> GetCustomBushDropItems(
    this ICustomBushApi api,
    ICustomBushData bush,
    string? id,
    bool includeToday = false
  )
  {
    if (string.IsNullOrEmpty(id))
    {
      return [];
    }

    api.TryGetDrops(id, out IList<ICustomBushDrop>? drops);
    return drops == null
      ? []
      : DropsHelper.GetGenericDropItems(
        drops,
        id,
        includeToday,
        bush.DisplayName,
        BushDropConverter
      );

    DropInfo BushDropConverter(ICustomBushDrop input)
    {
      return new DropInfo(input.Condition, input.Chance, input.ItemId);
    }
  }
}
