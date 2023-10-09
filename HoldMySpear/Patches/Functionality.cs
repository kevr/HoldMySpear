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
using System.Text;
using BepInEx.Logging;
using HarmonyLib;
using Unity.Curl;

namespace HoldMySpear.Patches;

static public class DropCheck
{
    static public ManualLogSource Logger = new ManualLogSource($"{Plugin.NAME}.DropCheck");

    static public bool IsSpear(ItemDrop.ItemData item)
    {
        return item.m_shared.m_skillType == Skills.SkillType.Spears;
    }

    static public bool Ownership(Dictionary<string, string> customData)
    {
        string playerName = Player.m_localPlayer.GetPlayerName();
        return customData.ContainsKey("owner") && customData["owner"] == playerName;
    }

    static public string Owner(ItemDrop.ItemData item, bool enforceOwnership)
    {
        if (item.m_customData.ContainsKey("owner"))
        {
            string owner = item.m_customData["owner"];

            if (enforceOwnership)
            {
                ref Player player = ref Player.m_localPlayer;
                if (owner != player.GetPlayerName())
                {
                    player.Message(MessageHud.MessageType.Center,
                        $"Get your own spear, this one is held by {owner}!");
                }
            }

            return owner;
        }
        return null;
    }
}

[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int),
    typeof(bool), typeof(float))]
static class ItemDropItemDataGetTooltipPatch
{
    static void Postfix(ItemDrop.ItemData item, int qualityLevel, bool crafting, ref string __result)
    {
        if (item == null || !DropCheck.IsSpear(item))
            return;

        StringBuilder sb = new StringBuilder();
        sb.Append($"{Environment.NewLine}{Environment.NewLine}");

        if (item.m_customData.ContainsKey("owner"))
        {
            string owner = item.m_customData["owner"];
            sb.Append($"Owned by: {owner}");
        }

        __result += sb.ToString();
    }
}

[HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Pickup))]
static class ItemDropPickup
{
    static bool Prefix(ItemDrop __instance)
    {
        // If the drop is not a spear, bypass this function.
        if (!DropCheck.IsSpear(__instance.m_itemData))
            return true;

        // A local reference to item's custom data.
        ref Dictionary<string, string> customData = ref __instance.m_itemData.m_customData;

        string owner = DropCheck.Owner(__instance.m_itemData, true);
        bool isOwned = owner != null;

        ref Player player = ref Player.m_localPlayer;
        string playerName = player.GetPlayerName();

        if (isOwned && owner != playerName)
            return false;

        customData["owner"] = playerName;
        owner = customData["owner"];

        if (!isOwned)
            DropCheck.Logger.LogInfo($"{owner} has obtained a spear.");

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

[HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveItemToThis), typeof(Inventory), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int))]
static class InventoryMoveItemToThisXY
{
    static bool Prefix(Inventory fromInventory, ItemDrop.ItemData item, int amount, int x, int y)
    {
        bool toPlayer = !fromInventory.Equals(Player.m_localPlayer.GetInventory());
        if (!toPlayer)
            return true;

        bool addable = Utility.Addable(item);
        DropCheck.Owner(item, true);
        return addable;
    }
}