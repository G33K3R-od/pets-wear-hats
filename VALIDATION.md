# Prototype 0.1.0 validation

- Compiled successfully with .NET SDK 8.0.408 Roslyn, targeting the supplied Unity-managed assemblies.
- Reference Game.dll SHA-256: `5d21e0ba2726cfaa4264c3c03419bb36b514f4228f56282773063143b752f591`.
- 33 automated checks pass: finite fitting values; required native patch targets and their parameter names; private sprite-array fields used by Harmony injection; serialized appearance fields; and native IL showing species initialization before saved appearance replay.
- Package validator passes: twelve 256×256 RGBA sprite variants; six matching English/Russian keys; configuration, metadata, compiled DLL and cover.
- Original vector hat artwork was rendered and visually inspected. No original game graphics or game assemblies are distributed.

These checks do not run Unity or prove visual fitting, click behavior or save/reload in the installed game. Dog offsets are initial estimates. Portrait hats are not implemented. See README.md for the first in-game test checklist.
