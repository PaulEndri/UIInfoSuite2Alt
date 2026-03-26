#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley.Menus;

namespace UIInfoSuite2Alt.Compatibility;

public interface ITabChangedEvent
{
  IClickableMenu Menu { get; }
  string Tab { get; }
  string OldTab { get; }
}

public interface ITabContextMenuEvent
{
  IClickableMenu Menu { get; }
  bool IsCurrentTab { get; }
  string Tab { get; }
  IClickableMenu? Page { get; }
  IList<ITabContextMenuEntry> Entries { get; }
  ITabContextMenuEntry CreateEntry(
    string label,
    Action? onSelect,
    IBetterGameMenuApi.DrawDelegate? icon = null
  );
}

public interface ITabContextMenuEntry
{
  string Label { get; }
  Action? OnSelect { get; }
  IBetterGameMenuApi.DrawDelegate? Icon { get; }
}

public interface IPageCreatedEvent
{
  IClickableMenu Menu { get; }
  string Tab { get; }
  string Source { get; }
  IClickableMenu Page { get; }
  IClickableMenu? OldPage { get; }
}

public interface IBetterGameMenu
{
  IClickableMenu Menu { get; }
  bool Invisible { get; set; }
  IReadOnlyList<string> VisibleTabs { get; }
  string CurrentTab { get; }
  IClickableMenu? CurrentPage { get; }
  bool TryGetPage(
    string target,
    [NotNullWhen(true)] out IClickableMenu? page,
    bool forceCreation = false
  );
  bool TryChangeTab(string target, bool playSound = true);
  void UpdateTabs(string? target = null);
}

public enum VanillaTabOrders
{
  Inventory = 0,
  Skills = 20,
  Social = 40,
  Map = 60,
  Crafting = 80,
  Animals = 100,
  Powers = 120,
  Collections = 140,
  Options = 160,
  Exit = 200,
}

public interface IBetterGameMenuApi
{
  delegate void DrawDelegate(SpriteBatch batch, Rectangle bounds);

  DrawDelegate CreateDraw(
    Texture2D texture,
    Rectangle source,
    float scale = 1f,
    int frames = 1,
    int frameTime = 16,
    Vector2? offset = null
  );

  void RegisterTab(
    string id,
    int order,
    Func<string> getDisplayName,
    Func<(DrawDelegate DrawMethod, bool DrawBackground)> getIcon,
    int priority,
    Func<IClickableMenu, IClickableMenu> getPageInstance,
    Func<DrawDelegate?>? getDecoration = null,
    Func<bool>? getTabVisible = null,
    Func<bool>? getMenuInvisible = null,
    Func<int, int>? getWidth = null,
    Func<int, int>? getHeight = null,
    Func<(IClickableMenu Menu, IClickableMenu OldPage), IClickableMenu?>? onResize = null,
    Action<IClickableMenu>? onClose = null
  );

  void UnregisterImplementation(string id);

  IBetterGameMenu? ActiveMenu { get; }
  IClickableMenu? ActivePage { get; }
  IBetterGameMenu? AsMenu(IClickableMenu menu);
  bool IsMenu(IClickableMenu menu);
  IClickableMenu? GetCurrentPage(IClickableMenu menu);

  IBetterGameMenu? TryOpenMenu(
    string? defaultTab = null,
    bool playSound = false,
    bool closeExistingMenu = false
  );

  IClickableMenu CreateMenu(string? defaultTab = null, bool playSound = false);

  IClickableMenu CreateMenu(int startingTab, bool playSound = false);

  delegate void MenuCreatedDelegate(IClickableMenu menu);
  delegate void TabChangedDelegate(ITabChangedEvent evt);
  delegate void TabContextMenuDelegate(ITabContextMenuEvent evt);
  delegate void PageCreatedDelegate(IPageCreatedEvent evt);

  void OnMenuCreated(MenuCreatedDelegate handler, EventPriority priority = EventPriority.Normal);
  void OffMenuCreated(MenuCreatedDelegate handler);
  void OnTabChanged(TabChangedDelegate handler, EventPriority priority = EventPriority.Normal);
  void OffTabChanged(TabChangedDelegate handler);
  void OnTabContextMenu(
    TabContextMenuDelegate handler,
    EventPriority priority = EventPriority.Normal
  );
  void OffTabContextMenu(TabContextMenuDelegate handler);
  void OnPageCreated(PageCreatedDelegate handler, EventPriority priority = EventPriority.Normal);
  void OffPageCreated(PageCreatedDelegate handler);
}
