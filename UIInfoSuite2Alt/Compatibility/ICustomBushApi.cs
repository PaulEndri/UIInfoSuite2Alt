using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using StardewValley;
using StardewValley.GameData;
using StardewValley.TerrainFeatures;
using UIInfoSuite2Alt.Infrastructure.Helpers;

namespace UIInfoSuite2Alt.Compatibility;

/// <summary>Mod API for Custom Bush (v1.5.2+).</summary>
public interface ICustomBushApi
{
  /// <summary>Determines if the given Bush instance is a custom bush.</summary>
  public bool IsCustomBush(Bush bush);

  /// <summary>Determines if the given Bush instance is a custom bush and in season.</summary>
  public bool IsInSeason(Bush bush);

  /// <summary>Tries to get the custom bush model associated with the given bush.</summary>
  public bool TryGetBush(
    Bush bush,
    [NotNullWhen(true)] out ICustomBushData? customBush,
    [NotNullWhen(true)] out string? id
  );

  /// <summary>Tries to get the custom bush drop associated with the given bush id.</summary>
  public bool TryGetDrops(string id, [NotNullWhen(true)] out IList<ICustomBushDrop>? drops);

  /// <summary>Tries to get the shake off item for the given bush.</summary>
  public bool TryGetShakeOffItem(Bush bush, [NotNullWhen(true)] out Item? item);
}

/// <summary>Model used for custom bushes (v1.5.2+).</summary>
public interface ICustomBushData
{
  /// <summary>Gets a unique identifier for the custom bush.</summary>
  public string Id { get; }

  /// <summary>Gets a list of conditions where any have to match for the bush to produce items.</summary>
  public List<string> ConditionsToProduce { get; }

  /// <summary>Gets the age needed to produce.</summary>
  public int AgeToProduce { get; }

  /// <summary>Gets the day of month to begin producing.</summary>
  public int DayToBeginProducing { get; }

  /// <summary>Gets the description of the bush.</summary>
  public string Description { get; }

  /// <summary>Gets the display name of the bush.</summary>
  public string DisplayName { get; }

  /// <summary>Gets the default texture used when planted indoors.</summary>
  public string IndoorTexture { get; }

  /// <summary>Gets the season in which this bush will produce its drops.</summary>
  public List<Season> Seasons { get; }

  /// <summary>Gets the rules which override the locations that custom bushes can be planted in.</summary>
  public List<PlantableRule> PlantableLocationRules { get; }

  /// <summary>Gets the texture of the tea bush.</summary>
  public string Texture { get; }

  /// <summary>Gets the row index for the custom bush's sprites.</summary>
  public int TextureSpriteRow { get; }
}

/// <summary>Model used for drops from custom bushes.</summary>
public interface ICustomBushDrop : ISpawnItemData
{
  /// <summary>Gets the probability that the item will be produced.</summary>
  public float Chance { get; }

  /// <summary>A game state query which indicates whether the item should be added.</summary>
  public string? Condition { get; }

  /// <summary>An ID for this entry within the current list (not the item itself).</summary>
  public string? Id { get; }

  /// <summary>Gets the specific season when the item can be produced.</summary>
  public Season? Season { get; }
}

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
