using Retinues.GUI.Services;

namespace Retinues.Behaviors.Doctrines.Catalogs
{
    /// <summary>
    /// Feat definitions for the Troops category.
    /// </summary>
    public static partial class FeatCatalog
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Stalwart Militia                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData SM_WatchersOnTheWalls = new()
        {
            Id = "feat_trp_watchers_on_the_walls",
            Name = L.T("feat_trp_watchers_on_the_walls_name", "Watchers on the Walls"),
            Description = L.T(
                "feat_trp_watchers_on_the_walls_desc",
                "Raise the militia value of a fief to {TARGET}."
            ),
            Target = 400,
            Worth = 55,
            Repeatable = false,
        };

        public static FeatData SM_TheyShallNotPass = new()
        {
            Id = "feat_trp_they_shall_not_pass",
            Name = L.T("feat_trp_they_shall_not_pass_name", "They Shall Not Pass"),
            Description = L.T(
                "feat_trp_they_shall_not_pass_desc",
                "Personally slay {TARGET} assailants during a siege defense."
            ),
            Target = 50,
            Worth = 30,
            Repeatable = false,
        };

        public static FeatData SM_DefenderOfTheCity = new()
        {
            Id = "feat_trp_defender_of_the_city",
            Name = L.T("feat_trp_defender_of_the_city_name", "Defender of the City"),
            Description = L.T(
                "feat_trp_defender_of_the_city_desc",
                "Defend a city against a besieging army."
            ),
            Target = 1,
            Worth = 15,
            Repeatable = true,
        };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Road Wardens                      //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData RW_TradeNetwork = new()
        {
            Id = "feat_trp_trade_network",
            Name = L.T("feat_trp_trade_network_name", "Trade Network"),
            Description = L.T(
                "feat_trp_trade_network_desc",
                "Own {TARGET} caravans at the same time."
            ),
            Target = 3,
            Worth = 40,
            Repeatable = false,
        };

        public static FeatData RW_BanditScourge = new()
        {
            Id = "feat_trp_bandit_scourge",
            Name = L.T("feat_trp_bandit_scourge_name", "Bandit Scourge"),
            Description = L.T("feat_trp_bandit_scourge_desc", "Clear a bandit hideout."),
            Target = 1,
            Worth = 10,
            Repeatable = true,
        };

        public static FeatData RW_MerchantsFavor = new()
        {
            Id = "feat_trp_merchants_favor",
            Name = L.T("feat_trp_merchants_favor_name", "Merchant's Favor"),
            Description = L.T(
                "feat_trp_merchants_favor_desc",
                "Complete a quest for a town merchant notable."
            ),
            Target = 10,
            Worth = 10,
            Repeatable = true,
        };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Armed Peasantry                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData AP_ShieldOfThePeople = new()
        {
            Id = "feat_trp_shield_of_the_people",
            Name = L.T("feat_trp_shield_of_the_people_name", "Shield of the People"),
            Description = L.T(
                "feat_trp_shield_of_the_people_desc",
                "Defend a village against an enemy raid."
            ),
            Target = 1,
            Worth = 50,
            Repeatable = false,
        };

        public static FeatData AP_HeadmansHelp = new()
        {
            Id = "feat_trp_headmans_help",
            Name = L.T("feat_trp_headmans_help_name", "Headman's Help"),
            Description = L.T(
                "feat_trp_headmans_help_desc",
                "Complete a quest for a village headman."
            ),
            Target = 1,
            Worth = 10,
            Repeatable = true,
        };

        public static FeatData AP_LandownersRequest = new()
        {
            Id = "feat_trp_landowners_request",
            Name = L.T("feat_trp_landowners_request_name", "Landowner's Request"),
            Description = L.T(
                "feat_trp_landowners_request_desc",
                "Complete a quest for a village landowner."
            ),
            Target = 1,
            Worth = 10,
            Repeatable = true,
        };

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Captains                        //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static FeatData CA_WarriorClass = new()
        {
            Id = "feat_trp_warrior_class",
            Name = L.T("feat_trp_warrior_class_name", "Warrior Class"),
            Description = L.T(
                "feat_trp_warrior_class_desc",
                "Max out the skills of a T6 elite troop."
            ),
            Target = 1,
            Worth = 40,
            Repeatable = false,
        };

        public static FeatData CA_Veterans = new()
        {
            Id = "feat_trp_veterans",
            Name = L.T("feat_trp_veterans_name", "Veterans"),
            Description = L.T("feat_trp_veterans_desc", "Max out the skills of a T5 basic troop."),
            Target = 1,
            Worth = 40,
            Repeatable = false,
        };

        public static FeatData CA_MeritoriousService = new()
        {
            Id = "feat_trp_meritorious_service",
            Name = L.T("feat_trp_meritorious_service_name", "Meritorious Service"),
            Description = L.T(
                "feat_trp_meritorious_service_desc",
                "Promote {TARGET} faction troops."
            ),
            Target = 100,
            Worth = 10,
            Repeatable = true,
        };
    }
}
