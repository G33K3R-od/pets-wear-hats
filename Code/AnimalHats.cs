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
using Game.UI;
using Game.Utils;
using HarmonyLib;
using KL.Utils;
using UnityEngine;

[assembly: AssemblyVersion("0.1.0.0")]

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
                D.Warn("[Animals Wear Hats] 0.1.0 prototype loaded");
            } catch (Exception error) {
                harmony.UnpatchAll(HarmonyId);
                D.Err("[Animals Wear Hats] Unsupported game API; prototype disabled: {0}", error);
            }
        }
    }

    [Serializable]
    public sealed class DogFit {
        public float Scale = 0.48f;
        public Vector2 Down = new Vector2(0f, 0.22f);
        public Vector2 Left = new Vector2(-0.20f, 0.12f);
        public Vector2 Up = new Vector2(0f, 0.10f);
        public Vector2 Right = new Vector2(0.20f, 0.12f);
        public Vector2 LyingDown = new Vector2(0f, -0.08f);
        public Vector2 For(Facing.Type facing, bool vertical) {
            var offset = facing == Facing.Type.Left ? Left
                : facing == Facing.Type.Up ? Up : facing == Facing.Type.Right ? Right : Down;
            return offset + (vertical ? Vector2.zero : LyingDown);
        }
        internal void Validate() {
            Scale = FitMath.SafeScale(Scale);
            Down = Safe(Down); Left = Safe(Left); Up = Safe(Up); Right = Safe(Right);
            LyingDown = Safe(LyingDown);
        }
        private static Vector2 Safe(Vector2 value) => new Vector2(
            FitMath.SafeOffset(value.x), FitMath.SafeOffset(value.y));
    }

    internal static class Settings {
        internal static DogFit Dog = new DogFit();
        internal static void Load() {
            try {
                var dir = Path.GetDirectoryName(typeof(Boot).Assembly.Location);
                var path = Path.Combine(dir, "..", "DogFit.json");
                if (File.Exists(path)) JsonUtility.FromJsonOverwrite(File.ReadAllText(path), Dog);
                Dog.Validate();
            } catch (Exception error) {
                Dog = new DogFit();
                D.Warn("[Animals Wear Hats] Using default dog fitting: {0}", error.Message);
            }
        }
    }

    internal static class Hats {
        internal const string TopHat = "Beings/Hats/AnimalsWearHats/Dog/TopHat";

        internal static bool IsDog(BeingData data) => data?.Species?.Id == SpeciesId.Dog;
        internal static bool Owns(LookPart part) => part != null && part.SpriteId == TopHat;
        internal static bool CanEdit(BeingData data) => IsDog(data) && data.IsPlayer
            && data.Being != null && !data.HasStateFlag(BeingState.Dead)
            && data.Looks?.Get(LookPartTypeDef.Hat) != null
            && (data.Looks.Get(LookPartTypeDef.Hat).SpriteId == null
                || Owns(data.Looks.Get(LookPartTypeDef.Hat)))
            && (data.Inventory == null || !data.Inventory.Equipment.Values.Any(
                slot => slot.Type.LookPartType == LookPartTypeDef.Hat));

        // Called after species initialization, before native saved Looks.LoadPart replay.
        // Use the existing native Hat type: no new serialized types or global species edits.
        internal static void EnsurePart(BeingData data) {
            if (!IsDog(data) || data.Looks?.Parts == null) return;
            var looks = data.Looks;
            if (looks.Get(LookPartTypeDef.Hat) != null) return;
            var body = looks.Get(LookPartTypeDef.Body);
            if (body == null) return;
            var part = new LookPart {
                Index = looks.Parts.Length,
                Scale = (body.Scale > 0 ? body.Scale : 1f) * Settings.Dog.Scale,
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

        internal static void Select(BeingData data, bool on) {
            if (!CanEdit(data)) return;
            var part = data.Looks.Get(LookPartTypeDef.Hat);
            if (part.SpriteId == (on ? TopHat : null)) return;
            // Refuse a broken install rather than persist a missing sprite in the save.
            if (on && !HasAllSprites()) {
                D.Err("[Animals Wear Hats] Hat graphics missing. Reinstall the complete mod folder.");
                UISounds.PlayActionDenied();
                return;
            }
            part.SpriteId = on ? TopHat : null;
            part.Color = Color.white;
            FitPart(data.Looks);
            data.Looks.IsDirty = true;
            data.Being.Looks.Rebuild(force: true, clearCache: true);
            UISounds.PlayDigitalConfirm();
        }

        internal static void FitPart(BeingLooks looks) {
            var hat = looks?.Get(LookPartTypeDef.Hat);
            var body = looks?.Get(LookPartTypeDef.Body);
            if (!Owns(hat) || body == null) return;
            hat.Scale = (body.Scale > 0 ? body.Scale : 1f) * Settings.Dog.Scale;
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
            if (Hats.IsDog(__instance.Being?.Data)) Hats.FitPart(__instance.Looks);
        }
        private static void Postfix(BeingLooksComp __instance, Facing.Type facing, bool vertical,
                AnimatedSpriteSet[] ___SpriteSets, AnimatedSpriteSet[] ___SpriteSetsH) {
            if (!Hats.IsDog(__instance.Being?.Data)) return;
            var hat = __instance.Looks?.Get(LookPartTypeDef.Hat);
            if (!Hats.Owns(hat)) return;
            var sets = vertical ? ___SpriteSets : ___SpriteSetsH;
            var index = (int)facing;
            if (sets == null || index >= sets.Length) return;
            var set = sets[index];
            if (set?.Offsets == null || hat.Index >= set.Offsets.Length) return;
            // Assign an absolute offset, including on cache hits. Never accumulate movement.
            set.Offsets[hat.Index] = hat.Offset + Settings.Dog.For(facing, vertical);
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
            Action refresh = () => button.SetText("awh.hat".T() + ": "
                + (Hats.Owns(data.Looks.Get(LookPartTypeDef.Hat)) ? "awh.tophat".T() : "awh.none".T()));
            button.Load("awh.hat".T(), () => {
                if (!Hats.CanEdit(data)) return;
                var choice = QuickChoice.Create("awh.hat".T());
                choice.Widget = button.Rect;
                choice.AddButton("awh.tophat".T(), () => { Hats.Select(data, true); refresh(); });
                choice.AddButton("awh.remove".T(), () => { Hats.Select(data, false); refresh(); });
                A.S.UIQuery.ShowQuickChoice.Ask(choice);
            });
            refresh();
            UIUtils.AddTip(ctx.Lifetime, button, "awh.tip".T());
        }
    }
}
