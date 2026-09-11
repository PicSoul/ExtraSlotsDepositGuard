# Building from source

## What you need

- [.NET SDK](https://dotnet.microsoft.com/download) 6.0 or newer (the project targets `netstandard2.1`, any modern SDK can build it)
- A Valheim install
- BepInEx installed for Valheim

No Unity and no game assemblies are included in this repository — Valheim's DLLs are not redistributable, so the build references them from your own install.

## Setup

```sh
git clone https://github.com/PicSoul/ExtraSlotsDepositGuard.git
cd ExtraSlotsDepositGuard
cp Local.props.example Local.props
```

Edit `Local.props` and point `ValheimInstall` at the folder containing `valheim.exe`.

If you use **r2modman** or **Thunderstore Mod Manager**, BepInEx lives inside the mod profile rather than the game folder, so also set `BepInExCore`:

```xml
<BepInExCore>$(AppData)\r2modmanPlus-local\Valheim\profiles\YourProfileName\BepInEx\core</BepInExCore>
```

`Local.props` is gitignored, so your paths never end up in a commit. The `VALHEIM_INSTALL` and `BEPINEX_CORE` environment variables work as an alternative if you prefer.

If a path is wrong the build stops with a message telling you which one, rather than a few hundred "type not found" errors.

## Build

```sh
dotnet build -c Release
```

The output lands at `bin/Release/ExtraSlotsDepositGuard.dll`. Copy it into `BepInEx/plugins/` to test.

## Packaging

`build.ps1` builds, validates and produces an upload-ready Thunderstore zip:

```powershell
.\build.ps1            # build + validate + zip
.\build.ps1 -Install   # also copy into a local r2modman profile
```

It checks the package against Thunderstore's rules before zipping — icon exactly 256x256, package name charset, semver, description length, dependency string format, required files at the archive root — and refuses to package if `manifest.json` and the `.csproj` disagree on the version number.

When bumping a version, change it in **both** `manifest.json` and `ExtraSlotsDepositGuard.csproj`. Thunderstore package versions are immutable: once uploaded, a version can never be edited or replaced, only superseded.

## How the mod is put together

Everything lives in [`Plugin.cs`](Plugin.cs). It is deliberately small.

- **`DepositGuardPlugin`** — config binding and Harmony setup. Both hooks are applied manually rather than by attribute, because one target is a private method in another mod.
- **`ExtraSlotsApi`** — late-bound access to Extra Slots. Resolved by name through `AccessTools.TypeByName`, so there is no compile-time reference to `ExtraSlots.dll` and no need to ship `ExtraSlotsAPI.dll`.
- **`MassQuickDepositScope`** — opens a protection window around one quick-deposit and snapshots the items currently in Extra Slots slots. Reference equality, not value equality: two identical stacks are different objects and only the one in a slot is protected.
- **`MoveItemGuard`** — prefix on `Inventory.MoveItemToThis` that cancels moves of snapshotted items while the window is open.

### Why it hooks where it does

Nearby Crafting's deposit loop is its own code, not Valheim's `Inventory.StackAll`. Extra Slots already guards `StackAll`, which is why its protection does not cover this case.

The obvious alternative — temporarily removing protected items from the inventory for the duration of the deposit, which is what Extra Slots does for `StackAll` — was avoided on purpose. It creates a window where an exception in any other mod's patch could leave items missing. Cancelling individual moves needs no such window and touches no inventory state.

Cancelling is safe because Nearby Crafting measures progress by comparing the source stack size before and after each move, and breaks out of its loop when a move yields zero. A refused move is therefore indistinguishable from a full chest, and its deposit counter stays accurate.

### If a dependency changes

Both hook targets are resolved at runtime and checked. If either goes missing — Nearby Crafting renaming `MassQuickDeposit`, Extra Slots changing its API — the plugin logs an error and applies no patches at all, leaving both mods running normally. It never half-applies.

## License

MIT. Fork it, ship it, take it over if this repo goes quiet.
