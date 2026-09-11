# ExtraSlots Deposit Guard

**Your quick-deposit hotkey should fill chests, not empty your gear.**

A small compatibility patch between [Nearby Crafting](https://thunderstore.io/c/valheim/p/IPA38/NearbyCrafting/) and [Extra Slots](https://thunderstore.io/c/valheim/p/shudnal/ExtraSlots/). Install it and forget about it.

## The problem

Nearby Crafting's Mass Quick Deposit hotkey (`F6` by default) is great: one keypress and every matching stack in your inventory flies into the chests around you.

Extra Slots keeps your equipment, quick, food, ammo and misc slots *inside* that same inventory. Nearby Crafting can't tell them apart from ordinary backpack space.

So you walk up to your storage, press `F6`, and your quiver empties into the ammo chest. Your cooked food goes into the food chest. Your spare trophies and coins vanish into the misc chest. You find out in the middle of the next fight.

## The fix

This mod holds those slots back. Press `F6` exactly like before — your regular inventory deposits, your slots stay put.

| Stays with you | Still deposits |
| --- | --- |
| Equipment slots | Your regular inventory |
| Quick slots | Extra Slots' extra **rows** |
| Food slots | Everything else, as before |
| Ammo slots | |
| Misc slots (trophies, fish, coins, keys, quest items) | |
| Custom slots from other mods (backpacks, quivers, rings…) | |

Nothing else changes. The deposit still reaches every eligible chest in range, and Nearby Crafting's on-screen message ("Quick Deposit: 47 items deposited into 3 chests") stays accurate — held-back items simply aren't counted.

Note that Extra Slots' extra inventory **rows** are ordinary inventory and still deposit normally. Only true slots are protected.

## Why this needs a separate mod

Extra Slots already protects its slots from Valheim's built-in "stack all" button. But Nearby Crafting doesn't use the vanilla stacking code — it has its own deposit routine, which Extra Slots has never heard of.

Neither mod is doing anything wrong. They just don't know about each other, so the fix belongs in a third mod rather than a fork of either one.

## Requirements

- [BepInEx](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [Nearby Crafting](https://thunderstore.io/c/valheim/p/IPA38/NearbyCrafting/) by IPA38
- [Extra Slots](https://thunderstore.io/c/valheim/p/shudnal/ExtraSlots/) by shudnal

Both are hard dependencies. If either is missing, this mod does nothing at all — it won't break anything, it just has no job to do.

Client-side only. No server install needed, and it doesn't matter what other players have.

## Configuration

Everything is protected by default, so most people never need to touch this.

`BepInEx/config/com.pics0ul.valheim.extraslotsdepositguard.cfg`

```ini
[General]
Enabled = true

[Protected Slots]
QuickSlots = true
MiscSlots = true
AmmoSlots = true
FoodSlots = true
EquipmentSlots = true

[Debug]
DebugLogging = false
```

Turn an individual category off if you actually *want* the hotkey to drain it. For example, `AmmoSlots = false` lets `F6` dump spare arrows into your ammo chest while still protecting everything else.

`EquipmentSlots` also covers extra utility slots and any custom slots added by other mods through the Extra Slots API.

Settings can be changed in a config manager in-game, or by editing the file.

## How it works

For the curious — you don't need to read this to use the mod.

While a quick-deposit is running, the mod takes a snapshot of what's currently sitting in your Extra Slots slots (via Extra Slots' public API) and cancels any attempt to move those particular items into a chest. The moment the deposit finishes, the mod goes completely dormant again.

No items are ever moved, hidden, or temporarily pulled out of your inventory to achieve this, so there's no window in which a crash or another misbehaving mod could lose them.

If a future update to either mod changes what this patch hooks into, it logs an error and disables itself rather than half-working. Nearby Crafting and Extra Slots keep running normally; you'd just lose the protection until this mod catches up. Please report it if that happens.

## Troubleshooting

Set `DebugLogging = true` and check `BepInEx/LogOutput.log`. On a healthy startup you'll see:

```text
[Info : ExtraSlots Deposit Guard] ExtraSlots Deposit Guard 1.0.1 loaded; protecting Extra Slots slots from quick-deposit.
```

With debug logging on, each held-back item is reported as it happens.

## Credits

This mod contains no code from either mod it patches. It reaches Extra Slots through that mod's public API.

- Nearby Crafting by **IPA38**
- Extra Slots by **shudnal**

Thanks to both for making mods worth making compatible.

## Source

[github.com/PicSoul/ExtraSlotsDepositGuard](https://github.com/PicSoul/ExtraSlotsDepositGuard)

Bug reports and pull requests welcome. Build instructions are in `BUILDING.md`.

## License

MIT — see `LICENSE`. Fork it, fix it, keep it alive if I go quiet.
