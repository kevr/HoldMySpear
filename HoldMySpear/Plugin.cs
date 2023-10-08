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

namespace HoldMySpear
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        // Constants
        internal const string NAME = "HoldMySpear";
        internal const string VERSION = "1.0.3";
        internal const string AUTHOR = "Kevver";
        internal const string GUID = AUTHOR + "." + NAME;

        private readonly Harmony harmony = new(NAME);

        // Plugin initialization
        private void Awake()
        {
            // Setup project loggers
            BepInEx.Logging.Logger.Sources.Add(DropCheck.Logger);

            // Plugin startup logic
            Logger.LogInfo($"Plugin {NAME} is loaded!");

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
