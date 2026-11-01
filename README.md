# Животные носят шляпы / Pets Wear Hats

A standalone cosmetic Stardeus mod by G33K3R-od. Special for Elisabeth.

## 0.2.0 — Cats, pigs, original Hat and colors

Select a player-owned **dog, cat or pig**, open its management overview and find **Animals Wear Hats → Headwear**.

- **Top hat:** the mod's included cylinder.
- **Hat (original game item):** uses the original Hat item definition and graphics loaded from the game. The selector shows an availability message if that item or its graphics cannot be resolved.
- **Remove hat:** removes the accessory.
- **Color:** choose a palette shade or enter your own six-digit RGB hex code, for example `#74B7DD`. Original colors restores white tint. The color applies to either hat and is retained when switching styles.

These are cosmetic appearance choices: no physical Hat is consumed, no crafting recipe is added, and no stats change. A direct-control chip and g33kUI are not required. English and Russian are included.

The native Hat's palette is used when available, with fallback color swatches otherwise. If the native Hat lacks an alternate walking or lying-down sprite, its regular sprite for the same direction is used; the selected accessory is not erased by the game's missing-sprite generator.

## Install

Extract the complete `AnimalsWearHats` folder from the runtime ZIP into the game's user Mods directory, replacing the previous version. Enable the mod and restart Stardeus.

## Fitting and verification

The player confirmed the dog's cylinder fitting in 0.1.1. Those scale and directional offsets are unchanged in 0.2.0. **Cat, pig and original Hat fitting are initial estimates and need in-game screenshots.** Hat rendering in portraits remains deferred.

Each species has its own `DogFit.json`, `CatFit.json` or `PigFit.json`. Scale is relative to its body; Down/Left/Up/Right are world-coordinate offsets; LyingDown adds an extra offset when lying. GameHatScale and GameHatOffset affect only the original Hat, so its position can be adjusted without changing the cylinder. Restart after editing.

## Saves and compatibility

The existing native `Hat` appearance layer stores the selected sprite ID and color through the game's serialized `BeingLooks.Parts`. A blank layer is added before the native saved-part restore, including for animals already in existing saves. No new serialized types, look-part IDs, inventory slots or per-animal preference files are introduced.

The mod does not edit shared species definitions or human equipment. It refuses to replace another accessory's sprite or edit an animal whose inventory already has headwear slots. All prototype Harmony patches are disabled together if a required method cannot be patched.

The new native Hat, color persistence and the expanded species support have not been verified in a Unity gameplay session here. Test actual save/reload with two animals using different styles and colors, switching styles and removing hats, all four directions and sleep. On a copy of the save, test disabling/re-enabling the mod. Include Player.log if a selector or sprite is missing.

## Build

Requires Python 3, .NET SDK and your installed game's managed assemblies (not distributed).

```sh
python build.py --managed-dir "/path/to/Stardeus_Data/Managed" --dotnet dotnet
dotnet run --project Tests -- "/path/to/Stardeus_Data/Managed/Game.dll"
python Tools/validate_package.py
python Tools/package.py
```

The output DLL is `Libraries/AnimalsWearHats.dll`. Regenerate original vector artwork with `python Tools/render_art.py` (requires PyMuPDF). Original game Hat graphics are resolved at runtime and are not copied into this repository or archive.
