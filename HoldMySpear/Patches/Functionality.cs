/**
 * HarmonyLib decorators and signatures were taken from BindOnEquip
 * by Azumatt, as well as this file's location in the project structure.
 * 
 * BindOnEquip repository: https://github.com/AzumattDev/BindOnEquip/
 **/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using BepInEx.Logging;
using HarmonyLib;

namespace HoldMySpear.Patches;

static public class DropCheck
{
    static public ManualLogSource Logger = new ManualLogSource($"{PluginInfo.PLUGIN_NAME}.DropCheck");

    static public bool IsSpear(ItemDrop.ItemData item)
    {
        return item.m_shared.m_skillType == Skills.SkillType.Spears;
    }

    static public bool Ownership(Dictionary<string, string> customData)
    {
        string playerName = Player.m_localPlayer.GetPlayerName();
        return customData.ContainsKey("owner") && customData["owner"] == playerName;
    }
}

[HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Pickup))]
static class ItemDropPickup
{
    static bool Prefix(ItemDrop __instance)
    {
        if (!DropCheck.IsSpear(__instance.m_itemData))
        {
            return true;
        }
        else
        {
            DropCheck.Logger.LogInfo("Found a spear!");
        }

        string owner = "(NOT SET)";
        if (__instance.m_itemData.m_customData.ContainsKey("owner"))
        {
            owner = __instance.m_itemData.m_customData["owner"];
        }
        DropCheck.Logger.LogInfo($"(Pre) Spear owned by {owner}");

        string playerName = Player.m_localPlayer.GetPlayerName();

        if (__instance.m_itemData.m_customData.ContainsKey("owner") && __instance.m_itemData.m_customData["owner"] != playerName)
            return false;

        __instance.m_itemData.m_customData["owner"] = playerName;
        owner = __instance.m_itemData.m_customData["owner"];
        DropCheck.Logger.LogInfo($"Spear owned by {owner}");
        return true;
    }
}

static class Utility
{
    // A functor expressing the ability to add an `item`
    // to the local player's inventory.
    static public bool Addable(ItemDrop.ItemData item)
    {
        bool isSpear = false;
        if (item == null || !(isSpear = DropCheck.IsSpear(item)))
        {
            return true;
        }

        if (!item.m_customData.ContainsKey("owner") && isSpear)
        {
            item.m_customData["owner"] = Player.m_localPlayer.GetPlayerName();
        }

        return DropCheck.Ownership(item.m_customData);
    }
}

[HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), typeof(ItemDrop.ItemData), typeof(int))]
static class ItemDropAddable
{
    static bool Prefix(Inventory __instance, ItemDrop.ItemData item, int stack = -1)
    {
        return Utility.Addable(item);
    }
}

[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData))]
static class ItemDropAdd
{
    static bool Prefix(Inventory __instance, ItemDrop.ItemData item)
    {
        return Utility.Addable(item);
    }
}