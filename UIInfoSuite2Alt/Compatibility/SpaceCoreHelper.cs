using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace UIInfoSuite2Alt.Compatibility;

public class CachedCustomSkillInfo
{
  public Texture2D Icon { get; }
  public Color BarColor { get; }
  public int[] ExperienceCurve { get; }
  public string DisplayName { get; }

  public CachedCustomSkillInfo(Texture2D icon, Color barColor, int[] experienceCurve, string displayName)
  {
    Icon = icon;
    BarColor = barColor;
    ExperienceCurve = experienceCurve;
    DisplayName = displayName;
  }
}

public static class SpaceCoreHelper
{
  private static readonly Color DefaultBarColor = new(148, 103, 198, 0.63f);

  private static readonly Dictionary<string, CachedCustomSkillInfo> SkillCache = new();
  private static MethodInfo? _getSkillMethod;
  private static bool _reflectionAttempted;

  public static CachedCustomSkillInfo GetSkillInfo(ISpaceCoreApi api, string skillId)
  {
    if (SkillCache.TryGetValue(skillId, out CachedCustomSkillInfo? cached))
    {
      return cached;
    }

    Texture2D icon = api.GetSkillPageIconForCustomSkill(skillId);
    string displayName = api.GetDisplayNameOfCustomSkill(skillId);

    Color barColor = DefaultBarColor;
    int[] experienceCurve = Array.Empty<int>();

    // Reflection to get ExperienceBarColor and ExperienceCurve from internal Skill object
    object? skillObject = GetSkillObject(skillId);
    if (skillObject != null)
    {
      try
      {
        Type skillType = skillObject.GetType();

        PropertyInfo? colorProp = skillType.GetProperty("ExperienceBarColor");
        if (colorProp?.GetValue(skillObject) is Color color)
        {
          barColor = color;
        }

        PropertyInfo? curveProp = skillType.GetProperty("ExperienceCurve");
        if (curveProp?.GetValue(skillObject) is int[] curve)
        {
          experienceCurve = curve;
        }
      }
      catch (Exception ex)
      {
        ModEntry.MonitorObject.Log(
          $"SpaceCore reflection failed for skill '{skillId}': {ex.Message}",
          LogLevel.Warn
        );
      }
    }

    var info = new CachedCustomSkillInfo(icon, barColor, experienceCurve, displayName);

    SkillCache[skillId] = info;
    return info;
  }

  public static int GetExperienceRequiredForLevel(CachedCustomSkillInfo info, int level)
  {
    if (level < 0)
    {
      return 0;
    }

    if (level < info.ExperienceCurve.Length)
    {
      return info.ExperienceCurve[level];
    }

    // Level beyond curve = maxed
    return -1;
  }

  public static void ClearCache()
  {
    SkillCache.Clear();
  }

  private static object? GetSkillObject(string skillId)
  {
    if (!_reflectionAttempted)
    {
      _reflectionAttempted = true;
      try
      {
        Type? skillsType = AccessTools.TypeByName("SpaceCore.Skills");
        _getSkillMethod = skillsType?.GetMethod(
          "GetSkill",
          BindingFlags.Public | BindingFlags.Static,
          null,
          new[] { typeof(string) },
          null
        );
      }
      catch (Exception ex)
      {
        ModEntry.MonitorObject.Log(
          $"SpaceCore Skills type reflection failed: {ex.Message}",
          LogLevel.Warn
        );
      }
    }

    if (_getSkillMethod == null)
    {
      return null;
    }

    try
    {
      return _getSkillMethod.Invoke(null, new object[] { skillId });
    }
    catch
    {
      return null;
    }
  }
}
