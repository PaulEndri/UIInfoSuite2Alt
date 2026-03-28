using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2Alt.Infrastructure;
using UIInfoSuite2Alt.Infrastructure.Extensions;

namespace UIInfoSuite2Alt.UIElements;

internal class ShowBirthdayIcon : IDisposable
{
  #region Properties
  private readonly PerScreen<List<NPC>> _birthdayNPCs = new(() => []);

  private readonly PerScreen<List<ClickableTextureComponent>> _birthdayIcons = new(() => []);

  private bool Enabled { get; set; }
  private bool HideBirthdayIfFullFriendShip { get; set; }
  private readonly IModHelper _helper;
  #endregion


  #region Life cycle
  public ShowBirthdayIcon(IModHelper helper)
  {
    _helper = helper;
  }

  public void Dispose()
  {
    ToggleOption(false);
  }

  public void ToggleOption(bool showBirthdayIcon)
  {
    Enabled = showBirthdayIcon;

    _helper.Events.GameLoop.DayStarted -= OnDayStarted;
    _helper.Events.Display.RenderingHud -= OnRenderingHud;
    _helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;

    if (showBirthdayIcon)
    {
      CheckForBirthday();
      _helper.Events.GameLoop.DayStarted += OnDayStarted;
      _helper.Events.Display.RenderingHud += OnRenderingHud;
      _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }
  }

  public void ToggleDisableOnMaxFriendshipOption(bool hideBirthdayIfFullFriendShip)
  {
    HideBirthdayIfFullFriendShip = hideBirthdayIfFullFriendShip;
    ToggleOption(Enabled);
  }
  #endregion


  #region Event subscriptions
  private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    if (e.IsOneSecond)
    {
      CheckForGiftGiven();
    }
  }

  private void OnDayStarted(object? sender, DayStartedEventArgs e)
  {
    CheckForBirthday();
  }

  private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
  {
    if (UIElementUtils.IsRenderingNormally())
    {
      EnqueueBirthdayIcons();
    }
  }
  #endregion


  #region Logic
  private void CheckForGiftGiven()
  {
    List<NPC> npcs = _birthdayNPCs.Value;
    // Iterate from the end so that removing items doesn't affect indices
    for (int i = npcs.Count - 1; i >= 0; i--)
    {
      Friendship? friendship = GetFriendshipWithNPC(npcs[i].Name);
      if (friendship != null && friendship.GiftsToday > 0)
      {
        npcs.RemoveAt(i);
        _birthdayIcons.Value.Clear();
      }
    }
  }

  private void CheckForBirthday()
  {
    _birthdayNPCs.Value.Clear();
    _birthdayIcons.Value.Clear();
    HashSet<string> seen = new();
    foreach (GameLocation? location in Game1.locations)
    {
      foreach (NPC? character in location.characters)
      {
        if (character.isBirthday() && seen.Add(character.Name))
        {
          Friendship? friendship = GetFriendshipWithNPC(character.Name);
          if (friendship != null)
          {
            if (
              HideBirthdayIfFullFriendShip
              && friendship.Points
                >= Utility.GetMaximumHeartsForCharacter(character)
                  * NPC.friendshipPointsPerHeartLevel
            )
            {
              continue;
            }

            _birthdayNPCs.Value.Add(character);
          }
        }
      }
    }

    if (_birthdayNPCs.Value.Count > 0)
    {
      ModEntry.MonitorObject.LogOnce(
        $"ShowBirthdayIcon: birthdays today, npcs=[{string.Join(", ", _birthdayNPCs.Value.Select(n => n.Name))}]",
        LogLevel.Trace
      );
    }
  }

  private static Friendship? GetFriendshipWithNPC(string name)
  {
    try
    {
      if (Game1.player.friendshipData.TryGetValue(name, out Friendship friendship))
      {
        return friendship;
      }

      return null;
    }
    catch (Exception ex)
    {
      ModEntry.MonitorObject.LogOnce(
        $"ShowBirthdayIcon: failed to get friendship data, npc={name}",
        LogLevel.Error
      );
      ModEntry.MonitorObject.Log(ex.ToString());
    }

    return null;
  }

  private static readonly Rectangle BirthdayBackgroundSource = new(228, 409, 16, 16);

  private void EnqueueBirthdayIcons()
  {
    List<NPC> npcs = _birthdayNPCs.Value;
    List<ClickableTextureComponent> icons = _birthdayIcons.Value;
    var scale = 2.9f;

    // Rebuild icon list only when NPC count changes
    if (icons.Count != npcs.Count)
    {
      icons.Clear();
      foreach (NPC npc in npcs)
      {
        icons.Add(
          new ClickableTextureComponent(
            npc.Name,
            Rectangle.Empty,
            null,
            npc.Name,
            npc.Sprite.Texture,
            npc.GetHeadShot(),
            2f
          )
        );
      }
    }

    for (int i = 0; i < npcs.Count; i++)
    {
      int capturedI = i;
      IconHandler.Handler.EnqueueIcon(
        "Birthday",
        (batch, pos) =>
        {
          batch.Draw(
            Game1.mouseCursors,
            new Vector2(pos.X, pos.Y - 3),
            BirthdayBackgroundSource,
            Color.White,
            0.0f,
            Vector2.Zero,
            scale,
            SpriteEffects.None,
            1f
          );

          icons[capturedI].bounds = new Rectangle(
            pos.X - 7,
            pos.Y - 5,
            (int)(16.0 * scale),
            (int)(16.0 * scale)
          );
          icons[capturedI].sourceRect = npcs[capturedI].GetHeadShot();
          icons[capturedI].draw(batch);
        },
        batch =>
        {
          if (icons[capturedI].containsPoint(Game1.getMouseX(), Game1.getMouseY()))
          {
            string hoverText = string.Format(I18n.NpcBirthday(), npcs[capturedI].displayName);
            IClickableMenu.drawHoverText(batch, hoverText, Game1.dialogueFont);
          }
        }
      );
    }
  }
  #endregion
}
