using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.SaveSystem;
using Retinues.Troops.Save;

namespace Retinues.Tests.Cases
{
    public static class LegacySaveContractTests
    {
        // Frozen wire schema shared by stable saves and the V2 reader. Append new IDs;
        // never renumber old ones or regenerate this baseline from the implementation.
        [GameTest("LegacySaveableFieldIdsAndTypesRemainCompatible", "save-contracts", RequiresCampaign = false)]
        public static void LegacySaveableFieldIdsAndTypesRemainCompatible()
        {
            Check(typeof(TroopSaveData),
                "1:StringId:String 2:VanillaStringId:String 3:Name:String 4:Level:Int32 5:IsFemale:Boolean 6:CultureId:String " +
                "7:UpgradeTargets:List<TroopSaveData> 8:EquipmentData:TroopEquipmentData 9:SkillData:TroopSkillData " +
                "10:BodyData:TroopBodySaveData 11:Race:Int32 12:FormationClassOverride:FormationClass " +
                "13:Captain:TroopSaveData 14:IsCaptain:Boolean 15:CaptainEnabled:Boolean 16:IsMariner:Boolean 17:SkillBaseline:Int32");
            Check(typeof(FactionSaveData),
                "1:RetinueElite:TroopSaveData 2:RetinueBasic:TroopSaveData 3:RootElite:TroopSaveData 4:RootBasic:TroopSaveData " +
                "5:MilitiaMelee:TroopSaveData 6:MilitiaMeleeElite:TroopSaveData 7:MilitiaRanged:TroopSaveData " +
                "8:MilitiaRangedElite:TroopSaveData 9:CaravanGuard:TroopSaveData 10:CaravanMaster:TroopSaveData " +
                "11:Villager:TroopSaveData 12:PrisonGuard:TroopSaveData 13:Civilians:List<TroopSaveData> " +
                "14:Bandits:List<TroopSaveData> 15:Heroes:List<TroopSaveData> 16:Mercenaries:List<TroopSaveData> 17:Extras:List<TroopSaveData>");
            Check(typeof(TroopBodySaveData),
                "1:AgeMin:Single 2:AgeMax:Single 3:WeightMin:Single 4:WeightMax:Single 5:BuildMin:Single " +
                "6:BuildMax:Single 7:HeightMin:Single 8:HeightMax:Single");
            Check(typeof(TroopSkillData), "1:Code:String");
            Check(typeof(TroopEquipmentData), "1:Codes:List<String> 2:Civilians:List<Boolean>");
        }

        [GameTest("LegacySaveNamespaceRemainsStable", "save-contracts", RequiresCampaign = false)]
        public static void LegacySaveNamespaceRemainsStable()
        {
            var definition = new SaveDefinitions();
            var baseId = typeof(SaveableTypeDefiner).GetField("_saveBaseId", BindingFlags.Instance | BindingFlags.NonPublic);
            Tests.AssertNotNull(baseId, "The real engine save definer still exposes the expected registration layout.");
            Tests.AssertEqual(90787, (int)baseId.GetValue(definition), "Changing the save namespace makes old saves unreadable.");
            Tests.AssertEqual(0, new TroopSaveData().SkillBaseline, "Saves predating the baseline field must retain its migration sentinel.");
        }

        private static void Check(Type type, string contract)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(f => new { Field = f, Attribute = f.GetCustomAttributesData().SingleOrDefault(a => a.AttributeType == typeof(SaveableFieldAttribute)) })
                .Where(f => f.Attribute != null)
                .Select(f => new { Id = Convert.ToInt32(f.Attribute.ConstructorArguments[0].Value), f.Field }).ToList();
            Tests.AssertEqual(fields.Count, fields.Select(f => f.Id).Distinct().Count(), type.Name + " cannot reuse field IDs.");
            foreach (string entry in contract.Split(' '))
            {
                var parts = entry.Split(':');
                int id = int.Parse(parts[0]);
                var field = fields.SingleOrDefault(f => f.Id == id);
                Tests.AssertNotNull(field, type.Name + " must still read field " + entry);
                Tests.AssertEqual(parts[1], field.Field.Name, type.Name + " field " + id + " keeps its meaning.");
                Tests.AssertEqual(parts[2], TypeName(field.Field.FieldType), type.Name + " field " + id + " keeps its wire type.");
            }
        }

        private static string TypeName(Type type) => !type.IsGenericType ? type.Name :
            type.Name.Split('`')[0] + "<" + string.Join(",", type.GetGenericArguments().Select(TypeName)) + ">";
    }
}
