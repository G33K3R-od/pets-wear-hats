# Животные носят шляпы / Pets Wear Hats

A standalone cosmetic mod for Stardeus, by G33K3R-od.

Special for Elisabeth.

## Prototype 0.1.0

Select a player-owned **dog**, open its management overview, and find **Animals Wear Hats → Headwear**. Choose **Top hat** or **Remove hat**. The accessory changes immediately; no direct-control chip is required. There is no g33kUI dependency.

The prototype includes one original vector-drawn top hat, four directional sprites, an alternate walking frame and lying-down variants. It does not add crafting recipes, physical inventory items, armor or stat bonuses. Cats and other species are not supported yet.

**Status:** compiled against the supplied Stardeus 0.15 managed assemblies. Automated API and persistence-path checks are available. Unity gameplay, visual fitting, click behavior and actual save/reload have **not** been verified here. Treat this as a first test build, not a Workshop-ready release.

## Install the test build

Extract the `AnimalsWearHats` folder from the runtime ZIP into the game's user Mods directory. Enable **Животные носят шляпы** and restart Stardeus. Select a dog belonging to your colony.

## Persistence and compatibility

The mod uses the game's existing `Hat` appearance layer and native serialized `BeingLooks.Parts`. Its initializer adds a blank layer to dogs before native saved looks are restored. Selection is stored as a sprite ID on that layer; the mod writes no external per-animal preference files and creates no new serialized classes or look-part type IDs.

It refuses to overwrite another accessory's sprite or edit dogs whose inventory already has headwear equipment slots. It does not edit shared species definitions or patch human equipment. A compatibility check during startup disables all prototype patches together if a required game method cannot be patched.

Hat persistence is implemented through the native save path, but a real save/reload and mod-disable test are still required. Use a separate test save for the first run. Native portraits intentionally omit `Hat` parts; portrait hats are **not included** in 0.1.0.

## Fitting

`DogFit.json` contains relative scale, four upright offsets and an additional lying offset. These are initial estimates because the supplied references do not contain the dog's original artwork. Values are in the renderer's world coordinates; they need an in-game screenshot for calibration. Restart after editing. Offsets are applied during native sprite-set construction and follow the dog's position filter and ambient movement.

## Build

Requires Python 3 and .NET SDK. The game's proprietary assemblies are not included.

```sh
python build.py --managed-dir "/path/to/Stardeus_Data/Managed" --dotnet dotnet
dotnet run --project Tests -- "/path/to/Stardeus_Data/Managed/Game.dll"
python Tools/validate_package.py
```

The output is `Libraries/AnimalsWearHats.dll`. Checked-in PNGs are ready to use. Regenerate them from the original SVGs with `python Tools/render_art.py` (requires PyMuPDF).

## First in-game check

1. Open an existing colony with a dog and locate the headwear selector.
2. Equip and remove the cylinder repeatedly; check no duplicate buttons appear when reselecting the dog.
3. Observe all four directions, walking, sleeping and changing animation speed.
4. Save with a hat, restart and reload; repeat after removing it. Check two dogs retain separate selections.
5. Check an unowned dog cannot be edited and humans, robots and other species remain unchanged.
6. Check a copy of the save after disabling and re-enabling the mod; inspect `Player.log` for missing-sprite errors.

## Next milestones

- Calibrate dog fitting in the actual game.
- Hats in animal portraits.
- Cat fitting and additional hats.
- Optional physical items, crafting and colonist dressing jobs.

Project title: **Животные носят шляпы**. Repository slug: `pets-wear-hats`.
