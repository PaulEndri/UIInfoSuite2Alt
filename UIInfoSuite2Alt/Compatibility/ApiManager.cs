using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI;

namespace UIInfoSuite2Alt.Compatibility;

public static class ModCompat
{
  public const string CustomBush = "furyx639.CustomBush";
  public const string Gmcm = "spacechase0.GenericModConfigMenu";
  public const string CloudySkies = "leclair.cloudyskies";
  public const string DeluxeJournal = "MolsonCAD.DeluxeJournal";
  public const string BetterGameMenu = "leclair.bettergamemenu";
  public const string FerngillEconomy = "paulsteele.fse";
  public const string RidgesideVillage = "Rafseazz.RidgesideVillage";
  public const string SunberryVillage = "SunberryTeam.SBVSMAPI";
  public const string EscasModdingPlugins = "Esca.EMP";
  public const string NpcMapLocations = "Bouhm.NPCMapLocations";
  public const string SpaceCore = "spacechase0.SpaceCore";
  public const string VanillaPlusProfessions = "KediDili.VanillaPlusProfessions";

  // original UIInfoSuite variants
  public const string UIInfoSuite2 = "Annosz.UiInfoSuite2";
  public const string UIInfoSuite = "Cdaragorn.UiInfoSuite";
}

public static class ApiManager
{
  private static readonly Dictionary<string, object> RegisteredApis = [];

  private static readonly List<string> SuccessfullyLoadedModIds = [];

  public static T? TryRegisterApi<T>(
    IModHelper helper,
    string modId,
    string? minimumVersion = null,
    bool warnIfNotPresent = false
  )
    where T : class
  {
    IModInfo? modInfo = helper.ModRegistry.Get(modId);
    if (modInfo == null)
      return null;

    if (minimumVersion != null && modInfo.Manifest.Version.IsOlderThan(minimumVersion))
    {
      ModEntry.MonitorObject.Log(
        $"Requested version {minimumVersion} for mod {modId}, but got {modInfo.Manifest.Version} instead.",
        LogLevel.Warn
      );
      return null;
    }

    var api = helper.ModRegistry.GetApi<T>(modId);
    if (api is null)
    {
      if (warnIfNotPresent)
        ModEntry.MonitorObject.Log($"Could not find API for mod {modId}", LogLevel.Warn);
      return null;
    }

    RegisteredApis[modId] = api;
    SuccessfullyLoadedModIds.Add(modId);
    return api;
  }

  public static void LogLoadedApis()
  {
    if (SuccessfullyLoadedModIds.Count > 0)
    {
      string allMods = string.Join(", ", SuccessfullyLoadedModIds);
      ModEntry.MonitorObject.Log($"Loaded APIs: {allMods}", LogLevel.Info);

      SuccessfullyLoadedModIds.Clear();
    }
  }

  public static bool GetApi<T>(string modId, [NotNullWhen(true)] out T? apiInstance)
    where T : class
  {
    apiInstance = null;
    if (!RegisteredApis.TryGetValue(modId, out object? api))
    {
      return false;
    }

    if (api is T apiVal)
    {
      apiInstance = apiVal;
      return true;
    }

    ModEntry.MonitorObject.Log(
      $"API was registered for mod {modId} but the requested type is not supported",
      LogLevel.Warn
    );
    return false;
  }
}
