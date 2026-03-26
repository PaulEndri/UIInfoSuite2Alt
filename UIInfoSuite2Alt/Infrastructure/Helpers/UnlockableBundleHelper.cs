using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using UIInfoSuite2Alt.Compatibility;
using SObject = StardewValley.Object;

namespace UIInfoSuite2Alt.Infrastructure.Helpers;

public record UbBundleRequiredItem(
  string BundleName,
  int BannerWidth,
  string? IconTexturePath,
  string? ColorHex
);

/// <summary>
/// Checks whether items are needed for Unlockable Bundles (DLX.Bundles).
/// Uses reflection because SMAPI's Pintail proxy can't map getBundles()
/// (nested generic interfaces: IDictionary&lt;string, IList&lt;IBundle&gt;&gt;).
/// Phase 1: only matches simple item IDs and quality requirements.
/// Skips Flavored, AP, Recipe, Money, and category entries.
/// </summary>
internal static class UnlockableBundleHelper
{
  private record CacheEntry(string BundleName, int Quality, string BundleKey);

  // Maps qualified item ID -> list of cache entries
  private static readonly Dictionary<string, List<CacheEntry>> Cache = [];
  private static bool _cachePopulated;

  // Maps bundle key -> metadata from the content asset (UnlockableModel fields)
  private record BundleAssetData(string? Name, string? IconTexture, string? ColorHex);

  private static readonly Dictionary<string, BundleAssetData> BundleDataMap = [];

  /// <summary>Raised when UB bundle state changes (discovered, purchased, contributed).</summary>
  public static event Action? BundleStateChanged;

  // Reflection state - resolved once on first use
  private static IModHelper _helper = null!;
  private static object? _apiInstance;
  private static MethodInfo? _getBundlesMethod;
  private static bool _reflectionResolved;

  /// <summary>Call from OnGameLaunched to provide the mod helper for API resolution.</summary>
  public static void Initialize(IModHelper helper)
  {
    _helper = helper;
  }

  /// <summary>Clear the cached UB bundle data. Call on day start.</summary>
  public static void ClearCache()
  {
    Cache.Clear();
    BundleDataMap.Clear();
    _cachePopulated = false;
  }

  /// <summary>
  /// Returns bundle display info if the item is needed for any discovered, unpurchased UB bundle.
  /// Returns null if UB is not installed, item doesn't match, or no bundles need it.
  /// </summary>
  public static UbBundleRequiredItem? GetBundleItemIfNotDonated(Item item)
  {
    if (item is not SObject obj || obj.bigCraftable.Value)
    {
      return null;
    }

    if (!EnsureApi())
    {
      return null;
    }

    PopulateCache();

    if (!Cache.TryGetValue(obj.QualifiedItemId, out List<CacheEntry>? entries))
    {
      return null;
    }

    foreach (CacheEntry entry in entries)
    {
      if (obj.Quality >= entry.Quality)
      {
        int bannerWidth = 36 + (int)Game1.smallFont.MeasureString(entry.BundleName).X;

        string? iconTexture = null;
        string? colorHex = null;
        if (BundleDataMap.TryGetValue(entry.BundleKey, out BundleAssetData? assetData))
        {
          iconTexture = assetData.IconTexture;
          colorHex = assetData.ColorHex;
        }

        return new UbBundleRequiredItem(entry.BundleName, bannerWidth, iconTexture, colorHex);
      }
    }

    return null;
  }

  /// <summary>
  /// Check all merchant stock items against UB bundles. Returns display names of matching items.
  /// </summary>
  public static List<string> GetMerchantBundleItemNames(IEnumerable<ISalable> stock)
  {
    if (!EnsureApi())
    {
      return [];
    }

    PopulateCache();

    List<string> names = [];
    foreach (ISalable salable in stock)
    {
      if (salable is not SObject obj || obj.bigCraftable.Value)
      {
        continue;
      }

      if (!Cache.TryGetValue(obj.QualifiedItemId, out List<CacheEntry>? entries))
      {
        continue;
      }

      foreach (CacheEntry entry in entries)
      {
        if (obj.Quality >= entry.Quality)
        {
          names.Add(obj.DisplayName);
          break;
        }
      }
    }

    return names;
  }

  /// <summary>
  /// Resolve the raw API object and getBundles method via reflection.
  /// Returns true if UB is installed and the API is available.
  /// </summary>
  private static bool EnsureApi()
  {
    if (_reflectionResolved)
    {
      return _getBundlesMethod != null;
    }

    _reflectionResolved = true;

    if (!_helper.ModRegistry.IsLoaded(ModCompat.UnlockableBundles))
    {
      return false;
    }

    // Get the raw API object (no Pintail proxy)
    _apiInstance = _helper.ModRegistry.GetApi(ModCompat.UnlockableBundles);
    if (_apiInstance == null)
    {
      return false;
    }

    Type apiType = _apiInstance.GetType();
    _getBundlesMethod = apiType.GetMethod("getBundles");
    if (_getBundlesMethod == null)
    {
      ModEntry.MonitorObject.Log(
        "UnlockableBundleHelper: getBundles() method not found on UB API",
        LogLevel.Warn
      );
      return false;
    }

    SubscribeEvent(apiType, "BundleContributedEvent");
    SubscribeEvent(apiType, "BundlePurchasedEvent");
    SubscribeEvent(apiType, "BundleDiscoveredEvent");

    return true;
  }

  private static void PopulateCache()
  {
    if (_cachePopulated)
    {
      return;
    }

    _cachePopulated = true;

    try
    {
      LoadBundleData();

      object? result = _getBundlesMethod!.Invoke(_apiInstance, null);
      if (result is not IDictionary bundleDict)
      {
        return;
      }

      foreach (DictionaryEntry entry in bundleDict)
      {
        if (entry.Value is not IList bundleInstances)
        {
          continue;
        }

        foreach (object bundleObj in bundleInstances)
        {
          ProcessBundle(bundleObj);
        }
      }

      int bundleCount = new HashSet<string>(
        Cache.Values.SelectMany(l => l).Select(e => e.BundleKey)
      ).Count;
      ModEntry.MonitorObject.Log(
        $"UnlockableBundleHelper: cached UB bundle needs, items={Cache.Count}, activeBundles={bundleCount}",
        LogLevel.Trace
      );
    }
    catch (Exception e)
    {
      ModEntry.MonitorObject.Log(
        $"UnlockableBundleHelper: failed to parse bundles, {e.Message}",
        LogLevel.Warn
      );
    }
  }

  /// <summary>
  /// Load bundle metadata from the UnlockableBundles/Bundles content asset.
  /// The IBundle API only exposes Key - name and icon live in the content data (UnlockableModel fields).
  /// </summary>
  private static void LoadBundleData()
  {
    BundleDataMap.Clear();
    try
    {
      object asset = _helper.GameContent.Load<object>("UnlockableBundles/Bundles");
      if (asset is not IDictionary assetDict)
      {
        return;
      }

      foreach (DictionaryEntry entry in assetDict)
      {
        string? bundleKey = entry.Key as string;
        if (bundleKey == null || entry.Value == null)
        {
          continue;
        }

        // UnlockableModel uses public fields, not properties
        Type t = entry.Value.GetType();
        string? bundleName = t.GetField("BundleName")?.GetValue(entry.Value) as string;
        string? iconAsset = t.GetField("BundleIconAsset")?.GetValue(entry.Value) as string;
        // Try ShopColor first, fall back to OverviewColor
        string? colorHex = t.GetField("ShopColor")?.GetValue(entry.Value) as string;
        if (string.IsNullOrEmpty(colorHex))
        {
          colorHex = t.GetField("OverviewColor")?.GetValue(entry.Value) as string;
        }

        BundleDataMap[bundleKey] = new BundleAssetData(bundleName, iconAsset, colorHex);
      }

      ModEntry.MonitorObject.Log(
        $"UnlockableBundleHelper: loaded UB bundle data, defined={BundleDataMap.Count}",
        LogLevel.Trace
      );
    }
    catch (Exception e)
    {
      ModEntry.MonitorObject.Log(
        $"UnlockableBundleHelper: failed to load bundle metadata, {e.Message}",
        LogLevel.Debug
      );
    }
  }

  private static void ProcessBundle(object bundleObj)
  {
    Type type = bundleObj.GetType();

    bool purchased = (bool)(type.GetProperty("Purchased")?.GetValue(bundleObj) ?? true);
    bool discovered = (bool)(type.GetProperty("Discovered")?.GetValue(bundleObj) ?? false);
    if (purchased || !discovered)
    {
      return;
    }

    string key = (string)(type.GetProperty("Key")?.GetValue(bundleObj) ?? "");

    // Get display name from content asset, fall back to key derivation
    string displayName;
    if (
      BundleDataMap.TryGetValue(key, out BundleAssetData? data) && !string.IsNullOrEmpty(data.Name)
    )
    {
      displayName = data.Name;
    }
    else
    {
      displayName = DeriveBundleName(key);
    }

    var price = type.GetProperty("Price")?.GetValue(bundleObj) as IDictionary<string, int>;
    var alreadyPaid =
      type.GetProperty("AlreadyPaid")?.GetValue(bundleObj) as IDictionary<string, int>;
    if (price == null)
    {
      return;
    }

    foreach (var (priceKey, requiredAmount) in price)
    {
      // Skip advanced pricing types and money
      if (
        priceKey.StartsWith("(UB.", StringComparison.Ordinal)
        || priceKey.Equals("Money", StringComparison.OrdinalIgnoreCase)
      )
      {
        continue;
      }

      // Check if already fully paid
      if (
        alreadyPaid != null
        && alreadyPaid.TryGetValue(priceKey, out int paid)
        && paid >= requiredAmount
      )
      {
        continue;
      }

      // Parse quality suffix (e.g. "(O)284:Gold" -> "(O)284", Gold)
      string itemPart = priceKey;
      int minQuality = 0;
      int colonIdx = priceKey.LastIndexOf(':');
      if (colonIdx > 0)
      {
        string qualitySuffix = priceKey[(colonIdx + 1)..];
        itemPart = priceKey[..colonIdx];
        minQuality = ParseQuality(qualitySuffix);
        if (minQuality < 0)
        {
          continue;
        }
      }

      // Skip negative IDs (categories) for phase 1
      if (int.TryParse(itemPart, out int numericId) && numericId < 0)
      {
        continue;
      }

      // Resolve to qualified item ID
      string qualifiedId = ResolveQualifiedId(itemPart);

      if (!Cache.TryGetValue(qualifiedId, out List<CacheEntry>? entryList))
      {
        entryList = [];
        Cache[qualifiedId] = entryList;
      }

      entryList.Add(new CacheEntry(displayName, minQuality, key));
    }
  }

  private static string ResolveQualifiedId(string itemId)
  {
    if (itemId.StartsWith('('))
    {
      ParsedItemData? data = ItemRegistry.GetData(itemId);
      return data?.QualifiedItemId ?? itemId;
    }

    ParsedItemData? resolved = ItemRegistry.GetData(itemId);
    return resolved?.QualifiedItemId ?? "(O)" + itemId;
  }

  /// <summary>Derive a human-readable name from a bundle key like "DLX.SomeMod.BridgeBundle".</summary>
  private static string DeriveBundleName(string bundleKey)
  {
    int lastSep = Math.Max(bundleKey.LastIndexOf('.'), bundleKey.LastIndexOf('/'));
    string raw = lastSep >= 0 ? bundleKey[(lastSep + 1)..] : bundleKey;
    return System.Text.RegularExpressions.Regex.Replace(raw, "(?<!^)([A-Z])", " $1");
  }

  private static int ParseQuality(string suffix)
  {
    return suffix switch
    {
      "Silver" => 1,
      "Gold" => 2,
      "Iridium" => 4,
      _ => -1,
    };
  }

  /// <summary>
  /// Subscribe to a UB API event via reflection to clear cache on bundle state changes.
  /// The delegate types are internal to UB, so we build a compatible handler dynamically.
  /// </summary>
  private static void SubscribeEvent(Type apiType, string eventName)
  {
    try
    {
      EventInfo? eventInfo = apiType.GetEvent(eventName);
      if (eventInfo == null)
      {
        return;
      }

      // All UB events have signature (object sender, T args) - we only need to clear cache
      Type handlerType = eventInfo.EventHandlerType!;
      MethodInfo clearMethod = typeof(UnlockableBundleHelper).GetMethod(
        nameof(OnBundleStateChanged),
        BindingFlags.NonPublic | BindingFlags.Static
      )!;
      Delegate handler = Delegate.CreateDelegate(handlerType, clearMethod);
      eventInfo.AddEventHandler(_apiInstance, handler);
    }
    catch (Exception e)
    {
      ModEntry.MonitorObject.Log(
        $"UnlockableBundleHelper: failed to subscribe to {eventName}, {e.Message}",
        LogLevel.Trace
      );
    }
  }

  private static void OnBundleStateChanged(object sender, object args)
  {
    ClearCache();
    BundleStateChanged?.Invoke();
  }
}
