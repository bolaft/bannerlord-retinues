#if DEBUG
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Retinues.Utilities;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;

namespace Retinues.Framework.Diagnostics.SaveSystem.Patches
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //            Patch: LoadContext.Load exception           //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Patches LoadContext.Load to capture and report exceptions with full details.
    /// </summary>
    [HarmonyPatch(typeof(LoadContext), nameof(LoadContext.Load))]
    internal static class Patch_LoadContext_Load
    {
        /// <summary>
        /// Transpiles the method IL to log exception.ToString() and report it.
        /// </summary>
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = new List<CodeInstruction>(instructions);

            var debugType = typeof(TaleWorlds.Library.Debug);

            static bool IsDebugPrint(MethodInfo mi)
            {
                if (mi == null)
                    return false;

                if (mi.DeclaringType != typeof(TaleWorlds.Library.Debug))
                    return false;

                if (mi.Name != "Print")
                    return false;

                var ps = mi.GetParameters();
                return ps.Length >= 1 && ps[0].ParameterType == typeof(string);
            }

            var getMessage = AccessTools.PropertyGetter(
                typeof(Exception),
                nameof(Exception.Message)
            );
            var toString = AccessTools.Method(
                typeof(Exception),
                nameof(Exception.ToString),
                Type.EmptyTypes
            );
            var report = AccessTools.Method(
                typeof(SaveSystemDiagnostics),
                nameof(SaveSystemDiagnostics.ReportLoadContextException)
            );

            // We patch the first Debug.Print(...) in the catch path that prints ex.Message.
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].opcode != OpCodes.Call)
                    continue;

                if (list[i].operand is not MethodInfo mi || !IsDebugPrint(mi))
                    continue;

                // Search backwards a bit for Exception.get_Message that produced the first argument.
                // (Small window avoids accidental matches)
                var start = Math.Max(0, i - 25);
                for (int j = i - 1; j >= start; j--)
                {
                    var ins = list[j];
                    if (ins.opcode != OpCodes.Callvirt)
                        continue;

                    if (ins.operand is not MethodInfo called || called != getMessage)
                        continue;

                    // Replace get_Message with ToString while preserving EH blocks + labels
                    var replacement = new CodeInstruction(OpCodes.Callvirt, toString);

                    if (ins.labels != null && ins.labels.Count > 0)
                        replacement.labels.AddRange(ins.labels);

                    if (ins.blocks != null && ins.blocks.Count > 0)
                        replacement.blocks.AddRange(ins.blocks);

                    list[j] = replacement;

                    // Insert our logger right after ToString:
                    // stack: [string]
                    // dup -> [string][string]
                    // call report -> consumes one, leaves one for Debug.Print
                    list.Insert(j + 1, new CodeInstruction(OpCodes.Dup));
                    list.Insert(j + 2, new CodeInstruction(OpCodes.Call, report));

                    return list;
                }
            }

            // If we didn't match, return original IL
            return list;
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //          Patch: SaveManager.Load call boundary         //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Instrument SaveManager.Load to track the save name and detect boundary failures.
    /// </summary>
    [HarmonyPatch(
        typeof(SaveManager),
        nameof(SaveManager.Load),
        new[] { typeof(string), typeof(ISaveDriver), typeof(bool) }
    )]
    internal static class Patch_SaveManager_Load
    {
        /// <summary>
        /// Record the name of the save before loading.
        /// </summary>
        static void Prefix(string saveName)
        {
            SaveSystemDiagnostics.LastSaveName = saveName;
            SaveSystemDiagnostics.LastLoadContextException = null;
        }

        /// <summary>
        /// Inspect the load result and emit diagnostics if loading failed.
        /// </summary>
        static void Postfix(string saveName, LoadResult __result)
        {
            if (__result == null || __result.Successful)
                return;

            // SaveManager.Load returns a failed LoadResult when LoadContext.Load returns false.
            // Errors are often useless; still print them.
            if (__result.Errors != null && __result.Errors.Length > 0)
            {
                for (int i = 0; i < __result.Errors.Length; i++)
                    Log.Error(
                        $"SaveLoad: SaveManager.Load failed ({saveName}) error[{i}]={__result.Errors[i]?.Message}"
                    );
            }
            else
            {
                Log.Error($"SaveLoad: SaveManager.Load failed ({saveName}) with no error array.");
            }

            if (!string.IsNullOrEmpty(SaveSystemDiagnostics.LastLoadContextException))
            {
                Log.Error(
                    $"SaveLoad: last LoadContext exception for '{saveName}' was already logged above."
                );
            }
            else
            {
                Log.Error(
                    $"SaveLoad: LoadContext did not report an exception string for '{saveName}'."
                );
            }
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
    //           Patch: MBSaveLoad.LoadSaveGameData           //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

    /// <summary>
    /// Track MBSaveLoad.LoadSaveGameData invocations to provide context on failures.
    /// </summary>
    [HarmonyPatch(typeof(MBSaveLoad), nameof(MBSaveLoad.LoadSaveGameData))]
    internal static class Patch_MBSaveLoad_LoadSaveGameData
    {
        /// <summary>
        /// Record the save name before attempting to load.
        /// </summary>
        static void Prefix(string saveName)
        {
            SaveSystemDiagnostics.LastSaveName = saveName;
        }

        /// <summary>
        /// Log an error if MBSaveLoad failed to return a LoadResult.
        /// </summary>
        static void Postfix(string saveName, LoadResult __result)
        {
            // MBSaveLoad returns null on failure, so __result will be null in the case you care about.
            if (__result != null)
                return;

            var extra = string.IsNullOrEmpty(SaveSystemDiagnostics.LastLoadContextException)
                ? "(no LoadContext exception captured)"
                : "(LoadContext exception captured; see earlier SaveLoad: LoadContext.Load failed log)";

            Log.Error($"SaveLoad: MBSaveLoad.LoadSaveGameData failed for '{saveName}' {extra}");
        }
    }
}
#endif
