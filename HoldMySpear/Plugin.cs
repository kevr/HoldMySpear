/**
 * A simple spear ownership plugin for Valheim.
 * 
 * A spear is owned by a player when:
 * 1) it enters their inventory
 * 2) it is picked up from the ground
 * 
 * Once a spear is owned, no other user may pick it up.
 * 
 * NOTE: This mod requires itself to be installed on all
 * player clients participating in the behavior of HoldMySpear.
 **/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using UnityEngine;
using JetBrains.Annotations;
using HarmonyLib;
using HoldMySpear.Patches;
using ServerSync;

namespace HoldMySpear
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        // Constants
        internal const string NAME = "HoldMySpear";
        internal const string VERSION = "1.0.10";
        internal const string AUTHOR = "Kevver";
        internal const string GUID = AUTHOR + "." + NAME;

        // Harmony
        private readonly Harmony harmony = new(NAME);

        // Configuration
        static public ConfigEntry<bool> isModEnabled;

        // ServerSync
        ServerSync.ConfigSync configSync = new ServerSync.ConfigSync(GUID) {
            DisplayName = NAME,
            CurrentVersion = VERSION,
            MinimumRequiredVersion = VERSION
        };

        // Plugin initialization
        private void Awake()
        {
            // Setup project loggers
            BepInEx.Logging.Logger.Sources.Add(HoldMySpear.Patches.Drop.Logger);

            // Plugin startup logic
            Logger.LogInfo($"Plugin {NAME} is loaded!");

            // Configuration setup
            string desc = "Enable the mod (Synced with server)";
            isModEnabled = Config.Bind("General", "isModEnabled", true, desc);
            configSync.AddLockingConfigEntry<bool>(isModEnabled);

            // Patch assembly via harmony mocks
            Assembly assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
        }

        // Plugin shutdown
        private void Destroy()
        {
            Logger.LogInfo($"Plugin {NAME} destroyed.");
        }
    }
}
