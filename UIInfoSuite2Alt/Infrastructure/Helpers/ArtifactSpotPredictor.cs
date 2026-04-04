using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Constants;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.GameData.Locations;
using StardewValley.Internal;
using StardewValley.Locations;
using StardewValley.Tools;

namespace UIInfoSuite2Alt.Infrastructure.Helpers;

internal readonly struct PredictedDrop
{
  public Item Item { get; }

  /// <summary>Chance (0-1) that a Secret Note/Journal Scrap replaces this item. 0 = no note possible.</summary>
  public float SecretNoteChance { get; }

  /// <summary>Qualified item ID of the note (e.g. "(O)79" or "(O)842").</summary>
  public string? SecretNoteItemId { get; }

  /// <summary>Display name of the note item (e.g. "Secret Note" or "Journal Scrap").</summary>
  public string? SecretNoteDisplayName { get; }

  public PredictedDrop(
    Item item,
    float secretNoteChance = 0f,
    string? secretNoteItemId = null,
    string? secretNoteDisplayName = null
  )
  {
    Item = item;
    SecretNoteChance = secretNoteChance;
    SecretNoteItemId = secretNoteItemId;
    SecretNoteDisplayName = secretNoteDisplayName;
  }
}

/// <summary>
/// Predicts what items will drop from artifact spots and seed spots
/// by replicating the game's seeded RNG logic.
/// </summary>
internal static class ArtifactSpotPredictor
{
  private const string SecretNoteQueryPrefix = "SECRET_NOTE_OR_ITEM ";

  /// <summary>
  /// Predicts all items that would drop from an artifact spot at the given tile.
  /// Replicates GameLocation.digUpArtifactSpot and the book check in Object.performToolAction.
  /// </summary>
  public static List<PredictedDrop> PredictArtifactSpotDrop(
    GameLocation location,
    int tileX,
    int tileY,
    Farmer farmer
  )
  {
    var results = new List<PredictedDrop>();

    // --- Defense Book check (from Object.performToolAction) ---
    // Uses a separate Random from the main artifact spot drops
    var bookRandom = Utility.CreateDaySaveRandom(
      -tileX * 7f,
      tileY * 777f,
      Game1.netWorldState.Value.TreasureTotemsUsed * 777
    );
    PredictDefenseBook(bookRandom, farmer, results);

    // --- Location-specific overrides (before base.digUpArtifactSpot) ---
    PredictLocationOverrideDrops(location, tileX, tileY, results);

    // --- Main artifact spot drops (from GameLocation.digUpArtifactSpot) ---
    var random = Utility.CreateDaySaveRandom(
      tileX * 2000,
      tileY,
      Game1.netWorldState.Value.TreasureTotemsUsed * 777
    );

    // Mystery Box check (Qi plane)
    PredictMysteryBox(random, farmer, results);

    // Rare global drops (Golden Animal Cracker, cosmetic, skill book)
    PredictRareDrops(random, farmer, results);

    // Location artifact spot drop table
    bool hasGenerousEnchantment =
      (farmer.CurrentTool as Hoe)?.hasEnchantmentOfType<GenerousEnchantment>() ?? false;
    PredictLocationDrops(random, location, farmer, hasGenerousEnchantment, results);

    return results;
  }

  /// <summary>
  /// Predicts what a seed spot will drop.
  /// Replicates the SeedSpot path in Object.performToolAction.
  /// </summary>
  public static List<PredictedDrop> PredictSeedSpotDrop(Farmer farmer, int tileX, int tileY)
  {
    var results = new List<PredictedDrop>();

    var random = Utility.CreateDaySaveRandom(
      -tileX * 7f,
      tileY * 777f,
      Game1.netWorldState.Value.TreasureTotemsUsed * 777
    );

    // Defense Book check (same random, consumed before seed generation)
    PredictDefenseBook(random, farmer, results);

    // Raccoon seeds
    results.Add(new PredictedDrop(PredictRaccoonSeeds(random, farmer)));

    return results;
  }

  /// <summary>
  /// Calculates the Secret Note / Journal Scrap drop chance for a location.
  /// Returns 0 if the player isn't eligible.
  /// </summary>
  private static (float chance, string? displayName, string? itemId) GetSecretNoteInfo(
    GameLocation location,
    Farmer farmer
  )
  {
    bool isIsland = location.InIslandContext();
    if (!isIsland && !farmer.hasMagnifyingGlass)
    {
      return (0f, null, null);
    }

    if (location.currentEvent != null && location.currentEvent.isFestival)
    {
      return (0f, null, null);
    }

    string itemId = isIsland ? "(O)842" : "(O)79";
    int unseenCount =
      Utility.GetUnseenSecretNotes(farmer, isIsland, out int totalNotes).Length
      - farmer.Items.CountId(itemId);

    if (unseenCount <= 0)
    {
      return (0f, null, null);
    }

    float ratio = (float)(unseenCount - 1) / Math.Max(1, totalNotes - 1);
    float chance =
      GameLocation.LAST_SECRET_NOTE_CHANCE
      + (GameLocation.FIRST_SECRET_NOTE_CHANCE - GameLocation.LAST_SECRET_NOTE_CHANCE) * ratio;

    string displayName = ItemRegistry.GetData(itemId)?.DisplayName ?? "Secret Note";
    return (chance, displayName, itemId);
  }

  /// <summary>
  /// Handles location-specific overrides that run before base.digUpArtifactSpot.
  /// IslandLocation and DesertFestival add extra drops with their own Random.
  /// </summary>
  private static void PredictLocationOverrideDrops(
    GameLocation location,
    int tileX,
    int tileY,
    List<PredictedDrop> results
  )
  {
    if (location is IslandLocation)
    {
      // IslandLocation.digUpArtifactSpot uses a separate Random without TreasureTotemsUsed
      var islandRandom = Utility.CreateDaySaveRandom(tileX * 2000, tileY);

      if (Game1.netWorldState.Value.GoldenCoconutCracked && islandRandom.NextDouble() < 0.1)
      {
        results.Add(new PredictedDrop(ItemRegistry.Create("(O)791"))); // Golden Coconut
      }
      else if (islandRandom.NextDouble() < 0.33)
      {
        int count = islandRandom.Next(2, 5);
        results.Add(new PredictedDrop(ItemRegistry.Create("(O)831", count))); // Taro Tuber
      }
      else if (islandRandom.NextDouble() < 0.15)
      {
        int count = islandRandom.Next(1, 3);
        results.Add(new PredictedDrop(ItemRegistry.Create("(O)275", count))); // Artifact Trove
      }
    }
    else if (location is DesertFestival)
    {
      // DesertFestival.digUpArtifactSpot adds Calico Eggs
      var festivalRandom = Utility.CreateDaySaveRandom(tileX * 2000, tileY);
      int count = festivalRandom.Next(3, 7);
      results.Add(new PredictedDrop(ItemRegistry.Create("CalicoEgg", count)));
    }
  }

  private static void PredictDefenseBook(Random random, Farmer farmer, List<PredictedDrop> results)
  {
    // The game increments ArtifactSpotsDug before this check, so we simulate +1
    uint spotsDug = farmer.stats.Get("ArtifactSpotsDug") + 1;

    // Game: if (spotsDug > 2 && random.NextDouble() < chance)
    // The random call only happens when spotsDug > 2
    if (spotsDug <= 2)
    {
      return;
    }

    bool hasBookMail = farmer.mailReceived.Contains("DefenseBookDropped");
    double bookChance = 0.008 + (hasBookMail ? 0.005 : (double)spotsDug * 0.002);

    if (random.NextDouble() < bookChance)
    {
      results.Add(new PredictedDrop(ItemRegistry.Create("(O)Book_Defense")));
    }
  }

  private static void PredictMysteryBox(Random random, Farmer farmer, List<PredictedDrop> results)
  {
    if (
      farmer.mailReceived.Contains("sawQiPlane")
      && random.NextDouble() < 0.05 + farmer.team.AverageDailyLuck() / 2.0
    )
    {
      int count = random.Next(1, 3);
      results.Add(new PredictedDrop(ItemRegistry.Create("(O)MysteryBox", count)));
    }
  }

  private static void PredictRareDrops(Random random, Farmer farmer, List<PredictedDrop> results)
  {
    // trySpawnRareObject with chanceModifier=9.0, dailyLuckWeight=1.0
    const double chanceModifier = 9.0;
    double luckMult = 1.0 + farmer.team.AverageDailyLuck();

    // Golden Animal Cracker (requires farming mastery)
    if (farmer.stats.Get(StatKeys.Mastery(0)) != 0)
    {
      if (random.NextDouble() < 0.001 * chanceModifier * luckMult)
      {
        results.Add(new PredictedDrop(ItemRegistry.Create("(O)GoldenAnimalCracker")));
      }
    }

    // Cosmetic item
    if (Game1.stats.DaysPlayed > 2)
    {
      if (random.NextDouble() < 0.002 * chanceModifier)
      {
        results.Add(new PredictedDrop(Utility.getRandomCosmeticItem(random)));
      }
    }

    // Skill Book
    if (Game1.stats.DaysPlayed > 2)
    {
      if (random.NextDouble() < 0.0006 * chanceModifier)
      {
        results.Add(new PredictedDrop(ItemRegistry.Create("(O)SkillBook_" + random.Next(5))));
      }
    }
  }

  private static void PredictLocationDrops(
    Random random,
    GameLocation location,
    Farmer farmer,
    bool hasGenerousEnchantment,
    List<PredictedDrop> results
  )
  {
    LocationData? data = location.GetData();
    var context = new ItemQueryContext(location, farmer, random, "artifact spot prediction");

    IEnumerable<ArtifactSpotDropData> dropTable = Game1.locationData["Default"].ArtifactSpots;
    if (data?.ArtifactSpots?.Count > 0)
    {
      dropTable = dropTable.Concat(data.ArtifactSpots);
    }

    dropTable = dropTable.OrderBy(p => p.Precedence);

    // Pre-calculate secret note chance for this location
    (float noteChance, string? noteDisplayName, string? noteItemId) = GetSecretNoteInfo(
      location,
      farmer
    );

    foreach (ArtifactSpotDropData drop in dropTable)
    {
      if (!random.NextBool(drop.Chance))
      {
        continue;
      }

      if (
        drop.Condition != null
        && !GameStateQuery.CheckConditions(drop.Condition, location, farmer, null, null, random)
      )
      {
        continue;
      }

      // Intercept SECRET_NOTE_OR_ITEM: resolve only the fallback item and attach note chance.
      // This avoids calling tryToCreateUnseenSecretNote which uses Game1.random.
      if (drop.ItemId != null && drop.ItemId.StartsWith(SecretNoteQueryPrefix))
      {
        string fallbackQuery = drop.ItemId[SecretNoteQueryPrefix.Length..];
        Item? fallbackItem = ItemQueryResolver.TryResolveRandomItem(
          fallbackQuery,
          context,
          avoidRepeat: false,
          null,
          delegate { }
        );

        if (fallbackItem != null)
        {
          results.Add(new PredictedDrop(fallbackItem, noteChance, noteItemId, noteDisplayName));
        }

        if (!drop.ContinueOnDrop)
        {
          break;
        }

        continue;
      }

      Item? item = ItemQueryResolver.TryResolveRandomItem(
        drop,
        context,
        avoidRepeat: false,
        null,
        null,
        null,
        delegate { }
      );

      if (item == null)
      {
        continue;
      }

      results.Add(new PredictedDrop(item));

      // Generous Enchantment consumes a random.NextBool() after each successful drop,
      // which shifts the Random state for subsequent ContinueOnDrop entries
      if (hasGenerousEnchantment && drop.ApplyGenerousEnchantment)
      {
        if (random.NextBool())
        {
          // Generous duplicate - add another copy of the same item
          Item duplicate = item.getOne();
          duplicate = (Item)ItemQueryResolver.ApplyItemFields(duplicate, drop, context);
          results.Add(new PredictedDrop(duplicate));
        }
      }

      if (!drop.ContinueOnDrop)
      {
        break;
      }
    }
  }

  private static Item PredictRaccoonSeeds(Random random, Farmer farmer)
  {
    // Replicates Utility.getRaccoonSeedForCurrentTimeOfYear
    int stack = random.Next(2, 4);
    while (random.NextDouble() < 0.1 + farmer.team.AverageDailyLuck())
    {
      stack++;
    }

    Season season = Game1.season;
    if (Game1.dayOfMonth > ((int)season == 0 ? 23 : 20))
    {
      season = (Season)(((int)season + 1) % 4);
    }

    string itemId = season switch
    {
      Season.Spring => "(O)CarrotSeeds",
      Season.Summer => "(O)SummerSquashSeeds",
      Season.Fall => "(O)BroccoliSeeds",
      Season.Winter => "(O)PowdermelonSeeds",
      _ => "(O)CarrotSeeds",
    };

    Item item = ItemRegistry.Create(itemId);
    item.Stack = stack;
    return item;
  }
}
