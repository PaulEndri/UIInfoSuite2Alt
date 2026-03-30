using StardewModdingAPI;
using StardewValley;
using Object = StardewValley.Object;

namespace UIInfoSuite2Alt.Compatibility.Helpers;

internal static class AquariumHelper
{
  private const string AquariumDonatedPrefix = "AquariumDonated:";

  private static bool _isModLoaded;

  public static bool IsModLoaded => _isModLoaded;

  public static void Initialize(IModHelper helper)
  {
    _isModLoaded = helper.ModRegistry.IsLoaded(ModCompat.StardewAquarium);
  }

  public static bool IsUndonatedAquariumFish(Item? item)
  {
    if (!_isModLoaded || item is not Object obj || obj.Category != Object.FishCategory)
    {
      return false;
    }

    string donationKey = AquariumDonatedPrefix + obj.Name.Replace(" ", string.Empty);
    return !Game1.MasterPlayer.mailReceived.Contains(donationKey);
  }
}
