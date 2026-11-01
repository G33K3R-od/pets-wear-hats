using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Game;
using Game.Components;
using Game.Constants;
using Game.Data;
using Game.Rendering;
using Game.Systems.Creatures;
using Game.Systems.UIUX;
using Game.Systems.Items;
using Game.UI;
using Game.Utils;
using HarmonyLib;
using KL.Utils;
using UnityEngine;

[assembly: AssemblyVersion("0.2.1.0")]

namespace AnimalsWearHats {
    public static class Boot {
        internal const string HarmonyId = "G33K3Rod.AnimalsWearHats";
        [RuntimeInitializeOnLoadMethod]
        private static void Init() {
            if (Harmony.HasAnyPatches(HarmonyId)) return;
            Settings.Load();
            var harmony = new Harmony(HarmonyId);
            try {
                harmony.CreateClassProcessor(typeof(InitializeLooksPatch)).Patch();
                harmony.CreateClassProcessor(typeof(RenderFitPatch)).Patch();
                harmony.CreateClassProcessor(typeof(ManagementPatch)).Patch();
                harmony.CreateClassProcessor(typeof(GameHatSpritePatch)).Patch();
                D.Warn("[Animals Wear Hats] 0.2.1 prototype loaded");
            } catch (Exception error) {
                harmony.UnpatchAll(HarmonyId);
                D.Err("[Animals Wear Hats] Unsupported game API; prototype disabled: {0}", error);
            }
        }
    }

    [Serializable]
    public sealed class AnimalFit {
        public float Scale = 0.30f;
        public float GameHatScale = 1f;
        public Vector2 GameHatOffset = Vector2.zero;
        public Vector2 Down = new Vector2(0f, -0.10f);
        public Vector2 Left = new Vector2(-0.40f, 0.19f);
        public Vector2 Up = new Vector2(0f, 0.43f);
        public Vector2 Right = new Vector2(0.40f, 0.19f);
        public Vector2 LyingDown = new Vector2(0f, -0.08f);
        public Vector2 For(Facing.Type facing, bool vertical) {
            var offset = facing == Facing.Type.Left ? Left
                : facing == Facing.Type.Up ? Up : facing == Facing.Type.Right ? Right : Down;
            return offset + (vertical ? Vector2.zero : LyingDown);
        }
        internal void Validate() {
            Scale = FitMath.SafeScale(Scale);
            GameHatScale = FitMath.SafeScale(GameHatScale);
            GameHatOffset = Safe(GameHatOffset);
            Down = Safe(Down); Left = Safe(Left); Up = Safe(Up); Right = Safe(Right);
            LyingDown = Safe(LyingDown);
        }
        private static Vector2 Safe(Vector2 value) => new Vector2(
            FitMath.SafeOffset(value.x), FitMath.SafeOffset(value.y));
    }

    internal static class Settings {
        internal static AnimalFit Dog = new AnimalFit();
        internal static AnimalFit Cat = new AnimalFit { Scale = .25f,
            Down = new Vector2(0, -.08f), Up = new Vector2(0, .34f),
            Left = new Vector2(-.21f, .08f), Right = new Vector2(.21f, .08f) };
        internal static AnimalFit Pig = new AnimalFit { Scale = .34f,
            Down = new Vector2(0, -.12f), Up = new Vector2(0, .36f),
            Left = new Vector2(-.32f, .17f), Right = new Vector2(.32f, .17f) };
        internal static AnimalFit For(BeingData data) => data?.Species?.Id == SpeciesId.Cat ? Cat
            : data?.Species?.Id == SpeciesId.Pig ? Pig : Dog;
        internal static void Load() {
            Read("DogFit.json", Dog); Read("CatFit.json", Cat); Read("PigFit.json", Pig);
        }
        private static void Read(string file, AnimalFit fit) {
            try {
                var dir = Path.GetDirectoryName(typeof(Boot).Assembly.Location);
                var path = Path.Combine(dir, "..", file);
                if (File.Exists(path)) JsonUtility.FromJsonOverwrite(File.ReadAllText(path), fit);
            } catch (Exception error) {
                D.Warn("[Animals Wear Hats] Cannot read {0}: {1}", file, error.Message);
            }
            fit.Validate();
        }
    }

    internal static class Hats {
        internal const string TopHat = "Beings/Hats/AnimalsWearHats/Dog/TopHat";

        internal static bool IsSupported(BeingData data) => SpeciesRules.Supports(data?.Species?.Id);
        internal static InventoryItemDef GameHat => InventoryItemDef.All.Values.FirstOrDefault(item =>
            (NativeHatIds.Matches(item.Id) || NativeHatIds.Matches(item.DefId))
            && (item.SlotId == InventorySlotId.Hat || item.SlotType?.LookPartType == LookPartTypeDef.Hat));
        internal static string GameHatSprite {
            get {
                // Prefer the installed item's definition, then the verified Core sprite ID.
                // Item-table linking or a renamed inventory ID must not hide existing art.
                foreach (var source in new[] { GameHat?.SpriteId, NativeHatIds.Sprite }) {
                    if (string.IsNullOrEmpty(source)) continue;
                    foreach (var variant in new[] { "N", "M", "F" }) {
                        var candidate = source.Replace("BODY_TYPE", variant);
                        if (RenderingService.Sprites.TryFind(candidate + "_D", out _)) return candidate;
                    }
                }
                return null;
            }
        }
        internal static bool IsGameHat(LookPart part) => part?.SpriteId != null
            && part.SpriteId != TopHat && part.SpriteId == GameHatSprite;
        internal static bool Owns(LookPart part) => part != null && (part.SpriteId == TopHat || IsGameHat(part));
        internal static bool CanEdit(BeingData data) => IsSupported(data) && data.IsPlayer
            && data.Being != null && !data.HasStateFlag(BeingState.Dead)
            && data.Looks?.Get(LookPartTypeDef.Hat) != null
            && (data.Looks.Get(LookPartTypeDef.Hat).SpriteId == null
                || Owns(data.Looks.Get(LookPartTypeDef.Hat)))
            && (data.Inventory == null || !data.Inventory.Equipment.Values.Any(
                slot => slot.Type.LookPartType == LookPartTypeDef.Hat));

        // Called after species initialization, before native saved Looks.LoadPart replay.
        // Use the existing native Hat type: no new serialized types or global species edits.
        internal static void EnsurePart(BeingData data) {
            if (!IsSupported(data) || data.Looks?.Parts == null) return;
            var looks = data.Looks;
            if (looks.Get(LookPartTypeDef.Hat) != null) return;
            var body = looks.Get(LookPartTypeDef.Body);
            if (body == null) return;
            var part = new LookPart {
                Index = looks.Parts.Length,
                Scale = (body.Scale > 0 ? body.Scale : 1f) * Settings.For(data).Scale,
                Offset = body.Offset,
                PosFilter = body.PosFilter,
                RenderSorted = true,
                Color = Color.white,
            };
            part.SetType(LookPartTypeDef.Hat);
            var parts = looks.Parts;
            Array.Resize(ref parts, parts.Length + 1);
            parts[parts.Length - 1] = part;
            looks.Parts = parts;
            looks.NumParts = parts.Length;
        }

        internal static void Select(BeingData data, string sprite) {
            if (!CanEdit(data)) return;
            var part = data.Looks.Get(LookPartTypeDef.Hat);
            if (sprite != null && sprite != TopHat && sprite != GameHatSprite) return;
            if (part.SpriteId == sprite) return;
            if (sprite == TopHat && !HasAllSprites()) {
                D.Err("[Animals Wear Hats] Hat graphics missing. Reinstall the complete mod folder.");
                UISounds.PlayActionDenied();
                return;
            }
            part.SpriteId = sprite;
            // Keep the animal's selected color when changing hat styles.
            Rebuild(data);
        }
        internal static void SetColor(BeingData data, Color color) {
            if (!CanEdit(data) || !Owns(data.Looks.Get(LookPartTypeDef.Hat))) return;
            color.a = 1f;
            data.Looks.Get(LookPartTypeDef.Hat).Color = color;
            Rebuild(data);
        }
        private static void Rebuild(BeingData data) {
            FitPart(data);
            data.Looks.IsDirty = true;
            data.Being.Looks.Rebuild(force: true, clearCache: true);
            UISounds.PlayDigitalConfirm();
        }
        internal static void FitPart(BeingData data) {
            var looks = data?.Looks;
            var hat = looks?.Get(LookPartTypeDef.Hat);
            var body = looks?.Get(LookPartTypeDef.Body);
            if (!Owns(hat) || body == null) return;
            var fit = Settings.For(data);
            hat.Scale = (body.Scale > 0 ? body.Scale : 1f) * fit.Scale
                * (IsGameHat(hat) ? fit.GameHatScale : 1f);
            hat.Offset = body.Offset;
            hat.PosFilter = body.PosFilter;
            hat.PosFilterH = 0;
        }

        private static bool HasAllSprites() {
            foreach (var pose in new[] {"", "_H", "_1"})
                foreach (var direction in new[] {"D", "L", "U", "R"})
                    if (!RenderingService.Sprites.TryFind(TopHat + pose + "_" + direction, out _))
                        return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(SpeciesLooksDef), "InitializeFor")]
    internal static class InitializeLooksPatch {
        private static void Postfix(BeingData d) => Hats.EnsurePart(d);
    }

    [HarmonyPatch(typeof(BeingLooksComp), "BuildSpriteSetFor")]
    internal static class RenderFitPatch {
        private static void Prefix(BeingLooksComp __instance) {
            if (Hats.IsSupported(__instance.Being?.Data)) Hats.FitPart(__instance.Being.Data);
        }
        private static void Postfix(BeingLooksComp __instance, Facing.Type facing, bool vertical,
                AnimatedSpriteSet[] ___SpriteSets, AnimatedSpriteSet[] ___SpriteSetsH) {
            if (!Hats.IsSupported(__instance.Being?.Data)) return;
            var hat = __instance.Looks?.Get(LookPartTypeDef.Hat);
            if (!Hats.Owns(hat)) return;
            var sets = vertical ? ___SpriteSets : ___SpriteSetsH;
            var index = (int)facing;
            if (sets == null || index >= sets.Length) return;
            var set = sets[index];
            if (set?.Offsets == null || hat.Index >= set.Offsets.Length) return;
            // Assign an absolute offset, including on cache hits. Never accumulate movement.
            set.Offsets[hat.Index] = hat.Offset + Settings.For(__instance.Being.Data).For(facing, vertical)
                + (Hats.IsGameHat(hat) ? Settings.For(__instance.Being.Data).GameHatOffset : Vector2.zero);
        }
    }

    // Native Hat may not ship the animal's alternate frame or lying sprite. Resolve
    // those from the original game art without invoking GenerateMissing (which clears it).
    [HarmonyPatch(typeof(BeingLooksComp), "GetFacingSprite")]
    internal static class GameHatSpritePatch {
        private static bool Prefix(BeingLooksComp __instance, LookPart part, Facing.Type facing,
                bool vertical, bool ___isAltFrame, ref bool ignoreColor, ref SpriteData __result) {
            if (!Hats.IsSupported(__instance.Being?.Data) || !Hats.IsGameHat(part)) return true;
            var source = part.SpriteId;
            __result = null;
            foreach (var pose in HatFrames.Poses(vertical, ___isAltFrame)) {
                if (RenderingService.Sprites.TryFind(Facing.FaceSpriteTo(source + pose, facing), out var sprite)) {
                    __result = sprite.Copy();
                    __result.Scale = part.Scale;
                    break;
                }
            }
            // Do not render a differently facing hat or erase the saved choice.
            ignoreColor = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(EntityOverview_BeingManagement), "LoadContent")]
    internal static class ManagementPatch {
        private static void Postfix(EntityOverviewContext ctx, WGroup group, ref int gIdx) {
            var data = ctx.Being;
            if (!Hats.CanEdit(data)) return;
            var block = UISpawn.Block(ctx.Lifetime, group, gIdx++);
            var header = UISpawn.Widget<WText>(ctx.Lifetime,
                UIPrefabId.WContent2Header, block.Container, 0);
            header.Load("awh.title".T());
            var button = UISpawn.Widget<WButtonText>(ctx.Lifetime,
                UIPrefabId.WContent2ButtonSimple, block.Container, 1);
            var colors = UISpawn.Widget<WButtonText>(ctx.Lifetime,
                UIPrefabId.WContent2ButtonSimple, block.Container, 2);
            Action refresh = () => {
                var part = data.Looks.Get(LookPartTypeDef.Hat);
                button.SetText("awh.hat".T() + ": " + (part.SpriteId == Hats.TopHat
                    ? "awh.tophat".T() : Hats.IsGameHat(part) ? "awh.gamehat".T() : "awh.none".T()));
                colors.SetText("awh.color".T() + ": #" + ColorUtility.ToHtmlStringRGB(part.Color));
                colors.SetInteractable(Hats.Owns(part));
            };
            button.Load("awh.hat".T(), () => {
                if (!Hats.CanEdit(data)) return;
                var choice = QuickChoice.Create("awh.hat".T());
                choice.Widget = button.Rect;
                choice.AddButton("awh.tophat".T(), () => { Hats.Select(data, Hats.TopHat); refresh(); });
                var sprite = Hats.GameHatSprite;
                if (sprite != null)
                    choice.AddButton("awh.gamehat".T(), () => { Hats.Select(data, sprite); refresh(); });
                else choice.AddLabel("awh.unavailable".T());
                choice.AddButton("awh.remove".T(), () => { Hats.Select(data, null); refresh(); });
                A.S.UIQuery.ShowQuickChoice.Ask(choice);
            });
            colors.Load("awh.color".T(), () => ShowColors(data, colors, refresh));
            refresh();
            UIUtils.AddTip(ctx.Lifetime, button, "awh.tip".T());
        }
        private static void ShowColors(BeingData data, WButtonText anchor, Action refresh) {
            if (!Hats.CanEdit(data) || !Hats.Owns(data.Looks.Get(LookPartTypeDef.Hat))) return;
            var choice = QuickChoice.Create("awh.color".T());
            choice.Widget = anchor.Rect;
            void Add(string name, Color tint) {
                choice.AddIconButton(name, new QuickChoice.IconButton {
                    Icon = Images.Sprite(IconId.CColorPreview), Tint = tint, AutoClose = true,
                    OnClick = () => { Hats.SetColor(data, tint); refresh(); }
                });
            }
            Add("awh.original".T(), Color.white);
            var groupId = Hats.GameHat?.ColorGroupId;
            var palette = groupId == null ? null : Palette.ForGroup(groupId);
            if (palette != null && palette.Count > 0) {
                foreach (var color in palette) Add(color.NameT, color.Color);
            } else {
                foreach (var hex in new[] { "D65E5E", "E8AF56", "E4D56C", "77B77B", "69B9C2", "708FD2", "A17CD4", "D083AF", "8C624B", "666675" }) {
                    ColorUtility.TryParseHtmlString("#" + hex, out var color);
                    Add("#" + hex, color);
                }
            }
            choice.AddInput("awh.custom".T(), new QuickChoice.Input {
                Placeholder = "#RRGGBB", Value = "#" + ColorUtility.ToHtmlStringRGB(data.Looks.Get(LookPartTypeDef.Hat).Color),
                OnSubmit = value => {
                    var hex = ColorHex.Normalize(value);
                    if (hex == null || !ColorUtility.TryParseHtmlString(hex, out var color)) {
                        UISounds.PlayActionDenied(); return;
                    }
                    Hats.SetColor(data, color); refresh();
                }
            });
            A.S.UIQuery.ShowQuickChoice.Ask(choice);
        }
    }
}
