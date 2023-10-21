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
using System.Security.Permissions;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Unity.Curl;
using UnityEngine;

namespace HoldMySpear.Patches;

static public class Drop
{
    // Constants
    public const string OWNER_KEY = "owner";

    // Statics
    static public ManualLogSource Logger = new ManualLogSource($"{Plugin.NAME}.Drop");

    static public bool IsSpear(ItemDrop.ItemData item)
    {
        return item != null && item.m_shared.m_skillType == Skills.SkillType.Spears;
    }

    static public bool IsOwned(ItemDrop.ItemData item)
    {
        ref Dictionary<string, string> data = ref item.m_customData;
        return data.ContainsKey(OWNER_KEY) && data[OWNER_KEY] != null;
    }

    static public bool IsOwner(ItemDrop.ItemData item)
    {
        ref Dictionary<string, string> data = ref item.m_customData;
        ref Player player = ref Player.m_localPlayer;
        return data.ContainsKey(OWNER_KEY) && data[OWNER_KEY] == player.GetPlayerName();
    }

    static public string Owner(ItemDrop.ItemData item)
    {
        if(!item.m_customData.ContainsKey(OWNER_KEY))
        {
            item.m_customData[OWNER_KEY] = null;
        }
        return item.m_customData[OWNER_KEY];
    }
}

static class Utility
{
    // A functor expressing the ability to add an `item`
    // to the local player's inventory.
    static public bool Addable(ItemDrop.ItemData item)
    {
        if (!Drop.IsSpear(item))
            return true;

        return !Drop.IsOwned(item) || Drop.IsOwner(item);
    }

    static public ZDO GetZDO(ref ItemDrop item)
    {
        ZNetView view = item.GetComponent<ZNetView>();
        return view.GetZDO();
    }

    static public void Save(ref ItemDrop item)
    {
        ZDO zdo = GetZDO(ref item);
        ItemDrop.SaveToZDO(item.m_itemData, zdo);
    }

    static public void Load(ref ItemDrop item)
    {
        item.Load();
    }
}

[HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.DropItems))]
static class CharacterDropDropItems
{
    static void Postfix(List<KeyValuePair<GameObject, int>> drops, UnityEngine.Vector3 centerPos, float dropArea)
    {
        foreach (KeyValuePair<GameObject, int> drop in drops)
        {
            ItemDrop item = drop.Key.GetComponent<ItemDrop>();
            if (item == null)
                continue;

            if (Drop.IsSpear(item.m_itemData) && Drop.IsOwned(item.m_itemData))
                Utility.Save(ref item);
        }
    }
}

[HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.GetHoverText))]
static class ItemDropGetHoverText
{
    static void Postfix(ItemDrop __instance, ref string __result)
    {
        if (!__instance)
            return;

        // Load ZDO
        Utility.Load(ref __instance);

        if (!__instance.m_itemData.m_customData.ContainsKey(Drop.OWNER_KEY))
            return;

        string owner = __instance.m_itemData.m_customData[Drop.OWNER_KEY];
        if (Drop.IsOwner(__instance.m_itemData))
        {
            string hotkey = "L-Alt + E";
            string label = "Disown";
            __result += $"{Environment.NewLine}[<color=yellow><b>{hotkey}</b></color>] {label}";
        }
        else
        {
            __result += $"{Environment.NewLine}{Environment.NewLine}<b>Owned by:</b> <color=orange>{owner}</color>";
        }
    }
}

[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), typeof(ItemDrop.ItemData), typeof(int),
    typeof(bool), typeof(float))]
static class ItemDropItemDataGetTooltipPatch
{
    static void Postfix(ItemDrop.ItemData item, int qualityLevel, bool crafting, ref string __result)
    {

        if (!Drop.IsSpear(item))
            return;

        StringBuilder sb = new StringBuilder();
        sb.Append($"{Environment.NewLine}{Environment.NewLine}");

        if (item.m_customData.ContainsKey(Drop.OWNER_KEY))
        {
            string owner = item.m_customData[Drop.OWNER_KEY];
            sb.Append($"<b>Owned by:</b> <color=orange>{owner}</color>");
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
        if (!Drop.IsSpear(__instance.m_itemData))
            return true;

        // A local reference to item's custom data.
        ref Dictionary<string, string> customData = ref __instance.m_itemData.m_customData;

        string owner = Drop.Owner(__instance.m_itemData);

        ref Player player = ref Player.m_localPlayer;
        string playerName = player.GetPlayerName();

        bool isOwned = Drop.IsOwned(__instance.m_itemData);
        if (isOwned)
        {
            if (owner != playerName)
            {
                string message = $"Get your own spear, this one is held by {owner}!";
                player.Message(MessageHud.MessageType.Center, message);
                return false;
            }
            else if (Input.GetKey(KeyCode.LeftAlt))
            {
                customData.Remove(Drop.OWNER_KEY);
                Utility.Save(ref __instance);
                Drop.Logger.LogInfo("(ItemDropPickup) -> Owner removed");
                return false;
            }
        }

        Drop.Logger.LogInfo($"(ItemDropPickup) -> Updated owner to {playerName}");

        customData[Drop.OWNER_KEY] = playerName;
        owner = customData[Drop.OWNER_KEY];

        if (!isOwned)
        {
            Drop.Logger.LogInfo($"(ItemDropPickup) -> {owner} has obtained a spear.");
        }

        return true;
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
        if (!Drop.IsSpear(item))
            return true;

        bool addable = Utility.Addable(item);
        if (addable && !Drop.IsOwned(item))
        {
            item.m_customData[Drop.OWNER_KEY] = Player.m_localPlayer.GetPlayerName();
            Drop.Logger.LogInfo($"(ItemDropAdd) -> Updated owner to {item.m_customData[Drop.OWNER_KEY]}");
        }

        return addable;
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

        if (!addable)
        {
            string owner = Drop.Owner(item);
            string message = $"Get your own spear, this one is held by {owner}!";
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, message);
        }

        return addable;
    }
}