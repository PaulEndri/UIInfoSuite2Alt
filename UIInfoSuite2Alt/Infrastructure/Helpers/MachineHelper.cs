using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using StardewValley.Objects;

namespace UIInfoSuite2Alt.Infrastructure.Helpers;

public static class MachineHelper
{
  // Adapted from Pathoschild/StardewMods: https://github.com/Pathoschild/StardewMods

  /// <summary>Get building chest names referenced by item conversion rules.</summary>
  public static void GetBuildingChestNames(BuildingData? data, ISet<string> inputChests, ISet<string> outputChests)
  {
    if (data?.ItemConversions?.Count is not > 0)
    {
      return;
    }

    foreach (BuildingItemConversion? rule in data.ItemConversions)
    {
      if (rule?.SourceChest is not null)
      {
        inputChests.Add(rule.SourceChest);
      }

      if (rule?.DestinationChest is not null)
      {
        outputChests.Add(rule.DestinationChest);
      }
    }
  }

  public static void GetBuildingChestItems(Building? building, List<Item?> inputItems, List<Item?> outputItems)
  {
    if (building is null)
    {
      return;
    }

    HashSet<string> inputChestNames = new();
    HashSet<string> outputChestNames = new();
    GetBuildingChestNames(building.GetData(), inputChestNames, outputChestNames);

    IEnumerable<Chest> inputChests = inputChestNames.Select(building.GetBuildingChest)
                                                    .Where(chest => chest is not null);
    IEnumerable<Chest> outputChests =
      outputChestNames.Select(building.GetBuildingChest).Where(chest => chest is not null);

    foreach (Chest chest in inputChests)
    {
      inputItems.AddRange(chest.Items);
    }

    foreach (Chest chest in outputChests)
    {
      outputItems.AddRange(chest.Items);
    }
  }
}
