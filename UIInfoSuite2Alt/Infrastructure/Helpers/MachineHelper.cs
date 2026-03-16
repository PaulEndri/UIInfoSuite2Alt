using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Extensions;
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

  /// <summary>Try to get building chest names from item conversion rules.</summary>
  public static bool TryGetBuildingChestNames(
    BuildingData? data,
    out ISet<string> inputChests,
    out ISet<string> outputChests
  )
  {
    inputChests = new HashSet<string>();
    outputChests = new HashSet<string>();

    GetBuildingChestNames(data, inputChests, outputChests);

    return inputChests.Count > 0 || outputChests.Count > 0;
  }

  /// <summary>Get building chests matching the given chest names.</summary>
  public static IEnumerable<Chest> GetBuildingChests(Building building, ISet<string> chestNames)
  {
    foreach (Chest chest in building.buildingChests)
    {
      if (chestNames.Contains(chest.Name))
      {
        yield return chest;
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


  /// <summary>Get all items from a building's chests.</summary>
  public static List<Item?> GetBuildingChestItems(
    Building? building,
    BuildingChestType whichItems = BuildingChestType.Chest
  )
  {
    List<Item?> items = new();
    if (building is null)
    {
      return items;
    }

    HashSet<string> inputChests = new();
    HashSet<string> outputChests = new();
    GetBuildingChestNames(building.GetData(), inputChests, outputChests);

    HashSet<string> chestsToGlob = new();

    if (whichItems is BuildingChestType.Chest or BuildingChestType.Load)
    {
      chestsToGlob.AddRange(inputChests);
    }

    if (whichItems is BuildingChestType.Chest or BuildingChestType.Collect)
    {
      chestsToGlob.AddRange(outputChests);
    }

    foreach (string chestName in chestsToGlob)
    {
      Chest? buildingChest = building.GetBuildingChest(chestName);
      if (buildingChest is null)
      {
        continue;
      }

      items.AddRange(buildingChest.Items);
    }

    return items;
  }
}
