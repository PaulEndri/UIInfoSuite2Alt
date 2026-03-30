using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using UIInfoSuite2Alt.Infrastructure;

namespace UIInfoSuite2Alt.Compatibility.Helpers;

internal static class InformantHelper
{
  private static IModHelper _helper = null!;
  private static Texture2D? _aquariumIcon;
  private static bool _aquariumIconLoaded;

  public static void RegisterDecorators(IModHelper helper)
  {
    if (!helper.ModRegistry.IsLoaded(ModCompat.Informant))
    {
      return;
    }

    var api = helper.ModRegistry.GetApi<IInformantApi>(ModCompat.Informant);
    bool aquarium = helper.ModRegistry.IsLoaded(ModCompat.StardewAquarium);

    if (api == null)
    {
      ModEntry.MonitorObject.Log(
        "InformantHelper: Informant detected but API unavailable, inventory tooltip overlap suppressed",
        LogLevel.Warn
      );
      return;
    }

    // Register Aquarium decorator if Stardew Aquarium is installed
    if (aquarium)
    {
      _helper = helper;

      api.AddItemDecorator(
        "uiis2alt-aquarium",
        () => "Stardew Aquarium",
        () => "Shows an icon on fish not yet donated to the Aquarium",
        GetAquariumDecoratorIcon
      );

      ModEntry.MonitorObject.Log(
        "InformantHelper: Registered Stardew Aquarium Decorator",
        LogLevel.Info
      );
    }

    ModEntry.MonitorObject.Log("InformantHelper: Informant Detected", LogLevel.Info);
  }

  private static Texture2D? GetAquariumDecoratorIcon(Item item)
  {
    if (!AquariumHelper.IsUndonatedAquariumFish(item))
    {
      return null;
    }

    if (!_aquariumIconLoaded)
    {
      _aquariumIconLoaded = true;
      try
      {
        Texture2D curatorSheet = _helper.GameContent.Load<Texture2D>("Characters/Curator");
        _aquariumIcon = Tools.CropTexture(curatorSheet, new Rectangle(0, 1, 16, 16));
      }
      catch (Exception)
      {
        ModEntry.MonitorObject.Log(
          "InformantHelper: failed to load Curator sprite for Aquarium decorator",
          LogLevel.Warn
        );
      }
    }

    return _aquariumIcon;
  }
}
