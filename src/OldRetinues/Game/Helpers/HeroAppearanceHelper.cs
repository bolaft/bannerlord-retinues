using System;
using HarmonyLib;
using Helpers;
using Retinues.Utils;
using SandBox.GauntletUI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace OldRetinues.Game.Helpers
{
    [HarmonyPatch(typeof(GauntletBarberScreen))]
    internal static class GauntletBarberScreenPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnFinalize")]
        private static void OnFinalizePostfix()
        {
            Log.Debug(
                "[HeroAppearanceHelper] GauntletBarberScreen.OnFinalize -> closing barber session (if any)."
            );
            HeroAppearanceHelper.OnBarberClosed();
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnFrameTick")]
        private static void OnFrameTickPostfix(GauntletBarberScreen __instance, float dt)
        {
            try
            {
                if (!HeroAppearanceHelper.HasActiveSession)
                    return;

                var handler = __instance.Handler;
                HeroAppearanceHelper.PrimeFaceGen(handler);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }
    }

    /// <summary>
    /// Opens the vanilla barber / FaceGen screen and redirects changes
    /// from MainHero to an arbitrary target hero.
    /// </summary>
    [SafeClass]
    public static class HeroAppearanceHelper
    {
        private static Hero _currentTarget;

        private static BodyProperties _savedMainBody;
        private static bool _hasSavedMainBody;

        private static bool _savedMainIsFemale;
        private static bool _hasSavedMainGender;

        public static bool HasActiveSession => _currentTarget != null;

        private static bool _needsPrime;
        private static bool _hasPrimed;

        private static Action _onClosed;

        /// <summary>
        /// Opens the appearance editor for the given hero.
        /// </summary>
        public static void OpenForHero(Hero hero, Action onClosed = null)
        {
            if (hero == null)
            {
                Log.Warn("[HeroAppearanceHelper] Tried to open appearance editor for null hero.");
                return;
            }

            if (
                TaleWorlds.Core.Game.Current == null
                || TaleWorlds.Core.Game.Current.GameStateManager == null
            )
            {
                Log.Warn(
                    "[HeroAppearanceHelper] Game or GameStateManager is null; cannot open appearance editor."
                );
                return;
            }

            if (Campaign.Current == null || !Campaign.Current.IsFaceGenEnabled)
            {
                Log.Warn("[HeroAppearanceHelper] FaceGen is disabled in this campaign.");
                return;
            }

            // Only one redirected session at a time.
            if (_currentTarget != null)
            {
                Log.Warn(
                    "[HeroAppearanceHelper] A barber session is already active; ignoring new request."
                );
                return;
            }

            try
            {
                _currentTarget = hero;
                _onClosed = onClosed;

                // Save main hero body + gender
                _savedMainBody = Hero.MainHero.BodyProperties;
                _hasSavedMainBody = true;

                _savedMainIsFemale = GetHeroIsFemale(Hero.MainHero);
                _hasSavedMainGender = true;

                _needsPrime = true;
                _hasPrimed = false;

                // Apply target gender to MainHero so FaceGen uses correct mesh
                var targetIsFemale = GetHeroIsFemale(hero);
                SetHeroIsFemale(Hero.MainHero, targetIsFemale);

                // Copy target hero's body onto MainHero so the barber shows the correct face
                var targetBody = hero.BodyProperties;
                ApplyBodyPropertiesToHero(Hero.MainHero, targetBody);

                Log.Debug(
                    $"[HeroAppearanceHelper] Session started for hero '{hero.Name?.ToString() ?? hero.StringId}'."
                );

                // Open vanilla barber screen for MainHero, as MapScreen does
                IFaceGeneratorCustomFilter filter = CharacterHelper.GetFaceGeneratorFilter();

                var barberState =
                    TaleWorlds.Core.Game.Current.GameStateManager.CreateState<BarberState>(
                        [Hero.MainHero.CharacterObject, filter]
                    );

                GameStateManager.Current.PushState(barberState);

                Log.Debug(
                    $"[HeroAppearanceHelper] Barber screen opened for target hero '{hero.Name?.ToString() ?? hero.StringId}'."
                );
            }
            catch (Exception ex)
            {
                Log.Error(
                    $"[HeroAppearanceHelper] Failed to open appearance editor. Exception: {ex}"
                );
                ClearSession(restoreMain: true);
            }
        }

        /// <summary>
        /// Primes the FaceGen handler by forcing a model spawn.
        /// </summary>
        internal static void PrimeFaceGen(IFaceGeneratorHandler handler)
        {
            if (!_needsPrime || _hasPrimed || handler == null)
                return;

            try
            {
                // This is the "fake first click" that forces the model to spawn.
                handler.DefaultFace();
                handler.ChangeToFaceCamera();

                _hasPrimed = true;
                _needsPrime = false;

                Log.Debug("[HeroAppearanceHelper] PrimeFaceGen: DefaultFace + camera/dress.");
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>
        /// Called from GauntletBarberScreen.OnExit postfix.
        /// </summary>
        public static void OnBarberClosed()
        {
            if (_currentTarget == null)
            {
                Log.Debug(
                    "[HeroAppearanceHelper] OnBarberClosed called with no active session (ignored)."
                );
                return;
            }

            Log.Debug(
                $"[HeroAppearanceHelper] OnBarberClosed for '{_currentTarget.Name?.ToString() ?? _currentTarget.StringId}'."
            );

            try
            {
                // Whatever FaceGen changed is now on MainHero
                var editedBody = Hero.MainHero.BodyProperties;
                var editedIsFemale = GetHeroIsFemale(Hero.MainHero);

                // Apply edited body + gender to the target hero
                ApplyBodyPropertiesToHero(_currentTarget, editedBody);
                SetHeroIsFemale(_currentTarget, editedIsFemale);

                Log.Debug(
                    $"[HeroAppearanceHelper] Applied edited body/gender to hero '{_currentTarget.Name?.ToString() ?? _currentTarget.StringId}'."
                );
            }
            catch (Exception ex)
            {
                Log.Error($"[HeroAppearanceHelper] Error while applying edited body/gender: {ex}");
            }
            finally
            {
                var callback = _onClosed;
                ClearSession(restoreMain: true);

                try
                {
                    callback?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Error($"[HeroAppearanceHelper] Error in OnClosed callback: {ex}");
                }
            }
        }

        private static void ClearSession(bool restoreMain)
        {
            if (restoreMain && Hero.MainHero != null)
            {
                try
                {
                    if (_hasSavedMainBody)
                    {
                        ApplyBodyPropertiesToHero(Hero.MainHero, _savedMainBody);
                        Log.Debug("[HeroAppearanceHelper] Restored main hero body after barber.");
                    }

                    if (_hasSavedMainGender)
                    {
                        SetHeroIsFemale(Hero.MainHero, _savedMainIsFemale);
                        Log.Debug("[HeroAppearanceHelper] Restored main hero gender after barber.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(
                        $"[HeroAppearanceHelper] Failed to restore main hero appearance: {ex}"
                    );
                }
            }

            Log.Debug("[HeroAppearanceHelper] Clearing session.");
            _currentTarget = null;
            _savedMainBody = default;
            _hasSavedMainBody = false;
            _savedMainIsFemale = default;
            _hasSavedMainGender = false;
            _onClosed = null;
        }

        /// <summary>
        /// Applies a full BodyProperties (dynamic + static) to a hero:
        /// - StaticBodyProperties for face/keys
        /// - SetBirthDay for age
        /// - Weight / Build.
        /// </summary>
        private static void ApplyBodyPropertiesToHero(Hero hero, in BodyProperties body)
        {
            if (hero == null)
                return;

            try
            {
                var dyn = body.DynamicProperties;
                var stat = body.StaticProperties;

#if BL13
                hero.StaticBodyProperties = stat;
#else
                Reflector.SetPropertyValue(hero, "StaticBodyProperties", stat);
#endif
                hero.SetBirthDay(CampaignTime.YearsFromNow(-dyn.Age));
                hero.Weight = dyn.Weight;
                hero.Build = dyn.Build;
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        private static bool GetHeroIsFemale(Hero hero) => hero?.IsFemale ?? false;

        private static void SetHeroIsFemale(Hero hero, bool value)
        {
            if (hero == null)
                return;

#if BL13
            hero.IsFemale = value;
#else
            // BL12: IsFemale has private setter; use reflection.
            Reflector.SetPropertyValue(hero, "IsFemale", value);
#endif
        }
    }
}
