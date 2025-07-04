# if EXILED
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Pickups;
using PlayerEvents = Exiled.Events.Handlers.Player;
# else
using LabApi.Loader.Features.Plugins;
using LabApi.Events.Handlers;
#endif
using ShootingInteractions.Configuration;
using ShootingInteractions.Properites;
using System;
using System.Reflection;
using System.Linq;

namespace ShootingInteractions
{
    public class Plugin : Plugin<Config>
    {
        internal static MethodInfo GetCustomItem = null;

        public static Plugin Instance { get; private set; }

        public override string Name => AssemblyInfo.Name;

        public override string Author => AssemblyInfo.Author;
#if EXILED
        public override Version RequiredExiledVersion => new(9, 6, 1);
        public override PluginPriority Priority => PluginPriority.First;
#else
        public override Version RequiredApiVersion => new(1, 1, 0);
        public override string Description => AssemblyInfo.Description;
#endif

        public override Version Version => new(AssemblyInfo.Version);

        private EventsHandler eventsHandler;
#if EXILED
        public override void OnEnabled()
#else
        public override void Enable()
#endif
        {
            Instance = this;

            RegisterEvents();
#if EXILED
            Assembly customItems = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == "Exiled.CustomItems");

            if (customItems is not null)
            {
                Type customItemType = customItems.GetType("Exiled.CustomItems.API.Features.CustomItem");
                GetCustomItem = customItemType?.GetMethod("TryGet", new[] { typeof(Pickup), customItemType.MakeByRefType() });
            }
            base.OnEnabled();
#endif
        }

#if EXILED
        public override void OnDisabled()
#else
        public override void Disable()
#endif
        {
            UnregisterEvents();

            Instance = null;
#if EXILED
            base.OnDisabled();
#endif
        }

        public void RegisterEvents()
        {
            eventsHandler = new EventsHandler();
#if EXILED
            PlayerEvents.Shot += eventsHandler.OnShot;
#else
            PlayerEvents.ShotWeapon += eventsHandler.OnShot;
#endif
        }

        public void UnregisterEvents()
        {
#if EXILED
            PlayerEvents.Shot += eventsHandler.OnShot;
#else
            PlayerEvents.ShotWeapon -= eventsHandler.OnShot;
#endif

            eventsHandler = null;
        }
    }
}
