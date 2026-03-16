using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using Object = StardewValley.Object;

namespace UIInfoSuite2Alt.UIElements;

internal class ShowMachineProcessingItem : IDisposable
{
  // Custom icon offsets from the machine's sprite center
  private static readonly Dictionary<string, Vector2> CustomOffsets = new()
  {
    { "Cask", new Vector2(0f, -20f) }
  };

  private readonly PerScreen<List<MachineIconData>> _visibleMachines = new(() => new List<MachineIconData>());

  private readonly IModHelper _helper;
  private bool _enabled;

  public ShowMachineProcessingItem(IModHelper helper)
  {
    _helper = helper;
    _helper.Events.Input.ButtonsChanged += OnButtonsChanged;
  }

  public void Dispose()
  {
    ToggleOption(false);
    _helper.Events.Input.ButtonsChanged -= OnButtonsChanged;
  }

  public void ToggleOption(bool enabled)
  {
    _enabled = enabled;

    _helper.Events.Display.RenderingHud -= OnRenderingHud;
    _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

    if (enabled)
    {
      _helper.Events.Display.RenderingHud += OnRenderingHud;
      _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }
  }

  private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
  {
    KeybindList keybind = ModEntry.ModConfig.ToggleMachineProcessingIcons;
    if (!Context.IsPlayerFree || !keybind.JustPressed())
    {
      return;
    }

    _helper.Input.SuppressActiveKeybinds(keybind);
    bool newValue = !_enabled;
    ModEntry.ModConfig.ShowMachineProcessingIcons = newValue;
    ModEntry.SaveConfig();
    ToggleOption(newValue);
  }

  private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    if (!e.IsMultipleOf(4))
    {
      return;
    }

    List<MachineIconData> machines = _visibleMachines.Value;
    machines.Clear();

    if (Game1.currentLocation == null ||
        !UIElementUtils.IsRenderingNormally() ||
        Game1.activeClickableMenu != null)
    {
      return;
    }

    // Viewport bounds in tile coordinates
    int startX = Game1.viewport.X / Game1.tileSize - 1;
    int startY = Game1.viewport.Y / Game1.tileSize - 1;
    int endX = (Game1.viewport.X + Game1.viewport.Width) / Game1.tileSize + 1;
    int endY = (Game1.viewport.Y + Game1.viewport.Height) / Game1.tileSize + 1;

    foreach ((Vector2 tile, Object obj) in Game1.currentLocation.Objects.Pairs)
    {
      int tileX = (int)tile.X;
      int tileY = (int)tile.Y;

      if (tileX < startX || tileX > endX || tileY < startY || tileY > endY)
      {
        continue;
      }

      if (!obj.bigCraftable.Value ||
          obj.heldObject.Value == null ||
          obj.readyForHarvest.Value ||
          obj.MinutesUntilReady <= 0 ||
          obj.Name == "Heater")
      {
        continue;
      }

      // Prefer the input item (preservedParentSheetIndex) over the output (heldObject).
      // For Wine/Juice/Jelly/Pickles, this shows the original fruit/vegetable instead of the output.
      // For machines without a preserved parent (Furnace, etc.), fall back to the output item.
      Object heldObject = obj.heldObject.Value;
      string? preservedId = heldObject.preservedParentSheetIndex.Value;

      ParsedItemData? itemData = !string.IsNullOrEmpty(preservedId)
        ? ItemRegistry.GetData("(O)" + preservedId) ?? ItemRegistry.GetData(preservedId)
        : ItemRegistry.GetData(heldObject.QualifiedItemId);

      ParsedItemData? machineData = ItemRegistry.GetData(obj.QualifiedItemId);
      if (itemData == null || machineData == null)
      {
        continue;
      }

      int machineSpriteHeight = machineData.GetSourceRect().Height;
      CustomOffsets.TryGetValue(obj.Name, out Vector2 offset);
      machines.Add(new MachineIconData(tile, itemData, machineSpriteHeight, offset));
    }
  }

  private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
  {
    if (!UIElementUtils.IsRenderingNormally() || Game1.activeClickableMenu != null)
    {
      return;
    }

    List<MachineIconData> machines = _visibleMachines.Value;
    if (machines.Count == 0)
    {
      return;
    }

    SpriteBatch spriteBatch = Game1.spriteBatch;

    foreach (MachineIconData machine in machines)
    {
      Vector2 screenPos = Game1.GlobalToLocal(
        new Vector2(machine.Tile.X * Game1.tileSize, machine.Tile.Y * Game1.tileSize)
      );

      // Center icon on the machine sprite.
      // Machine renders from (tileY + tileSize - spriteHeight) to (tileY + tileSize).
      int spriteHeight = machine.MachineSpriteHeight * Game1.pixelZoom;
      float machineCenterX = screenPos.X + Game1.tileSize / 2f;
      float machineCenterY = screenPos.Y + Game1.tileSize - spriteHeight / 2f;
      Vector2 iconPos = Utility.ModifyCoordinatesForUIScale(
        new Vector2(machineCenterX - 16f + machine.Offset.X, machineCenterY - 16f + machine.Offset.Y)
      );

      spriteBatch.Draw(
        machine.ItemData.GetTexture(),
        iconPos,
        machine.ItemData.GetSourceRect(),
        Color.White * 0.9f,
        0f,
        Vector2.Zero,
        Utility.ModifyCoordinateForUIScale(2f),
        SpriteEffects.None,
        1f
      );
    }
  }

  private readonly record struct MachineIconData(Vector2 Tile, ParsedItemData ItemData, int MachineSpriteHeight, Vector2 Offset);
}
