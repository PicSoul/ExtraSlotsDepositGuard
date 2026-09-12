# ExtraSlots Deposit Guard

**Your deposit hotkey should fill chests, not empty your gear.**

A small compatibility patch that keeps bulk-deposit features away from your [Extra Slots](https://thunderstore.io/c/valheim/p/shudnal/ExtraSlots/) slots. Works with [Nearby Crafting](https://thunderstore.io/c/valheim/p/IPA38/NearbyCrafting/), [StoreAndCraft](https://thunderstore.io/c/valheim/p/Morda/StoreAndCraft/), or both at once. Install it and forget about it.

## The problem

Bulk deposit is great: one keypress and every matching stack in your inventory flies into the chests around you.

Extra Slots keeps your equipment, quick, food, ammo and misc slots *inside* that same inventory. A deposit routine that walks the inventory can't tell them apart from ordinary backpack space.

So you walk up to your storage, press the key, and your quiver empties into the ammo chest. Your cooked food goes into the food chest. Your spare trophies and coins vanish into the misc chest. You find out in the middle of the next fight.

## The fix

This mod holds those slots back. Deposit exactly like before — your regular inventory goes, your slots stay put.

| Stays with you | Still deposits |
| --- | --- |
| Equipment slots | Your regular inventory |
| Quick slots | Extra Slots' extra **rows** |
| Food slots | Everything else, as before |
| Ammo slots | |
| Misc slots (trophies, fish, coins, keys, quest items) | |
| Custom slots from other mods (backpacks, quivers, rings…) | |

Nothing else changes. The deposit still reaches every eligible chest in range, and the host mod's on-screen count stays accurate — held-back items simply aren't counted.

Note that Extra Slots' extra inventory **rows** are ordinary inventory and still deposit normally. Only true slots are protected.

## What it guards

**Nearby Crafting** — the Mass Quick Deposit hotkey (`F6` by default).

**StoreAndCraft** — the dump hotkey (`.` by default) and single-item store (middle click).

StoreAndCraft's auto-store pulls items off the *ground*, never out of your inventory, so it was never a risk and is left alone.

## Why this needs a separate mod

Extra Slots already protects its slots from Valheim's built-in "stack all" button. But these mods don't use the vanilla stacking code — each has its own deposit routine, which Extra Slots has never heard of.

Nobody is doing anything wrong. They just don't know about each other, so the fix belongs in a third mod rather than a fork of any of them.

## Requirements

- [BepInEx](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [Extra Slots](https://thunderstore.io/c/valheim/p/shudnal/ExtraSlots/) by shudnal — required
- At least one of:
  - [Nearby Crafting](https://thunderstore.io/c/valheim/p/IPA38/NearbyCrafting/) by IPA38
  - [StoreAndCraft](https://thunderstore.io/c/valheim/p/Morda/StoreAndCraft/) by Morda

Extra Slots is a hard dependency. The two deposit mods are optional — this mod detects whichever you have installed and guards those. With neither present it logs a warning and stands down; nothing breaks, it just has no job to do.

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

Turn an individual category off if you actually *want* the hotkey to drain it. For example, `AmmoSlots = false` lets a deposit dump spare arrows into your ammo chest while still protecting everything else.

`EquipmentSlots` also covers extra utility slots and any custom slots added by other mods through the Extra Slots API. StoreAndCraft already skips equipped items on its own, so on that path this setting mostly covers slotted-but-unequipped gear.

Settings can be changed in a config manager in-game, or by editing the file.

## How it works

For the curious — you don't need to read this to use the mod.

While a deposit is running, the mod takes a snapshot of what's currently sitting in your Extra Slots slots (via Extra Slots' public API) and holds those particular items back. The moment the deposit finishes, the mod goes completely dormant again.

The two host mods are shaped differently, so they're guarded differently:

- **Nearby Crafting** offers no per-item filter, so its deposit call is wrapped in a scope and the individual moves are cancelled underneath it.
- **StoreAndCraft** has its own per-item filter, so the mod simply answers that filter with "no" — a protected item is skipped exactly the way a favourited one is. Its single-item store is a separate entry point and gets its own check.

No items are ever moved, hidden, or temporarily pulled out of your inventory to achieve this, so there's no window in which a crash or another misbehaving mod could lose them.

If a future update to a host mod changes what this patch hooks into, it logs an error and disables that half rather than half-working. The other mods keep running normally; you'd just lose the protection until this mod catches up. Please report it if that happens.

## Troubleshooting

Set `DebugLogging = true` and check `BepInEx/LogOutput.log`. On a healthy startup you'll see which hosts were found:

```text
[Info : ExtraSlots Deposit Guard] ExtraSlots Deposit Guard 1.1.0 loaded; protecting Extra Slots slots from: Nearby Crafting, StoreAndCraft.
```

With debug logging on, each held-back item is reported as it happens, tagged with the action that tried to take it (`quick-deposit`, `dump` or `store-one`).

## Credits

This mod contains no code from any mod it patches. It reaches Extra Slots through that mod's public API.

- Extra Slots by **shudnal**
- Nearby Crafting by **IPA38**
- StoreAndCraft by **Morda**

Thanks to all three for making mods worth making compatible.

## Source

[github.com/PicSoul/ExtraSlotsDepositGuard](https://github.com/PicSoul/ExtraSlotsDepositGuard)

Bug reports and pull requests welcome. Build instructions are in `BUILDING.md`.

## License

MIT — see `LICENSE`. Fork it, fix it, keep it alive if I go quiet.
