using System;
using HarmonyLib;
using BepInEx.Logging;
using ValheimVehicles.Constants;

namespace ValheimVehicles.Patches;

/// <summary>
/// Suppresses benign internal Unity physics warnings:
/// "BoxCollider does not support negative scale or size. The effective box size has been forced positive and is likely to give unexpected collision geometry..."
/// When pieces are destroyed (such as breaking the steering wheel), scale inversions or recalculations can prompt Unity to emit this warning.
/// Since it does not affect gameplay or collision behavior, this filter prevents it from cluttering the console or log file.
/// </summary>
public static class UnityLogWarningSuppressPatch
{
  public static void Apply(Harmony harmony)
  {
    try
    {
      // 1. Try patching UnityLogSource.OnUnityLogMessageReceived if present in BepInEx
      var unityLogSourceType = AccessTools.TypeByName("BepInEx.Logging.UnityLogSource");
      if (unityLogSourceType != null)
      {
        var targetMethod = AccessTools.Method(unityLogSourceType, "OnUnityLogMessageReceived");
        if (targetMethod != null)
        {
          var prefix = AccessTools.Method(typeof(UnityLogWarningSuppressPatch), nameof(UnityLogMessagePrefix));
          harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefix));
        }
      }

      // 2. Try patching ConsoleLogListener and DiskLogListener as fallback
      var prefixLogEvent = AccessTools.Method(typeof(UnityLogWarningSuppressPatch), nameof(LogEventPrefix));
      var consoleListenerType = AccessTools.TypeByName("BepInEx.Logging.ConsoleLogListener");
      if (consoleListenerType != null)
      {
        var logEvent = AccessTools.Method(consoleListenerType, "LogEvent", new[] { typeof(object), typeof(LogEventArgs) });
        if (logEvent != null)
        {
          harmony.Patch(logEvent, prefix: new HarmonyMethod(prefixLogEvent));
        }
      }

      var diskListenerType = AccessTools.TypeByName("BepInEx.Logging.DiskLogListener");
      if (diskListenerType != null)
      {
        var logEvent = AccessTools.Method(diskListenerType, "LogEvent", new[] { typeof(object), typeof(LogEventArgs) });
        if (logEvent != null)
        {
          harmony.Patch(logEvent, prefix: new HarmonyMethod(prefixLogEvent));
        }
      }
    }
    catch (Exception ex)
    {
      if (ModEnvironment.IsDebug)
      {
        UnityEngine.Debug.LogWarning($"[UnityLogWarningSuppressPatch] Could not attach log filter: {ex.Message}");
      }
    }
  }

  public static bool UnityLogMessagePrefix(string message)
  {
    if (!string.IsNullOrEmpty(message) && message.IndexOf("BoxCollider does not support negative scale or size", StringComparison.OrdinalIgnoreCase) >= 0)
    {
      return false; // Suppress benign BoxCollider warning
    }
    return true;
  }

  public static bool LogEventPrefix(object sender, LogEventArgs eventArgs)
  {
    if (eventArgs?.Data != null)
    {
      var str = eventArgs.Data.ToString();
      if (!string.IsNullOrEmpty(str) && str.IndexOf("BoxCollider does not support negative scale or size", StringComparison.OrdinalIgnoreCase) >= 0)
      {
        return false; // Suppress benign BoxCollider warning
      }
    }
    return true;
  }
}
