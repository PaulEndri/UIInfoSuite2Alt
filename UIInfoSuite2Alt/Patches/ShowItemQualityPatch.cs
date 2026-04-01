// Based on Show Item Quality by Jonqora & Bungus
// https://github.com/Jonqora/StardewValleyMods/tree/master/ShowItemQuality
// Licensed under GNU Lesser General Public License v3.0 (LGPL-3.0)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace UIInfoSuite2Alt.Patches;

internal static class ShowItemQualityPatch
{
  public static bool Enabled { get; set; }

  // Whether the standalone ShowItemQuality mod is loaded (we defer to it).
  public static bool ExternalModLoaded { get; private set; }

  public static void Initialize(Harmony harmony, bool showItemQualityLoaded)
  {
    if (showItemQualityLoaded)
    {
      ExternalModLoaded = true;
      ModEntry.MonitorObject.Log(
        "ShowItemQualityPatch: skipped, Show Item Quality mod is installed",
        LogLevel.Info
      );
      return;
    }

    harmony.Patch(
      original: AccessTools.Method(typeof(HUDMessage), nameof(HUDMessage.draw)),
      transpiler: new HarmonyMethod(typeof(ShowItemQualityPatch), nameof(HUDMessageDraw_Transpiler))
    );

    harmony.Patch(
      original: AccessTools.Method(typeof(Game1), nameof(Game1.addHUDMessage)),
      postfix: new HarmonyMethod(typeof(ShowItemQualityPatch), nameof(AddHUDMessage_Postfix))
    );

    ModEntry.MonitorObject.Log("ShowItemQualityPatch: initialized", LogLevel.Trace);
  }

  // Returns the appropriate StackDrawType based on config
  public static StackDrawType GetStackDrawType()
  {
    return Enabled ? StackDrawType.HideButShowQuality : StackDrawType.Hide;
  }

  /// <summary>
  /// Replaces the StackDrawType.Hide (0) argument to drawInMenu() with a call to
  /// GetStackDrawType() so the quality star visibility is toggleable at runtime.
  /// </summary>
  private static IEnumerable<CodeInstruction> HUDMessageDraw_Transpiler(
    IEnumerable<CodeInstruction> instructions
  )
  {
    try
    {
      var codes = new List<CodeInstruction>(instructions);
      MethodInfo getStackDrawType = AccessTools.Method(
        typeof(ShowItemQualityPatch),
        nameof(GetStackDrawType)
      );

      bool patched = false;
      for (int i = 0; i < codes.Count - 1; i++)
      {
        if (
          codes[i].opcode == OpCodes.Ldc_I4_0
          && codes[i + 1].opcode == OpCodes.Callvirt
          && codes[i + 1].operand is MethodInfo method
          && method.Name == "drawInMenu"
        )
        {
          codes[i] = new CodeInstruction(OpCodes.Call, getStackDrawType);
          patched = true;
          ModEntry.MonitorObject.LogOnce(
            "ShowItemQualityPatch: patched StackDrawType in HUDMessage.draw",
            LogLevel.Trace
          );
          break;
        }
      }

      if (!patched)
      {
        ModEntry.MonitorObject.Log(
          "ShowItemQualityPatch: could not find StackDrawType.Hide in HUDMessage.draw, another mod may have already patched it",
          LogLevel.Warn
        );
      }

      return codes;
    }
    catch (Exception ex)
    {
      ModEntry.MonitorObject.Log($"ShowItemQualityPatch: transpiler failed\n{ex}", LogLevel.Error);
      return instructions;
    }
  }

  /// <summary>
  /// When a HUD message stacks with an existing one, replace the old message with the new one
  /// so the displayed quality star matches the most recently picked up item.
  /// </summary>
  private static void AddHUDMessage_Postfix(HUDMessage message)
  {
    if (!Enabled)
    {
      return;
    }

    try
    {
      if (message.type == null && message.whatType == 0)
      {
        return;
      }

      for (int i = 0; i < Game1.hudMessages.Count; i++)
      {
        if (
          message.type != null
          && Game1.hudMessages[i].type != null
          && Game1.hudMessages[i].type!.Equals(message.type)
        )
        {
          message.number = Game1.hudMessages[i].number;
          Game1.hudMessages.RemoveAt(i);
          Game1.hudMessages.Insert(i, message);
          return;
        }
      }
    }
    catch (Exception ex)
    {
      ModEntry.MonitorObject.Log(
        $"ShowItemQualityPatch: addHUDMessage postfix failed\n{ex}",
        LogLevel.Error
      );
    }
  }
}
