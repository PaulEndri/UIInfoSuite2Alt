using StardewValley;
using StardewValley.Menus;

namespace UIInfoSuite2Alt.Compatibility.Helpers;

/// <summary>Abstracts GameMenu access to support both vanilla and Better Game Menu.</summary>
public static class GameMenuHelper
{
  // Vanilla tab index → BGM tab name mapping
  private static readonly string[] VanillaTabNames =
  {
    nameof(VanillaTabOrders.Inventory), // 0
    nameof(VanillaTabOrders.Skills), // 1
    nameof(VanillaTabOrders.Social), // 2
    nameof(VanillaTabOrders.Map), // 3
    nameof(VanillaTabOrders.Crafting), // 4
    nameof(VanillaTabOrders.Animals), // 5
    nameof(VanillaTabOrders.Powers), // 6
    nameof(VanillaTabOrders.Collections), // 7
    nameof(VanillaTabOrders.Options), // 8
    nameof(VanillaTabOrders.Exit), // 9
  };

  public static bool HasBetterGameMenu => GetBgmApi() != null;

  /// <summary>Returns true if the menu is a GameMenu or a Better Game Menu.</summary>
  public static bool IsGameMenu(IClickableMenu? menu)
  {
    if (menu is GameMenu)
    {
      return true;
    }

    IBetterGameMenuApi? bgm = GetBgmApi();
    return bgm != null && menu != null && bgm.IsMenu(menu);
  }

  /// <summary>Gets the currently displayed page from either menu type.</summary>
  public static IClickableMenu? GetCurrentPage(IClickableMenu? menu)
  {
    if (menu is GameMenu gameMenu)
    {
      return gameMenu.GetCurrentPage();
    }

    IBetterGameMenuApi? bgm = GetBgmApi();
    return bgm != null && menu != null ? bgm.GetCurrentPage(menu) : null;
  }

  /// <summary>Checks if the current tab matches the given vanilla tab index.</summary>
  public static bool IsTab(IClickableMenu? menu, int vanillaTabIndex)
  {
    if (menu is GameMenu gameMenu)
    {
      return gameMenu.currentTab == vanillaTabIndex;
    }

    IBetterGameMenuApi? bgm = GetBgmApi();
    if (bgm == null || menu == null)
    {
      return false;
    }

    IBetterGameMenu? bgmMenu = bgm.AsMenu(menu);
    if (bgmMenu == null || vanillaTabIndex < 0 || vanillaTabIndex >= VanillaTabNames.Length)
    {
      return false;
    }

    return bgmMenu.CurrentTab == VanillaTabNames[vanillaTabIndex];
  }

  /// <summary>Finds a page of the given type, searching all pages for vanilla or using TryGetPage for BGM.</summary>
  public static T? FindPage<T>(IClickableMenu? menu)
    where T : class
  {
    if (menu is GameMenu gameMenu)
    {
      foreach (IClickableMenu page in gameMenu.pages)
      {
        if (page is T found)
        {
          return found;
        }
      }

      return null;
    }

    IBetterGameMenuApi? bgm = GetBgmApi();
    if (bgm == null || menu == null)
    {
      return null;
    }

    IBetterGameMenu? bgmMenu = bgm.AsMenu(menu);
    if (bgmMenu == null)
    {
      return null;
    }

    // Try known tab names that might contain the requested page type
    foreach (string tabName in bgmMenu.VisibleTabs)
    {
      if (bgmMenu.TryGetPage(tabName, out IClickableMenu? page) && page is T found)
      {
        return found;
      }
    }

    return null;
  }

  /// <summary>Gets the hover text from either menu type.</summary>
  public static string GetHoverText(IClickableMenu? menu)
  {
    if (menu is GameMenu gameMenu)
    {
      return gameMenu.hoverText;
    }

    // BGM doesn't expose hoverText directly on the menu; it comes from the current page.
    // The caller should handle drawing hover text differently for BGM.
    return "";
  }

  /// <summary>Gets the child menu from either menu type.</summary>
  public static IClickableMenu? GetChildMenu(IClickableMenu? menu)
  {
    return menu?.GetChildMenu();
  }

  /// <summary>Gets the BGM tab name for a vanilla tab index.</summary>
  public static string? GetTabName(int vanillaTabIndex)
  {
    if (vanillaTabIndex >= 0 && vanillaTabIndex < VanillaTabNames.Length)
    {
      return VanillaTabNames[vanillaTabIndex];
    }

    return null;
  }

  private static IBetterGameMenuApi? GetBgmApi()
  {
    ApiManager.GetApi<IBetterGameMenuApi>(ModCompat.BetterGameMenu, out IBetterGameMenuApi? api);
    return api;
  }
}
