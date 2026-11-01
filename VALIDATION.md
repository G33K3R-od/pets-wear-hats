# 0.2.1 validation

- Mod DLL compiled successfully against the supplied Stardeus managed assemblies with .NET SDK 8.0.408 Roslyn.
- Reference Game.dll SHA-256: `5d21e0ba2726cfaa4264c3c03419bb36b514f4228f56282773063143b752f591`.
- 64 automated checks pass: finite fitting values; species scope; custom hex normalization and invalid/transparent input rejection; native Hat animation fallback order; required Harmony targets, parameter bindings and fields; native serialized sprite/color fields; and the native initialization-before-saved-parts persistence path.
- Package validator passes: twelve 256×256 RGBA cylinder sprites; eleven matching English/Russian translation keys; three species fitting files; metadata, DLL and cover.
- Player feedback confirms cylinder fitting on the dog in 0.1.1. Its scale and four directional offsets are unchanged. New GameHatScale/GameHatOffset properties only apply to the original Hat.
- Original Hat item and art are looked up at runtime from the game, not copied or recreated. If no matching definition/art is found, the picker displays an availability message while the cylinder still works.

Pending in-game verification: cat and pig fitting; native Hat fitting and tint; palette/custom-color interactions; actual save/reload of different style/color combinations across multiple pets; all directions and sleep; disabling/re-enabling the mod. Portrait hats remain deferred. The original native Hat may use its normal-direction art while lying if it has no lying variant. Automated checks do not run Unity.

0.2.1 fixes native Hat lookup using verified constants from Game.Constants.dll: Obj/Equipment/Hats/Hat01 and Beings/Hats/Human/Hat01/Hat01. Cat side offsets are lowered and moved inward; new fitting is awaiting confirmation. Workshop uploads are enabled and the full English listing is embedded in ModInfo.json.
