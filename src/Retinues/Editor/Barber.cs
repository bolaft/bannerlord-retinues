using System;
using HarmonyLib;
using Helpers;
using Retinues.Utilities;
using SandBox.GauntletUI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Editor
{
    [HarmonyPatch(typeof(GauntletBarberScreen))]
    internal static class GauntletBarberScreenPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnFinalize")]
        private static void OnFinalizePostfix()
        {
            try
            {
                Barber.OnBarberClosed();
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnFrameTick")]
        private static void OnFrameTickPostfix(GauntletBarberScreen __instance, float dt)
        {
            try
            {
                if (!Barber.HasActiveSession)
                    return;

                var handler = __instance.Handler;
                Barber.PrimeFaceGen(handler);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }

    /// <summary>
    /// Opens the vanilla barber / FaceGen screen and redirects changes
    /// from MainHero to an arbitrary target hero.
    /// </summary>
    [SafeClass]
    public static class Barber
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
                return;

            if (Game.Current == null || Game.Current.GameStateManager == null)
                return;

            if (Campaign.Current == null || !Campaign.Current.IsFaceGenEnabled)
                return;

            // Only one redirected session at a time.
            if (_currentTarget != null)
                return;

            try
            {
                bool isMainHero = hero == Hero.MainHero;

                _currentTarget = hero;
                _onClosed = onClosed;

                // Save main hero body + gender only if we're redirecting to a different hero.
                if (!isMainHero)
                {
                    _savedMainBody = Hero.MainHero.BodyProperties;
                    _hasSavedMainBody = true;

                    _savedMainIsFemale = GetHeroIsFemale(Hero.MainHero);
                    _hasSavedMainGender = true;
                }

                _needsPrime = true;
                _hasPrimed = false;

                // If editing a different hero, apply that hero's gender/body to MainHero so FaceGen shows the correct face.
                if (!isMainHero)
                {
                    var targetIsFemale = GetHeroIsFemale(hero);
                    SetHeroIsFemale(Hero.MainHero, targetIsFemale);

                    var targetBody = hero.BodyProperties;
                    ApplyBodyPropertiesToHero(Hero.MainHero, targetBody);
                }

                // Open vanilla barber screen for MainHero, as MapScreen does
                IFaceGeneratorCustomFilter filter = CharacterHelper.GetFaceGeneratorFilter();

                var barberState = Game.Current.GameStateManager.CreateState<BarberState>(
                    [Hero.MainHero.CharacterObject, filter]
                );

                GameStateManager.Current.PushState(barberState);
            }
            catch (Exception e)
            {
                Log.Exception(e);
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
                return;

            try
            {
                // Whatever FaceGen changed is now on MainHero
                var editedBody = Hero.MainHero.BodyProperties;
                var editedIsFemale = GetHeroIsFemale(Hero.MainHero);

                // Apply edited body + gender to the target hero
                ApplyBodyPropertiesToHero(_currentTarget, editedBody);
                SetHeroIsFemale(_currentTarget, editedIsFemale);
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
            finally
            {
                var callback = _onClosed;
                ClearSession(restoreMain: true);

                try
                {
                    callback?.Invoke();
                }
                catch (Exception e)
                {
                    Log.Exception(e);
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
                        ApplyBodyPropertiesToHero(Hero.MainHero, _savedMainBody);

                    if (_hasSavedMainGender)
                        SetHeroIsFemale(Hero.MainHero, _savedMainIsFemale);
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

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
                Reflection.SetPropertyValue(hero, "StaticBodyProperties", stat);
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
            Reflection.SetPropertyValue(hero, "IsFemale", value);
#endif
        }
    }
}
