using System;
using Retinues.Configuration;
using Retinues.Editor.Events;
using Retinues.Game;
using Retinues.Game.Doctrines;
using Retinues.UI.Services;
using TaleWorlds.Localization;

namespace Retinues.Editor.Controllers.Doctrines
{
    /// <summary>
    /// Provides doctrine data for the editor and implements actions.
    /// </summary>
    public sealed class DoctrinesController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Costs                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static int GetGoldCost(DoctrineDefinition def)
        {
            if (def == null)
                return 0;

            if (!Settings.DoctrinesCostMoney)
                return 0;

            if (def.GoldCost <= 0)
                return 0;

            int cost = (int)
                Math.Round(Math.Max(0, def.GoldCost) * Settings.DoctrineMoneyCostMultiplier);

            return Math.Max(0, cost);
        }

        public static int GetInfluenceCost(DoctrineDefinition def)
        {
            if (def == null)
                return 0;

            if (!Settings.DoctrinesCostInfluence)
                return 0;

            if (def.InfluenceCost <= 0)
                return 0;

            int cost = (int)
                Math.Round(
                    Math.Max(0, def.InfluenceCost) * Settings.DoctrineInfluenceCostMultiplier
                );

            return Math.Max(0, cost);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Actions                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        public static EditorAction<string> Acquire { get; } =
            Action<string>("Acquire")
                .AddCondition(
                    doctrineId => !string.IsNullOrEmpty(doctrineId),
                    L.T("doctrine_select_reason", "Select a doctrine first.")
                )
                .AddCondition(
                    _ => Settings.EnableDoctrines,
                    L.T("doctrines_disabled", "Doctrines are disabled.")
                )
                .AddCondition(
                    doctrineId => DoctrinesAPI.GetState(doctrineId) != DoctrineState.Acquired,
                    L.T(
                        "doctrine_acquire_already_acquired",
                        "This doctrine has already been acquired."
                    )
                )
                .AddCondition(
                    doctrineId => DoctrinesAPI.GetState(doctrineId) == DoctrineState.Unlocked,
                    L.T("doctrine_acquire_unavailable", "This doctrine is still locked.")
                )
                .ExecuteWith(doctrineId =>
                {
                    if (!DoctrinesCatalog.TryGetDoctrine(doctrineId, out var def) || def == null)
                    {
                        Notifications.Message(L.S("doctrine_not_found", "Doctrine not found."));
                        return;
                    }

                    var goldCost = GetGoldCost(def);
                    var influenceCost = GetInfluenceCost(def);

                    var gold = Player.Gold;
                    var inf = Player.Influence;

                    if (gold < goldCost)
                    {
                        Inquiries.Popup(
                            title: L.T("doctrine_not_enough_gold_title", "Not Enough Money"),
                            description: L.T(
                                    "doctrine_not_enough_gold_desc",
                                    "You need {COST} denars to unlock this doctrine."
                                )
                                .SetTextVariable("COST", goldCost),
                            delayUntilOnWorldMap: false
                        );
                        return;
                    }

                    if (inf < influenceCost)
                    {
                        Inquiries.Popup(
                            title: L.T(
                                "doctrine_not_enough_influence_title",
                                "Not Enough Influence"
                            ),
                            description: L.T(
                                    "doctrine_not_enough_influence_desc",
                                    "You need {COST} influence to unlock this doctrine."
                                )
                                .SetTextVariable("COST", influenceCost),
                            delayUntilOnWorldMap: false
                        );
                        return;
                    }

                    var title = L.T("doctrine_acquire_confirm_title", "Acquire Doctrine");

                    TextObject desc;

                    if (goldCost <= 0 && influenceCost <= 0)
                    {
                        desc = L.T("doctrine_acquire_confirm_desc_free", "Acquire {NAME}?")
                            .SetTextVariable("NAME", def.Name);
                    }
                    else if (goldCost > 0 && influenceCost > 0)
                    {
                        desc = L.T(
                                "doctrine_acquire_confirm_desc_both",
                                "Acquire {NAME} for {GOLD} denars and {INF} influence?"
                            )
                            .SetTextVariable("NAME", def.Name)
                            .SetTextVariable("GOLD", goldCost)
                            .SetTextVariable("INF", influenceCost);
                    }
                    else if (goldCost > 0)
                    {
                        desc = L.T(
                                "doctrine_acquire_confirm_desc_gold",
                                "Acquire {NAME} for {GOLD} denars?"
                            )
                            .SetTextVariable("NAME", def.Name)
                            .SetTextVariable("GOLD", goldCost);
                    }
                    else
                    {
                        desc = L.T(
                                "doctrine_acquire_confirm_desc_influence",
                                "Acquire {NAME} for {INF} influence?"
                            )
                            .SetTextVariable("NAME", def.Name)
                            .SetTextVariable("INF", influenceCost);
                    }

                    Inquiries.Popup(
                        title: title,
                        onConfirm: () =>
                        {
                            // Safety net: acquisition still validates in game logic.
                            if (!DoctrinesAPI.TryAcquire(doctrineId, out var error))
                            {
                                Notifications.Message(
                                    error?.ToString()
                                        ?? L.S(
                                            "doctrine_acquire_failed",
                                            "Cannot acquire doctrine."
                                        )
                                );
                                return;
                            }

                            Notifications.Message(
                                L.T("doctrine_acquired_msg", "Acquired doctrine: {NAME}.")
                                    .SetTextVariable("NAME", def.Name)
                            );

                            EventManager.FireBatch(() =>
                            {
                                EventManager.Fire(UIEvent.Doctrine);
                                EventManager.Fire(UIEvent.Page);
                            });
                        },
                        description: desc,
                        pauseGame: true,
                        delayUntilOnWorldMap: false
                    );
                })
                .Fire(UIEvent.Doctrine);
    }
}
