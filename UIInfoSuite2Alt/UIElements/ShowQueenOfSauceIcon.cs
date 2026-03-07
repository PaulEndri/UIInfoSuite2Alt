using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using UIInfoSuite2Alt.Infrastructure;
using UIInfoSuite2Alt.Infrastructure.Extensions;

namespace UIInfoSuite2Alt.UIElements;

internal class ShowQueenOfSauceIcon : IDisposable
{
  private class QueenOfSauceTV : TV
  {
    public string[] GetWeeklyRecipe()
    {
      return base.getWeeklyRecipe();
    }
  }

  #region Properties
  private readonly Dictionary<string, string> _recipesByDescription = new();
  private Dictionary<string, string> _recipes = new();
  private CraftingRecipe? _todaysRecipe;

  private readonly PerScreen<bool> _drawQueenOfSauceIcon = new();
  private bool _showRecipeItemIcon;

  private readonly PerScreen<ClickableTextureComponent> _icon = new();

  private readonly IModHelper _helper;
  #endregion

  #region Life cycle
  public ShowQueenOfSauceIcon(IModHelper helper)
  {
    _helper = helper;
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool showQueenOfSauceIcon)
  {
    _helper.Events.Display.RenderingHud -= OnRenderingHud;
    _helper.Events.GameLoop.DayStarted -= OnDayStarted;
    _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
    _helper.Events.GameLoop.SaveLoaded -= OnSaveLoaded;

    if (showQueenOfSauceIcon)
    {
      LoadRecipes();
      CheckForNewRecipe();

      _helper.Events.GameLoop.DayStarted += OnDayStarted;
      _helper.Events.Display.RenderingHud += OnRenderingHud;
      _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
      _helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    }
  }

  public void ToggleShowRecipeItemIcon(bool showRecipeItemIcon)
  {
    _showRecipeItemIcon = showRecipeItemIcon;
  }
  #endregion

  #region Event subscriptions
  private void OnDayStarted(object? sender, DayStartedEventArgs e)
  {
    CheckForNewRecipe();
  }

  private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
  {
    CheckForNewRecipe();
  }

  private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    if (e.IsOneSecond && _drawQueenOfSauceIcon.Value && _todaysRecipe != null && Game1.player.knowsRecipe(_todaysRecipe.name))
    {
      _drawQueenOfSauceIcon.Value = false;
    }
  }

  private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
  {
    if (UIElementUtils.IsRenderingNormally() && _drawQueenOfSauceIcon.Value && _todaysRecipe != null)
    {
      IconHandler.Handler.EnqueueIcon(
        "QueenOfSauce",
        (batch, pos) =>
        {
          if (_showRecipeItemIcon)
          {
            var itemData = _todaysRecipe.GetItemData(useFirst: true);
            Texture2D itemTexture = itemData.GetTexture();
            Rectangle itemSourceRect = itemData.GetSourceRect();

            _icon.Value = new ClickableTextureComponent(
              new Rectangle(pos.X, pos.Y, 40, 40),
              itemTexture,
              itemSourceRect,
              2.5f
            );
            _icon.Value.draw(batch);

            batch.Draw(
              Game1.mouseCursors,
              new Vector2(pos.X + 18, pos.Y + 18),
              new Rectangle(609, 361, 28, 28),
              Color.White,
              0f,
              Vector2.Zero,
              0.8f,
              SpriteEffects.None,
              1f
            );
          }
          else
          {
            _icon.Value = new ClickableTextureComponent(
              new Rectangle(pos.X, pos.Y, 40, 40),
              Game1.mouseCursors,
              new Rectangle(609, 361, 28, 28),
              1.3f
            );
            _icon.Value.draw(batch);
          }
        },
        batch =>
        {
          if (!Game1.IsFakedBlackScreen() &&
              (_icon.Value?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false))
          {
            IClickableMenu.drawHoverText(
              batch,
              I18n.TodaysRecipe() + _todaysRecipe?.DisplayName,
              Game1.dialogueFont
            );
          }
        }
      );
    }
  }
  #endregion

  #region Logic
  private void LoadRecipes()
  {
    if (_recipes.Count == 0)
    {
      _recipes = Game1.content.Load<Dictionary<string, string>>("Data\\TV\\CookingChannel");
      foreach (KeyValuePair<string, string> next in _recipes)
      {
        string[] values = next.Value.Split('/');
        if (values.Length > 1)
        {
          _recipesByDescription[values[1]] = values[0];
        }
      }
    }
  }

  private void CheckForNewRecipe()
  {
    int recipiesKnownBeforeTvCall = Game1.player.cookingRecipes.Count();
    string[] dialogue = new QueenOfSauceTV().GetWeeklyRecipe();
    if (!_recipesByDescription.TryGetValue(dialogue[0], out string? recipeName))
    {
      _todaysRecipe = null;
      _drawQueenOfSauceIcon.Value = false;
      return;
    }

    _todaysRecipe = new CraftingRecipe(recipeName, true);

    if (Game1.player.cookingRecipes.Count() > recipiesKnownBeforeTvCall)
    {
      Game1.player.cookingRecipes.Remove(_todaysRecipe.name);
    }

    _drawQueenOfSauceIcon.Value = (Game1.dayOfMonth % 7 == 0 || (Game1.dayOfMonth - 3) % 7 == 0) &&
                                  Game1.stats.DaysPlayed > 5 &&
                                  !Game1.player.knowsRecipe(_todaysRecipe.name);
  }
  #endregion
}
