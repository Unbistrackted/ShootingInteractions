# if EXILED
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using DoorLockType = Exiled.API.Enums.DoorLockType;
using Exiled.Events.EventArgs.Player;
using PlayerShotWeaponEventArgs = Exiled.Events.EventArgs.Player.ShotEventArgs;
#else
using LabApi.Features.Wrappers;
using Firearm = LabApi.Features.Wrappers.FirearmItem;
using Keycard = LabApi.Features.Wrappers.KeycardItem;
using LabApi.Events.Arguments.PlayerEvents;
using Log = LabApi.Features.Console.Logger;
#endif
using Interactables.Interobjects;
using Interactables.Interobjects.DoorButtons;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using InventorySystem.Items.Usables.Scp244;
using InventorySystem.Items.Firearms.Modules;
using MEC;
using Mirror;
using ShootingInteractions.Configuration;
using ShootingInteractions.Configuration.Bases;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Footprinting;
using ElevatorDoor = Interactables.Interobjects.ElevatorDoor;
using InteractableCollider = Interactables.InteractableCollider;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Scp2176Projectile = InventorySystem.Items.ThrowableProjectiles.Scp2176Projectile;
using CheckpointDoor = Interactables.Interobjects.CheckpointDoor;
using BasicDoor = Interactables.Interobjects.BasicDoor;
using Locker = MapGeneration.Distributors.Locker;
using ExperimentalWeaponLocker = MapGeneration.Distributors.ExperimentalWeaponLocker;
using LockerChamber = MapGeneration.Distributors.LockerChamber;
using TimedGrenadePickup = InventorySystem.Items.ThrowableProjectiles.TimedGrenadePickup;
using ThrowableItem = InventorySystem.Items.ThrowableProjectiles.ThrowableItem;
using InventorySystem.Items.Keycards;

namespace ShootingInteractions
{
    internal sealed class EventsHandler
    {
        /// <summary>
        /// The plugin's config
        /// </summary>
        private static Config Config => Plugin.Instance.Config;

        /// <summary>
        /// A list of gameobjects that cannot be interacted with.
        /// </summary>
        public static List<GameObject> BlacklistedObjects = new();

        /// <summary>
        /// The shot event. Used for accurate shooting interaction.
        /// </summary>
        /// <param name="args">The <see cref="ShotEventArgs"/>.</param>
        public void OnShot(PlayerShotWeaponEventArgs args)
        {
#if EXILED
            Vector3 origin = args.Player.CameraTransform.position;
            Vector3 direction = Config.AccurateBullets ? (args.RaycastHit.point - origin).normalized : args.Player.CameraTransform.forward;
            Firearm firearm = args.Firearm;
#else
            Vector3 origin = args.Player.Camera.position;
            Vector3 direction = args.Player.Camera.forward; //  Config.AccurateBullets ? (args..point - origin).normalized
            FirearmItem firearm = args.FirearmItem;
#endif

            // Check what's the player shooting at with a raycast, and return if the raycast doesn't hit something within 70 distance (maximum realistic distance)
            // Layer 1 = VolumeOverrideTunne
            // Layer 13 = Player's Hitboxes
            // Layer 16 = Surface Gate A Bridge
            // Layer 28 = Broken Glasses
            // Layer 29 = Fences
            if (!Physics.Raycast(origin, direction, out RaycastHit raycastHit, 70f, ~(1 << 1 | 1 << 13 | 1 << 16 | 1 << 28 | 1 << 29)))
                return;

            // Interact if the object isn't in the blacklist
            if (!BlacklistedObjects.Contains(raycastHit.transform.gameObject) && Interact(args.Player, raycastHit.transform.gameObject, firearm, direction))
            {
#if EXILED
                args.Player.ShowHitMarker();
#else
                args.Player.SendHitMarker();
#endif
                // Add the GameObject in the blacklist for a server tick
                BlacklistedObjects.Add(raycastHit.transform.gameObject);
                Timing.CallDelayed(Time.smoothDeltaTime, () => BlacklistedObjects.Remove(raycastHit.transform.gameObject));
            }
        }

        /// <summary>
        /// Interact with the game object.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> that's doing the interaction</param>
        /// <param name="gameObject">The <see cref="GameObject"/></param>
        /// <param name="firearm">The <see cref="Firearm"/> that was used</param>
        /// <param name="direction">The <see cref="Vector3"/> the direction which the raycast was shot</param>
        /// <returns>If the GameObject was interacted with.</returns>
        public static bool Interact(Player player, GameObject gameObject, Firearm firearm, Vector3 direction)
        {
            if (Config.Debug)
            {
                Log.Debug(gameObject.name);
                Log.Debug(gameObject.layer);
                Log.Debug(string.Join(", ", gameObject.GetComponents<Component>().ToList()));
                Log.Debug(string.Join(", ", gameObject.GetComponentsInParent<Component>().ToList()));
                Log.Debug(string.Join(", ", gameObject.GetComponentsInChildren<Component>().ToList()));
            }

            float penetration = 0;
            bool hasInteracted = false;

            foreach (ModuleBase moduleBase in firearm.Base.Modules)
                if (moduleBase is HitscanHitregModuleBase hitscanHitregModuleBase)
                    penetration = hitscanHitregModuleBase.BasePenetration;
#if EXILED
            bool isBypassEnabled = player.IsBypassModeEnabled;
#else
            bool isBypassEnabled = player.IsBypassEnabled;
#endif

            // Doors
            if (gameObject.GetComponentInParent<BasicDoorButton>() is BasicDoorButton button)
            {
                //Interactables.Interobjects.CheckpointDoor
                //Interactables.Interobjects.PryableDoor
                // Get the door associated to the button
                Door door = Door.Get(button.GetComponentInParent<DoorVariant>());
                DoorVariant doorVariant = button.GetComponentInParent<DoorVariant>();

                DoorsInteraction doorInteractionConfig = doorVariant switch
                {
                    PryableDoor => Config.Gates,
                    CheckpointDoor => Config.Checkpoints,
                    BasicDoor => Config.Doors,
                    _ => new DoorsInteraction { IsEnabled = false }
                };

                // Return if:
                //  - if the interaction isn't enabled
                //  - if MinimumPenetration isn't reached
                //  - door can't be found
                //  - door is moving
                //  - door is locked, and bypass mode is disabled
                //  - it's an open checkpoint
                if (!doorInteractionConfig.IsEnabled
                    || (doorInteractionConfig.MinimumPenetration / 100 >= penetration)
                    || door is null
                    || door.Base.IsMoving
                    || (doorVariant.NetworkActiveLocks > 0 && !isBypassEnabled)
                    || (doorVariant is CheckpointDoor && doorVariant.NetworkTargetState))
                    return false;

                // Get the door cooldown (used to lock the door AFTER it moved) and the config depending on the door type
                float cooldown = 0f;

                if (doorVariant is CheckpointDoor checkpoint)
                    cooldown = 0.6f + checkpoint.SequenceCtrl.OpenLoopTime + checkpoint.SequenceCtrl.WarningTime;

                else if (doorVariant is BasicDoor interactableDoor)
                {
                    // Return if the door is in cooldown
                    if (interactableDoor._remainingAnimCooldown >= 0.1f)
                        return false;

                    cooldown = interactableDoor._remainingAnimCooldown - 0.35f;

                    if (doorVariant is PryableDoor)
                    {
                        // A gate takes less time to open than close
                        if (!doorVariant.NetworkTargetState)
                            cooldown -= 0.35f;
                    }
                }

                // Should the door get locked ? (Generate number from 1 to 100 then check if lesser than interaction percentage)
                bool shouldLock = !door.IsLocked && Random.Range(1, 101) <= doorInteractionConfig.LockChance;

                // Lock the door if it should be locked BEFORE moving
                if (shouldLock && !doorInteractionConfig.MoveBeforeLocking && !door.IsLocked)
                {
#if EXILED
                    door.ChangeLock(DoorLockType.Isolation);

                    // Unlock the door after the time indicated in the config (if greater than 0)
                    if (doorInteractionConfig.LockDuration > 0)
                        Timing.CallDelayed(doorInteractionConfig.LockDuration, () => door.ChangeLock(DoorLockType.None));
#else
                    door.Lock(DoorLockReason.Isolation, true);

                    // Unlock the door after the time indicated in the config (if greater than 0)
                    if (doorInteractionConfig.LockDuration > 0)
                        Timing.CallDelayed(doorInteractionConfig.LockDuration, () => door.Lock(DoorLockReason.Isolation, false));
#endif
                    // Don't interact if bypass mode is disabled
                    if (!isBypassEnabled)
                        return false;
                }

                // Deny access if the door is a keycard door, bypass mode is disabled, and either: remote keycard is disabled OR the player has no keycard that open the door
                if (doorVariant.RequiredPermissions.RequiredPermissions != DoorPermissionFlags.None && !isBypassEnabled && (!doorInteractionConfig.RemoteKeycard || !HasPermission(player, doorVariant)))
                {
                    door.Base.PermissionsDenied(null, 0);
                    return false;
                }

                // Open or close the door
                doorVariant.NetworkTargetState = !doorVariant.NetworkTargetState;

                // Lock the door if it should be locked AFTER moving
                if (shouldLock && doorInteractionConfig.MoveBeforeLocking)
                    Timing.CallDelayed(cooldown, () =>
                    {
#if EXILED
                        door.ChangeLock(DoorLockType.Isolation);

                        // Unlock the door after the time indicated in the config (if greater than 0)
                        if (doorInteractionConfig.LockDuration > 0)
                            Timing.CallDelayed(doorInteractionConfig.LockDuration, () => door.ChangeLock(DoorLockType.None));
#else
                        door.Lock(DoorLockReason.Isolation, true);

                        // Unlock the door after the time indicated in the config (if greater than 0)
                        if (doorInteractionConfig.LockDuration > 0)
                            Timing.CallDelayed(doorInteractionConfig.LockDuration, () => door.Lock(DoorLockReason.Isolation, false));
#endif
                    });

                hasInteracted = true;
            }

            // Lockers (Pedestal, Weapon, Experimental and SCP-127)
            else if (gameObject.GetComponentInParent<Locker>() is Locker locker)
            {
                LockersInteraction lockerInteractionConfig = gameObject.name switch
                {
                    "Collider Keypad" => Config.BulletproofLockers,
                    "Collider Door" => Config.BulletproofLockers,
                    "Door" => Config.WeaponGridLockers,
                    "EWL_CenterDoor" => Config.ExperimentalWeaponLockers,
                    "Collider Lid" => Config.Scp127Container,
                    "Collider" => Config.RifleRackLockers,
                    _ => new LockersInteraction() { IsEnabled = false }
                };

                // Return if:
                //  - interaction isn't enabled
                //  - MinimumPenetration isn't reached
                //  - OnlyKeypad isn't enabled
                if (!lockerInteractionConfig.IsEnabled
                    || (lockerInteractionConfig.MinimumPenetration / 100 >= penetration)
                    || (lockerInteractionConfig is BulletproofLockersInteraction bulletProofLockerInteractionConfig && gameObject.name == "Collider Door" && bulletProofLockerInteractionConfig.OnlyKeypad))
                    return false;

                // Experimental weapon lockers
                if (gameObject.GetComponentInParent<ExperimentalWeaponLocker>() is ExperimentalWeaponLocker baseExpLocker)
                {
                    // Gets the wrapper for the experimental weapon locker
                    LabApi.Features.Wrappers.ExperimentalWeaponLocker expLocker = LabApi.Features.Wrappers.ExperimentalWeaponLocker.Get(baseExpLocker);

                    // Return if the locker doesn't allow interaction
                    if (!expLocker.CanInteract)
                        return false;

                    // Deny access if bypass mode is disabled and either: remote keycard is disabled OR the player has no keycard that open the locker
                    if (!isBypassEnabled && (!lockerInteractionConfig.RemoteKeycard || !HasPermission(player, expLocker.Chamber.Base)))
                    {
                        expLocker.PlayDeniedSound(expLocker.RequiredPermissions);
                        return false;
                    }

                    // Open the locker
                    expLocker.IsOpen = !expLocker.IsOpen;
                    locker.RefreshOpenedSyncvar();
                }

                // SCP-127 container, Pedestals and Weapon grid lockers
                if (gameObject.GetComponentInParent<LockerChamber>() is LockerChamber chamber)
                {
                    // Return if the locker doesn't allow interaction
                    if (!chamber.CanInteract)
                        return false;

                    // Deny access if bypass mode is disabled and either: remote keycard is disabled OR the player has no keycard that open the locker
                    if (!isBypassEnabled && (!lockerInteractionConfig.RemoteKeycard || !HasPermission(player, chamber)))
                    {
                        locker.RpcPlayDenied((byte)locker.Chambers.ToList().IndexOf(chamber), chamber.RequiredPermissions);
                        return false;
                    }

                    // Open the locker
                    chamber.SetDoor(!chamber.IsOpen, locker._grantedBeep);
                    locker.RefreshOpenedSyncvar();

                    hasInteracted = true;
                }

                // Rifle racks
                if (LabApi.Features.Wrappers.RifleRackLocker.Dictionary.TryGetValue(locker, out LabApi.Features.Wrappers.RifleRackLocker rifleRackLocker))
                {
                    // Return if the locker doesn't allow interaction
                    if (!rifleRackLocker.CanInteract)
                        return false;

                    // Deny access if bypass mode is disabled and either: remote keycard is disabled OR the player has no keycard that open the locker
                    if (!isBypassEnabled && (!lockerInteractionConfig.RemoteKeycard || !HasPermission(player, locker.Chambers.First())))
                    {
                        locker.RpcPlayDenied(locker.ComponentIndex, rifleRackLocker.RequiredPermissions);
                        return false;
                    }

                    // Open the locker
                    rifleRackLocker.IsOpen = !rifleRackLocker.IsOpen;
                    locker.RefreshOpenedSyncvar();

                    hasInteracted = true;
                }
            }

            // Elevators
            else if (gameObject.GetComponentInParent<ElevatorPanel>() is ElevatorPanel panel)
            {
                ElevatorsInteraction elevatorInteractionConfig = Config.Elevators;

                // Get the elevator associated to the button
#if EXILED
                Lift elevator = Lift.Get(panel.AssignedChamber);
                bool isLocked = elevator.IsLocked;
                bool isMoving = elevator.IsMoving;
#else
                Elevator elevator = Elevator.Get(panel.AssignedChamber);
                bool isLocked = elevator.AnyDoorLockedReason <= 0 ? elevator.AllDoorsLockedReason > 0 : true;
                bool isMoving = (uint)elevator.Base.CurSequence - 2 <= 1u;
#endif

                // Return if:
                //  - interaction isn't enabled
                //  - MinimumPenetration isn't reached
                //  - panel has no chamber
                //  - elevator can't be found
                //  - elevator is moving
                //  - elevator is locked and bypass mode is disabled
                //  - no elevator doors
                if (!elevatorInteractionConfig.IsEnabled
                    || (elevatorInteractionConfig.MinimumPenetration / 100 >= penetration)
                    || panel.AssignedChamber is null
                    || elevator is null
                    || isMoving
                    || !elevator.Base.IsReady
                    || (isLocked && !isBypassEnabled)
                    || !ElevatorDoor.AllElevatorDoors.TryGetValue(panel.AssignedChamber.AssignedGroup, out List<ElevatorDoor> list))
                    return false;

                // Should the elevator get locked ? (Generate a number from 1 to 100 then check if it's lesser than config percentage)
                bool shoudLock = !isLocked && Random.Range(1, 101) <= elevatorInteractionConfig.LockChance;

                // Lock the door if it should be locked BEFORE moving
                if (shoudLock && !elevatorInteractionConfig.MoveBeforeLocking)
                {
                    foreach (ElevatorDoor door in list)
                        door.ServerChangeLock(DoorLockReason.Isolation, true);

                    // Unlock the door after the time indicated in the config (if greater than 0)
                    if (elevatorInteractionConfig.LockDuration > 0)
                        Timing.CallDelayed(elevatorInteractionConfig.LockDuration, () =>
                        {
                            foreach (ElevatorDoor door in list)
                            {
                                door.NetworkActiveLocks = 0;
                                DoorEvents.TriggerAction(door, DoorAction.Unlocked, null);
                            }
                        });

                    // Don't interact if bypass mode is disabled
                    if (!isBypassEnabled)
                        return false;
                }

                // Move the elevator to the next level
#if EXILED
                elevator.TryStart(panel.AssignedChamber.NextLevel);
#else
                elevator.SetDestination(panel.AssignedChamber.NextLevel);
#endif

                // Lock the door if it should be locked AFTER moving
                if (shoudLock && elevatorInteractionConfig.MoveBeforeLocking)
                {
                    foreach (ElevatorDoor door in list)
                        door.ServerChangeLock(DoorLockReason.Isolation, true);

                    // Unlock the door after the time indicated in the config (if greater than 0)
                    if (elevatorInteractionConfig.LockDuration > 0)
                        Timing.CallDelayed(elevatorInteractionConfig.LockDuration + elevator.Base._animationTime + elevator.Base._rotationTime + elevator.Base._doorOpenTime + elevator.Base._doorCloseTime, () =>
                        {
                            foreach (ElevatorDoor door in list)
                            {
                                door.NetworkActiveLocks = 0;
                                DoorEvents.TriggerAction(door, DoorAction.Unlocked, null);
                            }
                        });
                }

                hasInteracted = true;
            }

            // Grenades
            else if (gameObject.GetComponentInParent<TimedGrenadePickup>() is TimedGrenadePickup grenadePickup)
            {
                // Custom grenades (EXILED ONLY)
                if (Plugin.GetCustomItem is not null && (bool)Plugin.GetCustomItem.Invoke(null, new[] { Pickup.Get(grenadePickup), null }))
                {
#if EXILED
                    // Return if:
                    //  - interaction isn't enabled
                    //  - MinimumPenetration isn't reached
                    if (!Config.CustomGrenades.IsEnabled
                        || (Config.CustomGrenades.MinimumPenetration >= penetration / 100))
                        return false;

                    // Set the attacker to the player shooting and explode the custom grenade
                    grenadePickup.PreviousOwner = new Footprint(player.ReferenceHub);
                    grenadePickup._replaceNextFrame = true;
#endif
                }

                // Non-custom grenades
                else
                {
                    TimedProjectilesInteraction grenadeInteractionConfig = grenadePickup.Info.ItemId switch
                    {
                        ItemType.GrenadeHE => Config.FragGrenades,
                        ItemType.GrenadeFlash => Config.Flashbangs,
                        _ => new TimedProjectilesInteraction() { IsEnabled = false }
                    };

                    // Return if:
                    //  - interaction isn't enabled
                    //  - MinimumPenetration isn't reached
                    //  - grenade base isn't found
                    //  - throwable ins't found
                    if (!grenadeInteractionConfig.IsEnabled || grenadeInteractionConfig.MinimumPenetration >= penetration / 100 || !InventoryItemLoader.AvailableItems.TryGetValue(grenadePickup.Info.ItemId, out ItemBase grenadeBase) || (grenadeBase is not ThrowableItem grenadeThrowable))
                        return false;

                    // Instantiate the projectile
                    ThrownProjectile grenadeProjectile = Object.Instantiate(grenadeThrowable.Projectile);

                    // Set the physics of the projectile
                    PickupStandardPhysics grenadeProjectilePhysics = grenadeProjectile.PhysicsModule as PickupStandardPhysics;
                    PickupStandardPhysics grenadePickupPhysics = grenadePickup.PhysicsModule as PickupStandardPhysics;
                    if (grenadeProjectilePhysics is not null && grenadePickupPhysics is not null)
                    {
                        Rigidbody grenadeProjectileRigidbody = grenadeProjectilePhysics.Rb;
                        Rigidbody grenadePickupRigidbody = grenadePickupPhysics.Rb;
                        grenadeProjectileRigidbody.position = grenadePickupRigidbody.position;
                        grenadeProjectileRigidbody.rotation = grenadePickupRigidbody.rotation;
                        grenadeProjectileRigidbody.AddForce(
                            direction
                            * (grenadeInteractionConfig.AdditionalVelocity ? grenadeInteractionConfig.VelocityForce : 1)
                            * (grenadeInteractionConfig.ScaleWithPenetration ? penetration * grenadeInteractionConfig.VelocityPenetrationMultiplier : 1));
                    }

                    // Lock the grenade pickup
                    grenadePickup.Info.Locked = true;

                    // Set the network info and owner of the projectile
                    grenadeProjectile.NetworkInfo = grenadePickup.Info;
                    grenadePickup.PreviousOwner = new Footprint(player.ReferenceHub);
                    grenadeProjectile.PreviousOwner = new Footprint(player.ReferenceHub);

                    // Spawn the grenade projectile
                    NetworkServer.Spawn(grenadeProjectile.gameObject);

                    // Should the grenade have a custom fuse time ? (Generate number from 1 to 100 then check if lesser than interaction percentage)
                    if (Random.Range(1, 101) <= grenadeInteractionConfig.CustomFuseTimeChance)

                        // Set the custom fuse time
                        (grenadeProjectile as TimeGrenade)._fuseTime = Mathf.Max(Time.smoothDeltaTime * 2, grenadeInteractionConfig.CustomFuseTimeDuration);

                    // Activate the projectile and destroy the pickup
                    grenadeProjectile.ServerActivate();
                    grenadePickup.DestroySelf();
                }

                hasInteracted = true;
            }

            // SCP-018
            else if (gameObject.GetComponentInParent<TimeGrenade>() is TimeGrenade scp018 && gameObject.name.Contains("Scp018Projectile"))
            {
                TimedProjectilesInteraction grenadeInteractionConfig = Config.Scp018;

                // Return if:
                //  - interaction isn't enabled
                //  - MinimumPenetration isn't reached
                //  - grenade base isn't found
                //  - throwable ins't found
                if (!grenadeInteractionConfig.IsEnabled
                    || (grenadeInteractionConfig.MinimumPenetration >= penetration / 100)
                    || !InventoryItemLoader.AvailableItems.TryGetValue(scp018.Info.ItemId, out ItemBase grenadeBase)
                    || (grenadeBase is not ThrowableItem grenadeThrowable))
                    return false;

                // Instantiate the projectile
                ThrownProjectile scp018Projectile = Object.Instantiate(grenadeThrowable.Projectile);

                // Set the physics of the projectile
                PickupStandardPhysics grenadeProjectilePhysics = scp018Projectile.PhysicsModule as PickupStandardPhysics;
                if (grenadeProjectilePhysics is not null)
                {
                    Rigidbody grenadeProjectileRigidbody = grenadeProjectilePhysics.Rb;
                    grenadeProjectileRigidbody.position = scp018.Position;
                    grenadeProjectileRigidbody.rotation = scp018.Rotation;
                    grenadeProjectileRigidbody.AddForce(
                        direction
                        * (grenadeInteractionConfig.AdditionalVelocity ? grenadeInteractionConfig.VelocityForce : 1)
                        * (grenadeInteractionConfig.ScaleWithPenetration ? penetration * grenadeInteractionConfig.VelocityPenetrationMultiplier : 1));
                }

                // Lock the grenade pickup
                scp018.Info.Locked = true;

                // Set the network info and owner of the projectile
                scp018Projectile.NetworkInfo = scp018.Info;
                scp018.PreviousOwner = new Footprint(player.ReferenceHub);
                scp018Projectile.PreviousOwner = new Footprint(player.ReferenceHub);

                // Should the grenade have a custom fuse time ? (Generate number from 1 to 100 then check if lesser than interaction percentage)
                if (Random.Range(1, 101) <= grenadeInteractionConfig.CustomFuseTimeChance)

                    // Set the custom fuse time
                    (scp018Projectile as TimeGrenade)._fuseTime = Mathf.Max(Time.smoothDeltaTime * 2, grenadeInteractionConfig.CustomFuseTimeDuration);

                // Activate the projectile and destroy the pickup
                scp018Projectile.ServerActivate();

                hasInteracted = true;
            }

            // SCP-244
            else if (gameObject.GetComponentInParent<Scp244DeployablePickup>() is Scp244DeployablePickup scp244)
            {
                // Return if:
                //  - interaction isn't enabled
                //  - MinimumPenetration isn't reached
                if (!Config.Scp244.IsEnabled
                    || (Config.Scp244.MinimumPenetration >= penetration / 100))
                    return false;

                // Shatters the SCP-244 deployable pickup
                scp244.State = Scp244State.Destroyed;

                hasInteracted = true;
            }

            // SCP-2176
            else if (gameObject.GetComponentInParent<Scp2176Projectile>() is Scp2176Projectile projectile)
            {
                // Return if:
                //  - interaction isn't enabled
                //  - MinimumPenetration isn't reached
                if (!Config.Scp2176.IsEnabled
                    || (Config.Scp2176.MinimumPenetration >= penetration / 100))
                    return false;

                // Shatters the SCP-2176 projectile
                projectile.ServerImmediatelyShatter();

                hasInteracted = true;
            }

            // Nuke Cancel Button
            else if (gameObject.GetComponentInParent<AlphaWarheadNukesitePanel>() is AlphaWarheadNukesitePanel warheadAlpha && gameObject.name.Contains("cancel"))
            {
                // Return if:
                //  - interaction isn't enabled
                //  - MinimumPenetration isn't reached
                if (!Config.NukeCancelButton.IsEnabled
                    || (Config.NukeCancelButton.MinimumPenetration >= penetration / 100))
                    return false;

                // Stops the nuke detonation
                Warhead.Stop(player);

                hasInteracted = true;
            }
#if EXILED
            // Nuke Start Button
            else if (gameObject.GetComponentInParent<InteractableCollider>() is not null && gameObject.name.Contains("Button") && player.CurrentRoom.Type == RoomType.Surface)
            {
                bool isKeycardActivated = Warhead.IsKeycardActivated;
                bool canBeStarted = Warhead.CanBeStarted;
#else
            // Nuke Start Button
            else if (gameObject.GetComponentInParent<InteractableCollider>() is not null && gameObject.name.Contains("Button") && player.Room.Zone == MapGeneration.FacilityZone.Surface)
            {
                bool isKeycardActivated = Warhead.IsAuthorized;
                bool canBeStarted = !Warhead.IsDetonationInProgress && !Warhead.IsDetonated && Warhead.BaseController?.CooldownEndTime <= NetworkTime.time;
#endif

                // Return if:
                //  - interaction isn't enabled
                //  - MinimumPenetration isn't reached
                //  - warhead isn't activated
                //  - warhead lever isn't pulled
                //  - warhead is locked
                if (!Config.NukeCancelButton.IsEnabled
                    || (Config.NukeCancelButton.MinimumPenetration >= penetration / 100)
                    || !isKeycardActivated
                    || !canBeStarted
                    || !Warhead.LeverStatus)
                    return false;

                // Starts the nuke detonation countdown
                Warhead.Start();

                hasInteracted = true;
            }

            return hasInteracted;
        }

        private static bool HasPermission(Player player, IDoorPermissionRequester requester)
        {
            foreach (Item item in player.Items)
            {
                if (item.Base is not IDoorPermissionProvider provider)
                    continue;

                if (!requester.CheckPermissions(provider, out PermissionUsed callback))
                    continue;

                // Callback is null if the door/provider doesn't have any permission/none flag.
                if (callback != null && item.Base is SingleUseKeycardItem singleUseKeycard)
                    singleUseKeycard._destroyed = true;

                return true;
            }

            return false;
        }
    }
}
