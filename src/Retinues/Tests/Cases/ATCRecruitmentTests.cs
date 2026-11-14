using System;
using System.Collections.Generic;
using HarmonyLib;
using Retinues.Compatibility;
using TaleWorlds.CampaignSystem;

namespace Retinues.Tests.Cases
{
    public static class ATCRecruitmentTests
    {
        [GameTest("ATCConfiguredRootsIncludeUpgradesButRejectAmbiguousRoles", "recruitment", RequiresCampaign = false)]
        public static void ConfiguredRoots()
        {
            using var f = new Europe1700RecruitmentTests.Fixture();
            var basic = f.Troop("atc_basic");
            var elite = f.Troop("atc_elite");
            var child = f.Troop("atc_upgraded");
            AccessTools.PropertySetter(typeof(CharacterObject), "UpgradeTargets").Invoke(basic, new object[] { new[] { child } });
            AccessTools.PropertySetter(typeof(CharacterObject), "UpgradeTargets").Invoke(child, new object[] { new[] { basic } });
            var map = new Dictionary<string, CharacterObject> { ["basic"] = basic, ["elite"] = elite };
            CharacterObject Resolve(string id) => map.TryGetValue(id, out var troop) ? troop : null;
            var config = new CultureConfig();
            config.BasicTroops.Volunteers.Add(new Volunteer { VolunteerID = "basic" });
            config.EliteTroops.Volunteers.Add(new Volunteer { VolunteerID = "elite" });
            ATCRecruitRole Role(CharacterObject t) => ATCRecruitment.Classify(config, t, Resolve, null, f.Basic, f.Elite);
            Tests.AssertEqual(ATCRecruitRole.Basic, Role(basic));
            Tests.AssertEqual(ATCRecruitRole.Basic, Role(child));
            Tests.AssertEqual(ATCRecruitRole.Elite, Role(elite));
            Tests.AssertEqual(ATCRecruitRole.None, Role(f.Ordinary));
            Tests.AssertEqual(ATCRecruitRole.None, Role(null));
            config.EliteTroops.Volunteers.Add(new Volunteer { VolunteerID = "basic" });
            Tests.AssertEqual(ATCRecruitRole.Ambiguous, Role(child));
            config.BasicTroops.Volunteers.Add(new Volunteer { VolunteerID = "missing" });
            Tests.AssertEqual(ATCRecruitRole.None, Role(f.Ordinary), "Missing roots and cycles must not cause a fallback classification.");
        }

        [GameTest("ATCUsesClanThenFactionThenDefaultCultureConfiguration", "recruitment", RequiresCampaign = false)]
        public static void ConfigPrecedence()
        {
            var clanExact = new CultureConfig { CultureID = "vlandia" };
            var clanDefault = new CultureConfig { CultureID = "default" };
            var factionExact = new CultureConfig { CultureID = "vlandia" };
            var factionDefault = new CultureConfig { CultureID = "default" };
            var global = new CultureConfig { CultureID = "vlandia" };
            var clans = new[] { new ClanConfig { ClanID = "player_clan", Cultures = new List<CultureConfig> { clanDefault, clanExact } } };
            var factions = new[] {
                new FactionConfig { FactionID = "vlandia", Cultures = new List<CultureConfig> { factionDefault, factionExact } },
                new FactionConfig { FactionID = "default", Cultures = new List<CultureConfig> { global } }
            };
            object Pick(string clan = "player_clan", string faction = "vlandia", string culture = "vlandia") =>
                ATCRecruitment.SelectCulture(clans, factions, clan, faction, culture);
            Tests.AssertEqual((object)clanExact, Pick());
            Tests.AssertEqual((object)clanDefault, Pick(culture: "aserai"));
            Tests.AssertEqual((object)factionExact, Pick(clan: "other"));
            Tests.AssertEqual((object)factionDefault, Pick(clan: "other", culture: "aserai"));
            Tests.AssertEqual((object)global, Pick(clan: "other", faction: "other"));
            Tests.AssertTrue(Pick(clan: "other", faction: "other", culture: "aserai") == null);
            Tests.AssertEqual((object)clanExact, Pick(), "An existing empty clan pool must not accidentally use a lower-priority faction pool.");
        }

        [GameTest("ATCRespectsPlayerOnlyClanOnlyAndReplacementRoots", "recruitment", RequiresCampaign = false)]
        public static void Restrictions()
        {
            using var f = new Europe1700RecruitmentTests.Fixture();
            var player = f.Troop("player_only");
            var clan = f.Troop("clan_only");
            var substitute = f.Troop("substitute");
            var roots = new Dictionary<string, CharacterObject> { ["player"] = player, ["clan"] = clan, ["substitute"] = substitute };
            var config = new CultureConfig();
            config.BasicTroops.Volunteers.Add(new Volunteer { VolunteerID = "player", PlayerOnly = true, ReplaceWith = "substitute" });
            config.BasicTroops.Volunteers.Add(new Volunteer { VolunteerID = "clan", ClanOnly = true });
            ATCRecruitRole Role(CharacterObject t, string type) => ATCRecruitment.Classify(config, t,
                id => roots.TryGetValue(id, out var root) ? root : null, type, f.Basic, f.Elite);
            Tests.AssertEqual(ATCRecruitRole.Basic, Role(player, null));
            Tests.AssertEqual(ATCRecruitRole.Basic, Role(clan, null));
            Tests.AssertEqual(ATCRecruitRole.None, Role(player, "player_clan"));
            Tests.AssertEqual(ATCRecruitRole.Basic, Role(substitute, "player_clan"));
            Tests.AssertEqual(ATCRecruitRole.Basic, Role(clan, "player_clan"));
            Tests.AssertEqual(ATCRecruitRole.None, Role(clan, "default"));
            Tests.AssertEqual(ATCRecruitRole.Basic, Role(f.Basic, "default"));
            Tests.AssertEqual(ATCRecruitRole.Basic, Role(substitute, "default"));
            f.Active(false);
            Tests.AssertEqual(ATCRecruitRole.None, ATCRecruitment.GetRole(player, null), "No context or active ATC must retain ordinary rules.");
        }

        public sealed class Volunteer
        {
            public string VolunteerID { get; set; }
            public string ReplaceWith { get; set; }
            public bool PlayerOnly { get; set; }
            public bool ClanOnly { get; set; }
        }
        public sealed class Pool { public List<Volunteer> Volunteers { get; set; } = new(); }
        public sealed class CultureConfig
        {
            public string CultureID { get; set; }
            public Pool BasicTroops { get; set; } = new();
            public Pool EliteTroops { get; set; } = new();
        }
        public sealed class ClanConfig
        {
            public string ClanID { get; set; }
            public List<CultureConfig> Cultures { get; set; } = new();
        }
        public sealed class FactionConfig
        {
            public string FactionID { get; set; }
            public List<CultureConfig> Cultures { get; set; } = new();
        }
    }
}
