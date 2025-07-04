# ShootingInteractions ![Version](https://img.shields.io/github/v/release/Unbistrackted/ShootingInteractions?style=plastic&label=Version&color=dc3e3e) ![Downloads](https://img.shields.io/github/downloads/Unbistrackted/ShootingInteractions/total?style=plastic&label=Downloads&color=50f63f) 

A plugin that adds new interactions when shooting on objects!

## Interactions
* Interact with doors/gates by shooting on their buttons
* Interact with elevators by shooting on their buttons
* Interact with lockers and bulletproof lockers/pedestals when shooting on them
* Interact with nuke buttons by shooting on them
* Make grenades/SCP-018 ignite when shooting on them, and (not for custom grenades) choose if they explode instantly or not
* Make SCP-2176 and SCP-244 shatter when shooting on it

## Other features
* Bypass mode support
* Random chance for buttons to "break/lock" when shooting on them, you can configure for how long, and if the door/elevator should move before the buttons break/lock
* Random chance for grenades to "malfunction" when shooting on them, you can configure the fuse time of the grenade (time it takes before exploding)
* Remote keycard (Check keycards in the player's inventory to open doors that requires a keycard)

## Configuration Settings:
| Setting Key | Value Type | Default Value | Description |
-------------------------------|------------|---------------|------------------------------------------------------------------------------------------------
| is_enabled | bool | true | Is the plugin enabled
| debug | bool | false | Are the plugin's debug logs enabled
| accurate_bullets | bool | false | Should it take into account where the bullet actually lands, instead of the center of the player's screen
| doors | DoorsInteraction | See DoorInteraction config | Default doors buttons interaction
| checkpoints | DoorsInteraction | See DoorInteraction config | Checkpoint doors buttons interaction
| gates | DoorsInteraction | See DoorInteraction config | Gates buttons interaction
| weapon_grid_lockers | LockersInteraction | See LockersInteraction config | Weapon grid lockers interaction
| bulletproof_lockers | BulletproofLockersInteraction | See BulletproofLockersInteraction config | Pedestals/Bulletproof lockers interaction
| rifle_rack_lockers | LockersInteraction | See LockersInteraction config | Rifle rack interaction
| experimental_weapon_lockers | LockersInteraction | See LockersInteraction config | Experimental weapon lockers interaction
| scp127_container | LockersInteraction | See LockersInteraction config | SCP-127 container interaction
| elevators | ElevatorsInteraction | See ElevatorsInteractionconfig | Elevators buttons interaction
| frag_grenades | TimedProjectilesInteraction | See TimedProjectilesInteraction config | Frag grenades interaction
| flashbangs | TimedProjectilesInteraction | See TimedProjectilesInteraction config | Flashbangs interaction
| custom_grenades | ProjectilesInteraction | See ProjectilesInteraction config | Custom grenades interaction
| nuke_start_button | NukeButtonsInteraction  | See NukeButtonsInteraction config | Nuke start button interaction
| nuke_cancel_button | NukeButtonsInteraction  | See NukeButtonsInteraction config | Nuke cancel button interaction
| scp018 | TimedProjectilesInteraction | See TimedProjectilesInteraction config | SCP-018 interaction
| scp2176 | ProjectilesInteraction | See ProjectilesInteraction config | SCP-2176 interaction
| scp244 | ProjectilesInteraction | See ProjectilesInteraction config | SCP-244 interaction

## DoorsInteraction Configuration Settings
| Setting Key | Value Type | Default Value | Description |
-------------------------------|------------|---------------|------------------------------------------------------------------------------------------------
| is_enabled | bool | true | Is the interaction enabled
| minimum_penetration | float | 0 | What's the weapon's minimum armor penetration percentage for the interaction to occur (0 is disabled)
| remote_keycard | bool | false | Should it check keycards in the player's inventory
| lock_chance | byte | 0 | Chance for the door to get locked after shooting (0 is disabled | 100 is always)
| lock_duration | float | 10 | For how long should the door stay locked (0 or less is infinite)
| move_before_breaking | bool | true | Should the door open/close before getting locked

## LockersInteraction Configuration Settings
| Config | Value Type | Default Value | Description |
-------------------------------|------------|---------------|------------------------------------------------------------------------------------------------
| is_enabled | bool | true | Is the interaction enabled
| minimum_penetration | float | 0 | What's the weapon's minimum armor penetration percentage for the interaction to occur (0 is disabled)
| remote_keycard | bool | true | Should it check keycards in the player's inventory

## BulletproofLockersInteraction Configuration Settings
| Setting Key | Value Type | Default | Description |
-------------------------------|------------|---------------|------------------------------------------------------------------------------------------------
| is_enabled | bool | true | Is the interaction enabled
| minimum_penetration | float | 0 | What's the weapon's minimum armor penetration percentage for the interaction to occur (0 is disabled)
| remote_keycard | bool | true | Should it check keycards in the player's inventory
| only_keypad | bool | true | Should it only work when shooting on the keypad (false is whole interactable)

## ElevatorsInteraction Configuration Settings
| Setting Key | Value Type | Default Value | Description |
-------------------------------|------------|---------------|------------------------------------------------------------------------------------------------
| is_enabled | bool | true | Is the interaction enabled
| minimum_penetration | float | 0 | What's the weapon's minimum armor penetration percentage for the interaction to occur (0 is disabled)
| lock_chance | byte | 0 | Chance for the elevator to get locked after shooting (0 is disabled | 100 is always)
| lock_duration | float | 10 | How long should the elevator stay locked (0 or less is infinite)
| move_before_breaking | bool | true | Should the elevator move before getting locked

## ProjectilesInteraction Configuration Settings
| Setting Key | Value Type | Default Value | Description |
-------------------------------|------------|---------------|------------------------------------------------------------------------------------------------
| is_enabled | bool | true | Is the interaction enabled
| minimum_penetration | float | 0 | What's the weapon's minimum armor penetration percentage for the interaction to occur (0 is disabled)

## TimedProjectilesInteraction Configuration Settings
| Setting Key | Value Type | Default Value | Description |
-------------------------------|------------|---------------|------------------------------------------------------------------------------------------------
| is_enabled | bool | true | Is the interaction enabled
| minimum_penetration | float | 0 | What's the weapon's minimum armor penetration percentage for the interaction to occur (0 is disabled)
| custom_fuse_time_chance | byte | 0 | Chance to have a custom fuse time (0 is disabled | 100 is always)
| custom_fuse_time_duration | float | 0 | Custom fuse time (in seconds)
| additional_velocity | bool | false | Should the bullet affect the object's velocity
| velocity_force | float | 7.5 | The force of the bullet that gets multiplied with the object velocity
| scale_with_penetration | bool | false | Should the bullet armor penetration affect the object's velocity
| velocity_penetration_multiplier | float | 7.5 | Constant that gets multiplied with the object velocity and bullet armor penetration

## NukeButtonsInteraction Configuration Settings
| Setting Key | Value Type | Default Value | Description |
-------------------------------|------------|---------------|------------------------------------------------------------------------------------------------
| is_enabled | bool | true | Is the interaction enabled
| minimum_penetration | float | 0 | What's the weapon's minimum armor penetration percentage for the interaction to occur (0 is disabled)


## Download

This plugin requires [Exiled](https://github.com/ExMod-Team/EXILED/) or [LabAPI](https://github.com/northwood-studios/LabAPI/).

You can download the latest version of ShootingInteractions [here](https://github.com/Unbistrackted/ShootingInteractions/releases/latest).

## Suggestions

If there's any issues, if you have any __suggestions__ or if you want to __help me improve the code__, feel free to DM me on **Discord** or open a **Github Issue**

- Discord : @unbistrackted

## Special thanks to:

https://github.com/IkaOverride for giving the permission to continue the development of [ShootingInteractions](https://github.com/Unbistrackted/ShootingInteractions).

