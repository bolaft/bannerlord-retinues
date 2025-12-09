using System.Collections.Generic;
using Retinues.Behaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace Retinues.Model
{
    /// <summary>
    /// Campaign behavior that persists model attributes registered via MAttribute.
    /// </summary>
    public sealed class MBehavior : BaseCampaignBehavior
    {
        public MBehavior()
        {
            // Fresh state per campaign.
            MAttributePersistence.Reset();
        }

        public override bool IsEnabled => true;

        public override void RegisterEvents()
        {
            // No events needed, this behavior only participates in save/load.
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (!IsEnabled)
                return;

            MAttributePersistence.Sync(dataStore);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Save definition helpers                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Called by BehaviorManager.DefineClassTypes via reflection.
        /// Registers the MAttributePersistence.Data class so the save system
        /// knows how to serialize our attribute store.
        /// </summary>
        public static void DefineClassTypes(SaveableTypeDefiner _)
        {
            // Local ID within BehaviorManager's base range.
            // Global type ID will be 070_992 + 001.
            BehaviorManager.RegisterClassDefinition(typeof(MAttributePersistence.Data), 001);
        }

        /// <summary>
        /// Called by BehaviorManager.DefineContainerDefinitions via reflection.
        /// Registers container types used by MAttributePersistence.Data.
        /// </summary>
        public static void DefineContainerDefinitions(SaveableTypeDefiner _)
        {
            BehaviorManager.RegisterContainerDefinition(typeof(Dictionary<string, int>));
            BehaviorManager.RegisterContainerDefinition(typeof(Dictionary<string, bool>));
            BehaviorManager.RegisterContainerDefinition(typeof(Dictionary<string, float>));
            BehaviorManager.RegisterContainerDefinition(typeof(Dictionary<string, string>));
            BehaviorManager.RegisterContainerDefinition(
                typeof(Dictionary<string, Dictionary<string, int>>)
            );
        }
    }
}
