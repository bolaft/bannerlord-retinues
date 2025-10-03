using System.Collections.Generic;
using Retinues.Core.Features.Xp.Behaviors;
using Retinues.Core.Troops;
using Retinues.Core.Utils;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace Retinues.Core.Safety.Legacy
{
    [SafeClass]
    public sealed class TroopSaveBehavior : CampaignBehaviorBase
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Sync Data                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private List<TroopSaveData> _troops;

        public override void SyncData(IDataStore ds)
        {
            if (ds.IsSaving)
                _troops = null; // Clear reference before saving

            ds.SyncData("Retinues_Troops", ref _troops);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Registration                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public override void RegisterEvents()
        {
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(
                this,
                OnAfterSessionLaunched
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Events                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private void OnAfterSessionLaunched(CampaignGameStarter starter)
        {
            if (_troops is { Count: > 0 })
            {
                foreach (var data in _troops)
                {
                    // Load the troop from save data
                    var troop = TroopLoader.Load(ConvertSaveData(data));

                    // Add experience points to the troop
                    TroopXpBehavior.Add(troop, data.XpPool);
                }

                Log.Info($"Troops migrated: {_troops.Count} roots.");
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Internals                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private Troops.Save.TroopSaveData ConvertSaveData(TroopSaveData data)
        {
            return new Troops.Save.TroopSaveData
            {
                StringId = data.StringId,
                VanillaStringId = data.VanillaStringId,
                Name = data.Name,
                Level = data.Level,
                IsFemale = data.IsFemale,
                SkillCode = data.SkillCode,
                EquipmentCode = data.EquipmentCode,
                UpgradeTargets = data.UpgradeTargets.ConvertAll(ConvertSaveData),
            };
        }
    }

    /* ━━━━━━━ Save Data ━━━━━━ */

    public class TroopSaveData
    {
        [SaveableField(1)]
        public string StringId;

        [SaveableField(2)]
        public string VanillaStringId;

        [SaveableField(3)]
        public string Name;

        [SaveableField(4)]
        public int Level;

        [SaveableField(5)]
        public bool IsFemale;

        [SaveableField(6)]
        public string SkillCode;

        [SaveableField(7)]
        public string EquipmentCode;

        [SaveableField(8)]
        public List<TroopSaveData> UpgradeTargets = [];

        [SaveableField(9)]
        public int XpPool = 0;
    }
}
