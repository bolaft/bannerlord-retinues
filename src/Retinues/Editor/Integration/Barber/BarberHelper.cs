using System;
using Helpers;
using Retinues.Domain;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Retinues.Editor.Integration.Barber
{
    /// <summary>
    /// Opens the vanilla barber / FaceGen screen and redirects changes
    /// from MainHero to an arbitrary target hero.
    /// </summary>
    [SafeClass]
    public static class BarberHelper
    {
        private static WHero _currentTarget;

        private static BodyProperties _savedMainBody;
        private static bool _hasSavedMainBody;
        private static bool _savedMainIsFemale;
        private static bool _hasSavedMainGender;

        public static bool HasActiveSession => _currentTarget?.Base != null;

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

            var target = WHero.Get(hero);
            if (target?.Base == null)
                return;

            if (Game.Current?.GameStateManager == null)
                return;

            if (Campaign.Current == null || !Campaign.Current.IsFaceGenEnabled)
                return;

            // Only one redirected session at a time.
            if (_currentTarget != null)
                return;

            var main = Player.Hero;
            if (main?.Base == null)
                return;

            try
            {
                bool isMainHero = target.IsMainHero;

                _currentTarget = target;
                _onClosed = onClosed;

                // Save main hero body + gender only if we're redirecting to a different hero.
                if (!isMainHero)
                {
                    _savedMainBody = main.Base.BodyProperties;
                    _hasSavedMainBody = true;

                    _savedMainIsFemale = main.IsFemale;
                    _hasSavedMainGender = true;
                }

                _needsPrime = true;
                _hasPrimed = false;

                // If editing a different hero, apply that hero's gender/body to MainHero so FaceGen shows the correct face.
                if (!isMainHero)
                {
                    main.IsFemale = target.IsFemale;
                    ApplyBodyPropertiesToHero(main.Base, target.Base.BodyProperties);
                }

                // Open vanilla barber screen for MainHero, as MapScreen does.
                IFaceGeneratorCustomFilter filter = CharacterHelper.GetFaceGeneratorFilter();

                var barberState = Game.Current.GameStateManager.CreateState<BarberState>(
                    [main.Base.CharacterObject, filter]
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
            if (_currentTarget?.Base == null)
                return;

            var main = Player.Hero;
            if (main?.Base == null)
            {
                ClearSession(restoreMain: false);
                return;
            }

            try
            {
                // Whatever FaceGen changed is now on MainHero.
                var editedBody = main.Base.BodyProperties;
                var editedIsFemale = main.IsFemale;

                // Apply edited body + gender to the target hero.
                ApplyBodyPropertiesToHero(_currentTarget.Base, editedBody);
                _currentTarget.IsFemale = editedIsFemale;
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

        /// <summary>
        /// Clears the current barber session state.
        /// </summary>
        private static void ClearSession(bool restoreMain)
        {
            var main = Player.Hero;

            if (restoreMain && main?.Base != null)
            {
                try
                {
                    if (_hasSavedMainBody)
                        ApplyBodyPropertiesToHero(main.Base, _savedMainBody);

                    if (_hasSavedMainGender)
                        main.IsFemale = _savedMainIsFemale;
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
        /// - StaticBodyProperties for face keys
        /// - SetBirthDay for age
        /// - Weight and Build
        /// </summary>
        private static void ApplyBodyPropertiesToHero(Hero hero, in BodyProperties body)
        {
            if (hero == null)
                return;

            try
            {
                var dyn = body.DynamicProperties;
                var stat = body.StaticProperties;

#if BL13 || BL14
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
    }
}
