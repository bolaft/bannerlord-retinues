using HarmonyLib;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Retinues.Behaviors.Troops.Patches
{
    /// <summary>
    /// Protects the custom troop stubs from the engine's load-time non-ready sweep.
    ///
    /// During campaign load the engine calls MBObjectManager.UnregisterNonReadyObjects, which
    /// unregisters every object whose IsReady flag is false. A stub instance materialized from
    /// save data self-registers before the module XMLs run, and if its XML initialization does
    /// not complete that session it is still referenced by live rosters when the sweep
    /// unregisters it. The next save then writes it by value with IsRegistered=false, and the
    /// save after that materializes a floating null-name twin that crashes wage/food
    /// calculations — an unloadable save.
    ///
    /// The stubs are a fixed XML-backed pool that always exists, so they are never legitimate
    /// sweep targets: mark them ready before the sweep runs.
    /// </summary>
    [HarmonyPatch(typeof(MBObjectManager), nameof(MBObjectManager.UnregisterNonReadyObjects))]
    internal static class StubReadyPatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            try
            {
                var manager = MBObjectManager.Instance;
                if (manager == null)
                    return;

                var characters = manager.GetObjectTypeList<CharacterObject>();
                if (characters == null)
                    return;

                int protectedCount = 0;

                for (int i = 0; i < characters.Count; i++)
                {
                    var co = characters[i];
                    if (co == null || co.IsReady)
                        continue;

                    var id = co.StringId;
                    if (
                        string.IsNullOrEmpty(id)
                        || !id.StartsWith(
                            WCharacter.CustomTroopPrefix,
                            System.StringComparison.Ordinal
                        )
                    )
                        continue;

                    co.IsReady = true;
                    protectedCount++;
                }

                if (protectedCount > 0)
                    Log.Warning(
                        $"Marked {protectedCount} custom troop stub(s) ready before the "
                            + "non-ready sweep; they would otherwise have been unregistered "
                            + "while still referenced by rosters."
                    );
            }
            catch (System.Exception e)
            {
                Log.Exception(e, "StubReadyPatch failed.");
            }
        }
    }
}
