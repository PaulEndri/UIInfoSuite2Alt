using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Menus;
using SObject = StardewValley.Object;

namespace UIInfoSuite2Alt.Infrastructure.Helpers;

// Maps qualified item ID (or category string) -> list of [bundleIdx, quantity, quality]
using BundleIngredientsCache = Dictionary<string, List<List<int>>>;

public record BundleRequiredItem(string Name, int BannerWidth, int Id, string QualifiedId, int Quality);

public record BundleKeyData(string Name, int Color);

internal static class BundleHelper
{
  private static readonly Dictionary<int, BundleKeyData> BundleIdToBundleKeyDataMap = new();
  private static readonly BundleIngredientsCache AllBundleIngredients = new();

  public static BundleKeyData? GetBundleKeyDataFromIndex(int bundleIdx, bool forceRefresh = false)
  {
    PopulateBundleCaches(forceRefresh);
    return BundleIdToBundleKeyDataMap.GetValueOrDefault(bundleIdx);
  }

  public static Color? GetRealColorFromIndex(int bundleIdx, bool forceRefresh = false)
  {
    PopulateBundleCaches(forceRefresh);
    BundleKeyData? bundleData = BundleIdToBundleKeyDataMap.GetValueOrDefault(bundleIdx);
    if (bundleData == null)
    {
      return null;
    }

    return Bundle.getColorFromColorIndex(bundleData.Color);
  }

  private static int GetBundleBannerWidthForName(string bundleName)
  {
    return 68 + (int)Game1.dialogueFont.MeasureString(bundleName).X;
  }

  public static BundleRequiredItem? GetBundleItemIfNotDonated(Item item)
  {
    if (item is not SObject donatedItem || donatedItem.bigCraftable.Value)
    {
      return null;
    }

    // No bundles to track if player chose Joja route
    var communityCenter = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
    if (Game1.MasterPlayer.mailReceived.Contains("JojaMember"))
    {
      return null;
    }

    // No bundles to track if CC is complete and Missing Bundle (Abandoned JojaMart) is also done
    bool missingBundleDone = Game1.MasterPlayer.mailReceived.Contains("ccMovieTheater");
    if (communityCenter.areAllAreasComplete() && missingBundleDone)
    {
      return null;
    }

    PopulateBundleCaches();

    BundleRequiredItem? output;
    List<List<int>>? bundleRequiredItemsList;

    if (AllBundleIngredients.TryGetValue(donatedItem.QualifiedItemId, out bundleRequiredItemsList))
    {
      output = GetBundleItemIfNotDonatedFromList(bundleRequiredItemsList, donatedItem);
      if (output != null)
      {
        return output;
      }
    }

    if (donatedItem.Category >= 0 ||
        !AllBundleIngredients.TryGetValue(donatedItem.Category.ToString(), out bundleRequiredItemsList))
    {
      return null;
    }

    output = GetBundleItemIfNotDonatedFromList(bundleRequiredItemsList, donatedItem);
    return output;
  }

  private static BundleRequiredItem? GetBundleItemIfNotDonatedFromList(List<List<int>>? lists, ISalable obj)
  {
    if (lists == null)
    {
      return null;
    }

    foreach (List<int> list in lists)
    {
      if (list.Count < 3 || obj.Quality < list[2])
      {
        continue;
      }

      BundleKeyData? bundleKeyData = GetBundleKeyDataFromIndex(list[0]);
      if (bundleKeyData == null)
      {
        continue;
      }

      return new BundleRequiredItem(
        bundleKeyData.Name,
        GetBundleBannerWidthForName(bundleKeyData.Name),
        list[0],
        obj.QualifiedItemId,
        obj.Quality
      );
    }

    return null;
  }

  /// <summary>
  /// Builds both the bundle name/color map and the ingredients cache from BundleData.
  /// Unlike the game's bundlesIngredientsInfo, this includes ALL areas (not just unlocked ones),
  /// so the traveling merchant and item tooltips can detect bundle needs for locked rooms too.
  /// </summary>
  public static void PopulateBundleCaches(bool force = false)
  {
    if (BundleIdToBundleKeyDataMap.Count != 0 && !force)
    {
      return;
    }

    BundleIdToBundleKeyDataMap.Clear();
    AllBundleIngredients.Clear();

    Dictionary<string, string> bundleData = Game1.netWorldState.Value.BundleData;
    Dictionary<int, bool[]> donationStatus = Game1.netWorldState.Value.Bundles.Pairs
      .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());

    foreach (KeyValuePair<string, string> bundleInfo in bundleData)
    {
      try
      {
        string[] bundleLocationInfo = bundleInfo.Key.Split('/');
        int bundleIdx = Convert.ToInt32(bundleLocationInfo[1]);
        string[] bundleContentsData = bundleInfo.Value.Split('/');

        // Populate name/color map
        string localizedName = bundleContentsData[6];
        int color = Convert.ToInt32(bundleContentsData[3]);
        BundleIdToBundleKeyDataMap[bundleIdx] = new BundleKeyData(localizedName, color);

        // Populate ingredients cache for all undonated items (no area-unlock filter)
        string[] itemEntries = ArgUtility.SplitBySpace(bundleContentsData[2]);
        if (!donationStatus.TryGetValue(bundleIdx, out bool[]? donated))
        {
          continue;
        }

        for (int i = 0; i < itemEntries.Length; i += 3)
        {
          int slotIndex = i / 3;
          if (slotIndex < donated.Length && donated[slotIndex])
          {
            continue;
          }

          string itemId = itemEntries[i];
          int quantity = Convert.ToInt32(itemEntries[i + 1]);
          int quality = Convert.ToInt32(itemEntries[i + 2]);

          // Negative IDs are category matches, otherwise resolve to qualified item ID
          string key;
          if (int.TryParse(itemId, out int numericId) && numericId < 0)
          {
            key = numericId.ToString();
          }
          else
          {
            ParsedItemData? data = ItemRegistry.GetData(itemId);
            key = data != null ? data.QualifiedItemId : "(O)" + itemId;
          }

          if (!AllBundleIngredients.TryGetValue(key, out List<List<int>>? entryList))
          {
            entryList = new List<List<int>>();
            AllBundleIngredients[key] = entryList;
          }

          entryList.Add(new List<int> { bundleIdx, quantity, quality });
        }
      }
      catch (Exception)
      {
        ModEntry.MonitorObject.Log(
          $"Failed to parse info for bundle {bundleInfo.ToString()}, some information may be unavailable"
        );
      }
    }
  }
}
