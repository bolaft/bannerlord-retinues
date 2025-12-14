// using System;
// using System.Collections.Generic;
// using Retinues.Game;
// using Retinues.Game.Wrappers;
// using Retinues.Utils;
// using TaleWorlds.Library;

// namespace Retinues.Features.Swaps
// {
//     [SafeClass]
//     public static class PartySwapCheats
//     {
//         // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
//         //                        Commands                        //
//         // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

//         /// <summary>
//         /// Applies the hero-safe troop swap to the main party.
//         /// Usage:
//         ///   retinues.swap_main_party_hero_safe
//         ///   retinues.swap_main_party_hero_safe members
//         ///   retinues.swap_main_party_hero_safe prisoners
//         ///   retinues.swap_main_party_hero_safe all
//         /// </summary>
//         [CommandLineFunctionality.CommandLineArgumentFunction(
//             "swap_main_party_hero_safe",
//             "retinues"
//         )]
//         public static string SwapMainPartyHeroSafe(List<string> args)
//         {
//             try
//             {
//                 var party = Player.Party;
//                 if (party?.Base == null)
//                     return "Main party not available (campaign not started?).";

//                 var faction = Player.Clan;

//                 bool doMembers = true;
//                 bool doPrisoners = false;

//                 if (args != null && args.Count > 0)
//                 {
//                     var mode = (args[0] ?? "").Trim().ToLowerInvariant();
//                     if (mode is "help" or "-h" or "--help" or "?")
//                         return "Usage: retinues.swap_main_party_hero_safe [members|prisoners|all]";

//                     if (mode is "all" or "both")
//                     {
//                         doMembers = true;
//                         doPrisoners = true;
//                     }
//                     else if (mode is "members" or "m")
//                     {
//                         doMembers = true;
//                         doPrisoners = false;
//                     }
//                     else if (mode is "prisoners" or "p")
//                     {
//                         doMembers = false;
//                         doPrisoners = true;
//                     }
//                     else
//                     {
//                         return "Invalid mode. Usage: retinues.swap_main_party_hero_safe [members|prisoners|all]";
//                     }
//                 }

//                 static string BeforeAfter(WRoster r, Action apply)
//                 {
//                     if (r == null)
//                         return "<null roster>";

//                     int beforeTotal = r.HealthyCount;
//                     int beforeCustom = r.CustomCount;
//                     int beforeHeroes = r.HeroCount;

//                     apply?.Invoke();

//                     int afterTotal = r.HealthyCount;
//                     int afterCustom = r.CustomCount;
//                     int afterHeroes = r.HeroCount;

//                     return $"total {beforeTotal}->{afterTotal}, custom {beforeCustom}->{afterCustom}, heroes {beforeHeroes}->{afterHeroes}, customRatio {r.CustomRatio * 100f:0.#}%";
//                 }

//                 var sb = new System.Text.StringBuilder();
//                 sb.AppendLine($"Main party: {party.Name} | faction: {faction.Name}");

//                 if (doMembers)
//                 {
//                     var roster = party.MemberRoster;
//                     sb.AppendLine(
//                         "Members:   "
//                             + BeforeAfter(roster, () => roster.SwapTroopsPreservingHeroes(faction))
//                     );
//                 }

//                 if (doPrisoners)
//                 {
//                     var roster = party.PrisonRoster;
//                     if (roster == null)
//                         sb.AppendLine("Prisoners: <none>");
//                     else
//                         sb.AppendLine(
//                             "Prisoners: "
//                                 + BeforeAfter(
//                                     roster,
//                                     () => roster.SwapTroopsPreservingHeroes(faction)
//                                 )
//                         );
//                 }

//                 Log.Info("Cheat: swap_main_party_hero_safe executed.");
//                 return sb.ToString().TrimEnd();
//             }
//             catch (Exception ex)
//             {
//                 Log.Exception(ex, "swap_main_party_hero_safe failed");
//                 return $"swap_main_party_hero_safe failed: {ex.GetType().Name}: {ex.Message}";
//             }
//         }
//     }
// }
