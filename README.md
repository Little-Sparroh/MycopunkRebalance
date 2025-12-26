# MycopunkRebalance

A BepInEx mod for Mycopunk that rebalances various gameplay mechanics.

## Description

This mod modifies multiple aspects of Mycopunk gameplay to adjust balance, including movement abilities, weapon effectiveness, and equipment behavior.

## Features

- **Default Migration**: Enables firing weapons while sprinting and sliding
- **Default Cloud Skip**: Enables cloud skipping (double jump) at all times by allowing air jumps and adjusting jump speed
- **Default Structural Survey**: Makes the structural survey upgrade always active, highlighting low-health enemy parts
- **Default Magboots**: Enables magnetic boots and wallrunning by default
- **Enhanced Aiming**: Improves aiming mechanics, allowing aiming while reloading
- **Enhanced Strafing**: Enhances strafing capabilities in the wingsuit
- **Auto Cannon Nerf**: Reduces the effectiveness of the autocannon weapon
- **Amalgamation Flamethrower Nerf**: Reduces the effectiveness of the flamethrower weapon
- **Core Protection Rework**: Modifies enemy core protection mechanics, affecting how shells and damage are handled
- **Atmospheric Energizers Rework**: Updates the swarm gun or atmospheric energizers functionality

## Dependencies

* Mycopunk (base game)
* [BepInEx](https://github.com/BepInEx/BepInEx) - Version 5.4.2403 or compatible

## Installation

**Via Thunderstore (Recommended)**
1. Download and install via Thunderstore Mod Manager
2. The mod will be automatically installed to the correct location

**Manual Installation**
1. Download the mod package
2. Extract the .dll file to `<Mycopunk Directory>/BepInEx/plugins/`

## Configuration

The mod includes several configuration options that can be adjusted in the config file located at `<Mycopunk Directory>/BepInEx/config/sparroh.mycopunkrebalance.cfg`:

- **Movement Modifications**
  - CanFireWhileSprinting: Allows firing weapons while sprinting
  - CanFireWhileSliding: Allows firing weapons while sliding
  - CloudSkip: Enables cloud skipping ability

- **Structural Survey**
  - AlwaysActive: Makes structural survey always active

## Usage

Once installed, the mod loads automatically through BepInEx when the game starts. Check the BepInEx console for loading confirmation messages.

## Help

* **Plugin not loading?** Check BepInEx logs for errors and verify BepInEx version compatibility
* **Configuration not working?** Ensure the config file is in the correct location and restart the game after changes
* **Issues with specific features?** Check the CHANGELOG.md for recent updates and known issues

## Authors

* Sparroh

## License

This project is licensed under the MIT License - see the LICENSE file for details
