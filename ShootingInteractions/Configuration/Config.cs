#if EXILED
using Exiled.API.Interfaces;
#endif
using ShootingInteractions.Configuration.Bases;
using System.ComponentModel;

namespace ShootingInteractions.Configuration
{
#if EXILED
    public sealed class Config : IConfig
#else
    public sealed class Config
#endif
    {
        [Description("Is the plugin enabled")]
        public bool IsEnabled { get; set; } = true;

        [Description("Are the plugin's debug logs enabled")]
        public bool Debug { get; set; } = false;

        [Description("Check where the bullet actually lands instead of the center of the player's screen")]
        public bool AccurateBullets { get; set; } = false;

        [Description("Door buttons interaction")]
        public DoorsInteraction Doors { get; set; } = new();

        [Description("Checkpoint door buttons interaction")]
        public DoorsInteraction Checkpoints { get; set; } = new();

        [Description("Gate buttons interaction")]
        public DoorsInteraction Gates { get; set; } = new();

        [Description("Weapon grid lockers interaction")]
        public LockersInteraction WeaponGridLockers { get; set; } = new();

        [Description("Pedestals/Bulletproof lockers interaction")]
        public BulletproofLockersInteraction BulletproofLockers { get; set; } = new();

        [Description("Rifle rack interaction")]
        public LockersInteraction RifleRackLockers { get; set; } = new();

        [Description("Experimental weapon lockers interaction")]
        public LockersInteraction ExperimentalWeaponLockers { get; set; } = new();

        [Description("SCP-127 container interaction")]
        public LockersInteraction Scp127Container { get; set; } = new();

        [Description("Elevators buttons interaction")]
        public ElevatorsInteraction Elevators { get; set; } = new();

        [Description("Frag grenades interaction")]
        public TimedProjectilesInteraction FragGrenades { get; set; } = new();

        [Description("Flashbangs interaction")]
        public TimedProjectilesInteraction Flashbangs { get; set; } = new();
#if EXILED

        [Description("Custom grenades interaction")]
        public ProjectilesInteraction CustomGrenades { get; set; } = new();
#endif

        [Description("Nuke start button interaction")]
        public NukeButtonsInteraction NukeStartButton { get; set; } = new();

        [Description("Nuke cancel button interaction")]
        public NukeButtonsInteraction NukeCancelButton { get; set; } = new();

        [Description("SCP-018 interaction")]
        public TimedProjectilesInteraction Scp018 { get; set; } = new();

        [Description("SCP-2176 interaction")]
        public ProjectilesInteraction Scp2176 { get; set; } = new();

        [Description("SCP-244 interaction")]
        public ProjectilesInteraction Scp244 { get; set; } = new();
    }
}
