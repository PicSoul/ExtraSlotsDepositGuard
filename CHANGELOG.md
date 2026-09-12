# Changelog

## 1.1.0

- Added support for **StoreAndCraft**. Its dump hotkey (`.`) and single-item store (middle click) no longer take items out of Extra Slots slots.
- Nearby Crafting and StoreAndCraft are now both **optional**. The mod detects whichever you have installed and guards those; only Extra Slots is required. Previously Nearby Crafting was a hard dependency and the mod would not load without it.
- The startup log now names the deposit sources it is guarding.
- Host detection now asks the chainloader before probing for types, so a host you do not have
  installed no longer leaves a HarmonyX "could not find type" warning in the log.
- Debug logging now tags each held-back item with the action that tried to take it (`quick-deposit`, `dump` or `store-one`).

## 1.0.1

- Added a link to the source repository: https://github.com/PicSoul/ExtraSlotsDepositGuard
- No functional changes. If 1.0.0 is working for you, there is no need to update.

## 1.0.0

- Initial release.
- Nearby Crafting's Mass Quick Deposit hotkey no longer deposits items held in Extra Slots equipment, quick, food, ammo and misc slots.
- Per-category toggles so individual slot groups can opt back in.
