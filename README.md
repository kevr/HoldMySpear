HoldMySpear
-----------

A simple spear ownership mod for Valheim.

Why? As a spear user, when playing in multi-player with friends, spears
cannot be thrown without risking pickup by another player. Because of this,
spear users must essentially opt-out of using the spear's special ability
when playing with others to avoid confusion and/or lose their weapon.

This mod solves that issue by providing permanent player ownership of
spears.

## Overview

By default, spears are not owned by players. With HoldMySpears,
once a player obtains a spear in their inventory, that spear is
then bound to that player indefinitely.

- [Tips](#tips)
- [Dependencies](#dependencies)
- [Requirements](#requirements)
- [Installation](#installation)
- [Accreditation](#accreditation)

## Tips

Just like binding a spear to yourself, other players in the world may
bind any unbound spear to themselves; for this reason, it's a good idea
to take ownership of any spears outside of your inventory when first
running this mod (spears in storage, item stands, etc).

## Dependencies:

- BepInEx 5.4.2200

## Requirements

This mod must be installed on all clients who participate; players
without this mod will be able to pick up all spears in the world
they have access to.

## Installation

Place `HoldMySpear.dll` into `C:\Users\<username>\AppData\LocalLow\IronGate\Valheim\BepInEx\plugins` and run Valheim.

## Accreditation

Much of the understanding of BepInEx, Valheim and HarmonyX APIs
was derived through [Azumatt](https://github.com/AzumattDev)'s
[BindOnEquip](https://github.com/AzumattDev/BindOnEquip) plugin.

The name of this project was suggested by Chee, a user on Discord.