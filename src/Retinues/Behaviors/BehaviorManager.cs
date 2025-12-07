using System;
using System.Collections.Generic;
using System.Reflection;
using Retinues.Utilities;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace Retinues.Behaviors
{
    /// <summary>
    /// Central registry and persistence helper for Retinues behaviors.
    /// - Acts as the module's SaveableTypeDefiner.
    /// - Discovers behaviors via reflection.
    /// - Registers enabled campaign/mission behaviors.
    /// - Aggregates save definitions from static methods on behaviors.
    /// </summary>
    public sealed class BehaviorManager : SaveableTypeDefiner
    {
        /// <summary>
        /// Unique ID for this module's SaveableTypeDefiner.
        /// </summary>
        private const int saveableTypeDefinerId = 070_992;

        /// <summary>
        /// Construct the definer with the module's unique base ID.
        /// </summary>
        public BehaviorManager()
            : base(saveableTypeDefinerId) { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //            SaveableTypeDefiner entry points            //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Register classes that the save system must know how to serialize.
        /// Behaviors contribute their own definitions via:
        ///   public static void DefineClassTypes(SaveableTypeDefiner d) { ... }
        /// </summary>
        protected override void DefineClassTypes()
        {
            base.DefineClassTypes();
            RegisterBehaviorClassTypes(this);
        }

        /// <summary>
        /// Register container (collection) types used by saved data.
        /// Behaviors contribute their own definitions via:
        ///   public static void DefineContainerDefinitions(SaveableTypeDefiner d) { ... }
        /// </summary>
        protected override void DefineContainerDefinitions()
        {
            base.DefineContainerDefinitions();
            RegisterBehaviorContainerDefinitions(this);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                 Behavior type discovery                //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static readonly Lazy<IReadOnlyList<Type>> _behaviorTypes = new(ScanBehaviorTypes);

        /// <summary>
        /// All non-abstract behavior types (campaign or mission) in this assembly.
        /// </summary>
        public static IReadOnlyList<Type> BehaviorTypes => _behaviorTypes.Value;

        private static IReadOnlyList<Type> ScanBehaviorTypes()
        {
            var asm = typeof(BehaviorManager).Assembly;
            var baseCampaign = typeof(BaseCampaignBehavior);
            var baseMission = typeof(BaseMissionBehavior);

            var result = new List<Type>();

            foreach (var t in asm.GetTypes())
            {
                if (t.IsAbstract || t.IsInterface)
                    continue;

                if (baseCampaign.IsAssignableFrom(t) || baseMission.IsAssignableFrom(t))
                    result.Add(t);
            }

            Log.Debug($"Discovered {result.Count} behavior types.");
            return result;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                Auto registration helpers               //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Creates and registers all enabled campaign behaviors with the campaign starter.
        /// Behaviors must have a public parameterless constructor.
        /// </summary>
        public static void RegisterCampaignBehaviors(CampaignGameStarter starter)
        {
            if (starter == null)
                return;

            foreach (var behavior in CreateCampaignBehaviors())
                starter.AddBehavior(behavior);
        }

        /// <summary>
        /// Creates and registers all enabled mission behaviors with the mission.
        /// Behaviors must have a public parameterless constructor.
        /// </summary>
        public static void RegisterMissionBehaviors(Mission mission)
        {
            if (mission == null)
                return;

            foreach (var behavior in CreateMissionBehaviors())
                mission.AddMissionBehavior(behavior);
        }

        private static IEnumerable<BaseCampaignBehavior> CreateCampaignBehaviors()
        {
            foreach (var t in BehaviorTypes)
            {
                if (!typeof(BaseCampaignBehavior).IsAssignableFrom(t))
                    continue;

                BaseCampaignBehavior instance = null;

                try
                {
                    instance = (BaseCampaignBehavior)Activator.CreateInstance(t);
                }
                catch (Exception e)
                {
                    Log.Exception(e, $"Failed to instantiate {t.FullName}.");
                }

                if (instance == null)
                    continue;

                if (!instance.IsEnabled)
                {
                    Log.Debug($"Skipping disabled campaign behavior {t.Name}.");
                    continue;
                }

                Log.Debug($"Registered campaign behavior {t.Name}.");
                yield return instance;
            }
        }

        private static IEnumerable<BaseMissionBehavior> CreateMissionBehaviors()
        {
            foreach (var t in BehaviorTypes)
            {
                if (!typeof(BaseMissionBehavior).IsAssignableFrom(t))
                    continue;

                BaseMissionBehavior instance = null;

                try
                {
                    instance = (BaseMissionBehavior)Activator.CreateInstance(t);
                }
                catch (Exception e)
                {
                    Log.Exception(e, $"Failed to instantiate {t.FullName}.");
                }

                if (instance == null)
                    continue;

                if (!instance.IsEnabled)
                {
                    Log.Debug($"Skipping disabled mission behavior {t.Name}.");
                    continue;
                }

                Log.Debug($"Registered mission behavior {t.Name}.");
                yield return instance;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Save definitions                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Lets behaviors contribute class definitions to this SaveableTypeDefiner.
        /// Each behavior can declare:
        ///   public static void DefineClassTypes(SaveableTypeDefiner definer) { ... }
        /// </summary>
        private static void RegisterBehaviorClassTypes(SaveableTypeDefiner definer)
        {
            if (definer == null)
                return;

            foreach (var t in BehaviorTypes)
            {
                var method = t.GetMethod(
                    "DefineClassTypes",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    [typeof(SaveableTypeDefiner)],
                    null
                );

                if (method == null)
                    continue;

                try
                {
                    method.Invoke(null, [definer]);
                }
                catch (Exception e)
                {
                    Log.Exception(e, $"Error in {t.FullName}.DefineClassTypes.");
                }
            }
        }

        /// <summary>
        /// Lets behaviors contribute container definitions to this SaveableTypeDefiner.
        /// Each behavior can declare:
        ///   public static void DefineContainerDefinitions(SaveableTypeDefiner definer) { ... }
        /// </summary>
        private static void RegisterBehaviorContainerDefinitions(SaveableTypeDefiner definer)
        {
            if (definer == null)
                return;

            foreach (var t in BehaviorTypes)
            {
                var method = t.GetMethod(
                    "DefineContainerDefinitions",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    null,
                    [typeof(SaveableTypeDefiner)],
                    null
                );

                if (method == null)
                    continue;

                try
                {
                    method.Invoke(null, [definer]);
                }
                catch (Exception e)
                {
                    Log.Exception(e, $"Error in {t.FullName}.DefineContainerDefinitions.");
                }
            }
        }
    }
}
