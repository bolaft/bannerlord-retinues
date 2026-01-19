using Retinues.Behaviors.Doctrines.Definitions;
using Retinues.Configuration;
using Retinues.Domain;
using Retinues.Editor.Events;
using Retinues.Editor.MVC.Shared.Controllers;
using Retinues.Framework.Behaviors;
using Retinues.Interface.Services;
using TaleWorlds.Localization;

namespace Retinues.Editor.MVC.Pages.Doctrines.Controllers
{
    /// <summary>
    /// Provides doctrine data and actions for the editor UI.
    /// </summary>
    public sealed class DoctrinesController : BaseController
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                        Actions                         //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Attempts to acquire the specified doctrine after validating costs and prerequisites.
        /// </summary>
        public static ControllerAction<Doctrine> Acquire { get; } =
            Action<Doctrine>("Acquire")
                .AddCondition(
                    _ => Settings.EnableDoctrines,
                    L.T("doctrines_disabled", "Doctrines are disabled.")
                )
                .AddCondition(
                    doctrine => !doctrine.IsAcquired,
                    L.T(
                        "doctrine_acquire_already_acquired",
                        "This doctrine has already been acquired."
                    )
                )
                .AddCondition(
                    doctrine => doctrine.IsUnlocked,
                    L.T("doctrine_acquire_unavailable", "This doctrine is still locked.")
                )
                .ExecuteWith(doctrine =>
                {
                    var gold = Player.Gold;
                    var inf = Player.Influence;

                    if (gold < doctrine.MoneyCost)
                    {
                        Inquiries.Popup(
                            title: L.T("doctrine_not_enough_gold_title", "Not Enough Money"),
                            description: L.T(
                                    "doctrine_not_enough_gold_desc",
                                    "You need {COST} denars to unlock this doctrine."
                                )
                                .SetTextVariable("COST", doctrine.MoneyCost),
                            delayUntilOnWorldMap: false
                        );
                        return;
                    }

                    if (inf < doctrine.InfluenceCost)
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
                                .SetTextVariable("COST", doctrine.InfluenceCost),
                            delayUntilOnWorldMap: false
                        );
                        return;
                    }

                    var title = L.T("doctrine_acquire_confirm_title", "Acquire Doctrine");

                    TextObject desc;

                    if (doctrine.MoneyCost <= 0 && doctrine.InfluenceCost <= 0)
                    {
                        desc = L.T("doctrine_acquire_confirm_desc_free", "Acquire {NAME}?")
                            .SetTextVariable("NAME", doctrine.Name);
                    }
                    else if (doctrine.MoneyCost > 0 && doctrine.InfluenceCost > 0)
                    {
                        desc = L.T(
                                "doctrine_acquire_confirm_desc_both",
                                "Acquire {NAME} for {GOLD} denars and {INF} influence?"
                            )
                            .SetTextVariable("NAME", doctrine.Name)
                            .SetTextVariable("GOLD", doctrine.MoneyCost)
                            .SetTextVariable("INF", doctrine.InfluenceCost);
                    }
                    else if (doctrine.MoneyCost > 0)
                    {
                        desc = L.T(
                                "doctrine_acquire_confirm_desc_gold",
                                "Acquire {NAME} for {GOLD} denars?"
                            )
                            .SetTextVariable("NAME", doctrine.Name)
                            .SetTextVariable("GOLD", doctrine.MoneyCost);
                    }
                    else
                    {
                        desc = L.T(
                                "doctrine_acquire_confirm_desc_influence",
                                "Acquire {NAME} for {INF} influence?"
                            )
                            .SetTextVariable("NAME", doctrine.Name)
                            .SetTextVariable("INF", doctrine.InfluenceCost);
                    }

                    Inquiries.Popup(
                        title: title,
                        onConfirm: () =>
                        {
                            // Safety net: acquisition still validates in game logic.
                            if (
                                !Player.TrySpendGold(doctrine.MoneyCost)
                                || !Player.TrySpendInfluence(doctrine.InfluenceCost)
                            )
                            {
                                Notifications.Message(
                                    L.S("doctrine_acquire_failed", "Cannot acquire doctrine.")
                                );
                                return;
                            }

                            doctrine.IsAcquired = true;
                            CustomEvents.FireDoctrineAcquired(doctrine);

                            Notifications.Message(
                                L.T("doctrine_acquired_msg", "Acquired doctrine: {NAME}.")
                                    .SetTextVariable("NAME", doctrine.Name)
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
