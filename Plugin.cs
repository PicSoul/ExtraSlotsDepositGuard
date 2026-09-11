using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace ExtraSlotsDepositGuard
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(NearbyCraftingGuid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(ExtraSlotsGuid, BepInDependency.DependencyFlags.HardDependency)]
    public sealed class DepositGuardPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.pics0ul.valheim.extraslotsdepositguard";
        public const string PluginName = "ExtraSlots Deposit Guard";
        public const string PluginVersion = "1.0.1";

        internal const string NearbyCraftingGuid = "com.mikeg.valheim.nearbycrafting";
        internal const string ExtraSlotsGuid = "shudnal.ExtraSlots";

        internal static ManualLogSource ModLogger;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> ProtectQuickSlots;
        internal static ConfigEntry<bool> ProtectMiscSlots;
        internal static ConfigEntry<bool> ProtectAmmoSlots;
        internal static ConfigEntry<bool> ProtectFoodSlots;
        internal static ConfigEntry<bool> ProtectEquipmentSlots;
        internal static ConfigEntry<bool> DebugLogging;

        private Harmony _harmony;

        internal static bool DebugEnabled => DebugLogging != null && DebugLogging.Value;

        private void Awake()
        {
            ModLogger = Logger;

            Enabled = Config.Bind("General", "Enabled", true,
                "Keep Nearby Crafting's quick-deposit hotkey from emptying Extra Slots slots.");

            ProtectQuickSlots = Config.Bind("Protected Slots", "QuickSlots", true,
                "Never quick-deposit items sitting in Extra Slots quick slots.");
            ProtectMiscSlots = Config.Bind("Protected Slots", "MiscSlots", true,
                "Never quick-deposit items sitting in Extra Slots misc slots (trophies, fish, coins, keys, quest items).");
            ProtectAmmoSlots = Config.Bind("Protected Slots", "AmmoSlots", true,
                "Never quick-deposit items sitting in Extra Slots ammo slots.");
            ProtectFoodSlots = Config.Bind("Protected Slots", "FoodSlots", true,
                "Never quick-deposit items sitting in Extra Slots food slots.");
            ProtectEquipmentSlots = Config.Bind("Protected Slots", "EquipmentSlots", true,
                "Never quick-deposit items sitting in Extra Slots equipment slots, including extra utility slots and custom slots added through the Extra Slots API.");

            DebugLogging = Config.Bind("Debug", "DebugLogging", false,
                "Log every item this mod holds back from a quick-deposit.");

            if (!ExtraSlotsApi.Resolve())
            {
                Logger.LogError("Could not bind to the Extra Slots API. Quick-deposit protection is inactive.");
                return;
            }

            MethodInfo massQuickDeposit = FindMassQuickDeposit();
            if (massQuickDeposit == null)
            {
                Logger.LogError(
                    "Could not find NearbyCrafting's MassQuickDeposit method. " +
                    "Nearby Crafting has probably changed; quick-deposit protection is inactive.");
                return;
            }

            try
            {
                _harmony = new Harmony(PluginGuid);

                _harmony.Patch(
                    massQuickDeposit,
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(MassQuickDepositScope), nameof(MassQuickDepositScope.Prefix))),
                    finalizer: new HarmonyMethod(AccessTools.Method(typeof(MassQuickDepositScope), nameof(MassQuickDepositScope.Finalizer))));

                // Run ahead of anything else hooking MoveItemToThis so a protected item is
                // rejected before another mod does work on the strength of a move that is
                // not going to happen.
                var moveGuard = new HarmonyMethod(AccessTools.Method(typeof(MoveItemGuard), nameof(MoveItemGuard.Prefix)))
                {
                    priority = Priority.First
                };

                _harmony.Patch(
                    AccessTools.Method(typeof(Inventory), nameof(Inventory.MoveItemToThis),
                        new[] { typeof(Inventory), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int) }),
                    prefix: moveGuard);

                Logger.LogInfo(PluginName + " " + PluginVersion + " loaded; protecting Extra Slots slots from quick-deposit.");
            }
            catch (Exception ex)
            {
                try { _harmony?.UnpatchSelf(); } catch { }
                _harmony = null;
                Logger.LogError("Failed to apply Harmony patches; quick-deposit protection is inactive.");
                Logger.LogError(ex);
            }
        }

        private void OnDestroy()
        {
            try { _harmony?.UnpatchSelf(); } catch { }
            _harmony = null;
            MassQuickDepositScope.Reset();
        }

        // NearbyCrafting.NearbyCraftingPlugin.MassQuickDeposit(Player) is private static.
        private static MethodInfo FindMassQuickDeposit()
        {
            Type pluginType = AccessTools.TypeByName("NearbyCrafting.NearbyCraftingPlugin");
            if (pluginType == null)
                return null;

            return AccessTools.Method(pluginType, "MassQuickDeposit", new[] { typeof(Player) });
        }

        internal static void Debug(string message)
        {
            if (DebugEnabled)
                ModLogger.LogInfo(message);
        }
    }

    /// <summary>
    /// Late-bound access to shudnal's Extra Slots. The API is resolved by name so this mod
    /// needs no compile-time reference to ExtraSlots.dll or ExtraSlotsAPI.dll.
    /// </summary>
    internal static class ExtraSlotsApi
    {
        private static Func<List<ItemDrop.ItemData>> _quickSlotItems;
        private static Func<List<ItemDrop.ItemData>> _miscSlotItems;
        private static Func<List<ItemDrop.ItemData>> _ammoSlotItems;
        private static Func<List<ItemDrop.ItemData>> _foodSlotItems;
        private static Func<List<ItemDrop.ItemData>> _equipmentSlotItems;

        internal static bool Resolve()
        {
            Type api = AccessTools.TypeByName("ExtraSlots.API");
            if (api == null)
                return false;

            _quickSlotItems = BindGetter(api, "GetQuickSlotsItems");
            _miscSlotItems = BindGetter(api, "GetMiscSlotsItems");
            _ammoSlotItems = BindGetter(api, "GetAmmoSlotsItems");
            _foodSlotItems = BindGetter(api, "GetFoodSlotsItems");
            _equipmentSlotItems = BindGetter(api, "GetEquipmentSlotsItems");

            return _quickSlotItems != null
                && _miscSlotItems != null
                && _ammoSlotItems != null
                && _foodSlotItems != null
                && _equipmentSlotItems != null;
        }

        private static Func<List<ItemDrop.ItemData>> BindGetter(Type api, string name)
        {
            MethodInfo method = AccessTools.Method(api, name, Type.EmptyTypes);
            if (method == null || !typeof(IEnumerable).IsAssignableFrom(method.ReturnType))
            {
                DepositGuardPlugin.ModLogger.LogWarning("Extra Slots API method '" + name + "' is missing or has an unexpected shape.");
                return null;
            }

            return () =>
            {
                var items = new List<ItemDrop.ItemData>();
                if (!(method.Invoke(null, null) is IEnumerable raw))
                    return items;

                foreach (object entry in raw)
                {
                    if (entry is ItemDrop.ItemData item)
                        items.Add(item);
                }
                return items;
            };
        }

        internal static void AddProtectedItems(HashSet<ItemDrop.ItemData> target)
        {
            Collect(target, _quickSlotItems, DepositGuardPlugin.ProtectQuickSlots.Value, "quick");
            Collect(target, _miscSlotItems, DepositGuardPlugin.ProtectMiscSlots.Value, "misc");
            Collect(target, _ammoSlotItems, DepositGuardPlugin.ProtectAmmoSlots.Value, "ammo");
            Collect(target, _foodSlotItems, DepositGuardPlugin.ProtectFoodSlots.Value, "food");
            Collect(target, _equipmentSlotItems, DepositGuardPlugin.ProtectEquipmentSlots.Value, "equipment");
        }

        private static void Collect(HashSet<ItemDrop.ItemData> target, Func<List<ItemDrop.ItemData>> getter, bool wanted, string category)
        {
            if (!wanted || getter == null)
                return;

            try
            {
                foreach (ItemDrop.ItemData item in getter())
                {
                    if (item != null)
                        target.Add(item);
                }
            }
            catch (Exception ex)
            {
                DepositGuardPlugin.ModLogger.LogWarning(
                    "Could not read Extra Slots " + category + " slots (" + ex.GetType().Name + "): " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Opens a protection window for the duration of one Nearby Crafting mass quick-deposit and
    /// snapshots the items that must stay where they are.
    /// </summary>
    internal static class MassQuickDepositScope
    {
        private static readonly HashSet<ItemDrop.ItemData> ProtectedItems =
            new HashSet<ItemDrop.ItemData>(ReferenceComparer.Instance);

        private static Inventory _guardedInventory;
        private static int _depth;
        internal static int BlockedMoves;

        internal static bool IsProtected(Inventory fromInventory, ItemDrop.ItemData item)
        {
            return _depth > 0
                && item != null
                && fromInventory != null
                && ReferenceEquals(fromInventory, _guardedInventory)
                && ProtectedItems.Contains(item);
        }

        internal static void Prefix(Player player)
        {
            // Re-entrancy guard: only the outermost call owns the snapshot.
            if (_depth++ > 0)
                return;

            ProtectedItems.Clear();
            BlockedMoves = 0;
            _guardedInventory = null;

            if (DepositGuardPlugin.Enabled == null || !DepositGuardPlugin.Enabled.Value)
                return;

            if (player == null)
                return;

            Inventory inventory = player.GetInventory();
            if (inventory == null)
                return;

            _guardedInventory = inventory;

            try
            {
                ExtraSlotsApi.AddProtectedItems(ProtectedItems);
            }
            catch (Exception ex)
            {
                DepositGuardPlugin.ModLogger.LogWarning(
                    "Could not snapshot Extra Slots contents (" + ex.GetType().Name + "): " + ex.Message);
                ProtectedItems.Clear();
            }

            DepositGuardPlugin.Debug("Quick-deposit started; holding back " + ProtectedItems.Count + " item stack(s) in Extra Slots.");
        }

        internal static Exception Finalizer(Exception __exception)
        {
            if (_depth > 0)
                _depth--;

            if (_depth == 0)
            {
                if (BlockedMoves > 0)
                    DepositGuardPlugin.Debug("Quick-deposit finished; blocked " + BlockedMoves + " move(s) out of Extra Slots.");

                ProtectedItems.Clear();
                _guardedInventory = null;
                BlockedMoves = 0;
            }

            return __exception;
        }

        internal static void Reset()
        {
            _depth = 0;
            ProtectedItems.Clear();
            _guardedInventory = null;
            BlockedMoves = 0;
        }

        private sealed class ReferenceComparer : IEqualityComparer<ItemDrop.ItemData>
        {
            internal static readonly ReferenceComparer Instance = new ReferenceComparer();

            public bool Equals(ItemDrop.ItemData x, ItemDrop.ItemData y) => ReferenceEquals(x, y);

            public int GetHashCode(ItemDrop.ItemData obj) =>
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }

    /// <summary>
    /// Cancels the individual item moves Nearby Crafting attempts out of protected slots.
    /// Nearby Crafting measures progress by watching the source stack size, so a cancelled move
    /// simply counts as zero deposited and it moves on to the next item.
    /// </summary>
    internal static class MoveItemGuard
    {
        internal static bool Prefix(Inventory fromInventory, ItemDrop.ItemData item, ref bool __result)
        {
            if (!MassQuickDepositScope.IsProtected(fromInventory, item))
                return true;

            MassQuickDepositScope.BlockedMoves++;

            if (DepositGuardPlugin.DebugEnabled)
            {
                string name = item.m_shared != null ? item.m_shared.m_name : "<unknown>";
                DepositGuardPlugin.Debug(
                    "Held back '" + name + "' at slot " + item.m_gridPos.x + "," + item.m_gridPos.y + " during quick-deposit.");
            }

            __result = false;
            return false;
        }
    }
}
