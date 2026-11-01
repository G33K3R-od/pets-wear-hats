using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using AnimalsWearHats;

internal static class Tests {
    private static int checks;
    private static void Check(bool success, string name) {
        checks++;
        if (!success) throw new Exception(name);
    }
    private static void Main(string[] args) {
        Check(args.Length == 1, "Pass the supplied Game.dll path");
        Check(FitMath.SafeScale(float.NaN) == .30f, "NaN scale fallback");
        Check(FitMath.SafeScale(float.PositiveInfinity) == .30f, "Infinite scale fallback");
        Check(FitMath.SafeScale(-10f) == .05f, "Negative scale bounded");
        Check(FitMath.SafeScale(10f) == 2f, "Huge scale bounded");
        Check(FitMath.SafeScale(.6f) == .6f, "Valid fit preserved");
        Check(FitMath.SafeOffset(float.NaN) == 0f, "NaN offset fallback");
        Check(FitMath.SafeOffset(float.NegativeInfinity) == 0f, "Infinite offset fallback");
        Check(FitMath.SafeOffset(-10f) == -2f && FitMath.SafeOffset(10f) == 2f,
            "Offsets bounded in both directions");
        Check(FitMath.SafeOffset(-.2f) == -.2f, "Valid negative offset preserved");
        Check(SpeciesRules.Supports("Dog") && SpeciesRules.Supports("Cat") && SpeciesRules.Supports("Pig"),
            "Dogs, cats and pigs supported");
        Check(!SpeciesRules.Supports("Human") && !SpeciesRules.Supports("Robot") && !SpeciesRules.Supports(null),
            "Humans, robots and missing species excluded");
        Check(ColorHex.Normalize(" #aa33ff ") == "#AA33FF", "Normalize custom color");
        Check(ColorHex.Normalize("123456") == "#123456", "Hex without leading hash");
        Check(ColorHex.Normalize("000000") == "#000000" && ColorHex.Normalize("FFFFFF") == "#FFFFFF", "Black and white");
        foreach (var invalid in new[] { null, "", "#FFF", "#12345600", "#ZZZZZZ", "<color=red>", "#12 456" })
            Check(ColorHex.Normalize(invalid) == null, "Reject invalid or transparent custom color");
        Check(HatFrames.Poses(true, false).SequenceEqual(new[] { "" }), "Standing native hat frame");
        Check(HatFrames.Poses(true, true).SequenceEqual(new[] { "_1", "" }), "Walking frame falls back to base");
        Check(HatFrames.Poses(false, false).SequenceEqual(new[] { "_H", "" }), "Lying frame falls back to base");
        Check(HatFrames.Poses(false, true).SequenceEqual(new[] { "_H", "" }), "Lying ignores alternate walking state");

        using var input = File.OpenRead(args[0]);
        using var pe = new PEReader(input);
        var reader = pe.GetMetadataReader();
        TypeDefinition Type(string name) {
            foreach (var handle in reader.TypeDefinitions) {
                var def = reader.GetTypeDefinition(handle);
                if (reader.GetString(def.Name) == name) return def;
            }
            throw new Exception("Missing game type " + name);
        }
        MethodDefinition Method(string type, string name) {
            var methods = Type(type).GetMethods().Select(h => reader.GetMethodDefinition(h))
                .Where(m => reader.GetString(m.Name) == name).ToArray();
            Check(methods.Length == 1, "Unambiguous patch target " + type + "." + name);
            return methods.Single();
        }
        void Parameters(string type, string method, params string[] names) {
            var actual = Method(type, method).GetParameters().Select(h => reader.GetParameter(h))
                .Where(p => p.SequenceNumber > 0).Select(p => reader.GetString(p.Name));
            Check(actual.SequenceEqual(names), "Harmony argument names " + type + "." + method);
        }
        void Fields(string type, params string[] names) {
            var fields = Type(type).GetFields().Select(h => reader.GetString(reader.GetFieldDefinition(h).Name)).ToHashSet();
            foreach (var name in names) Check(fields.Contains(name), type + "." + name);
        }
        Parameters("SpeciesLooksDef", "InitializeFor", "d");
        Parameters("BeingLooksComp", "BuildSpriteSetFor", "facing", "vertical");
        Parameters("EntityOverview_BeingManagement", "LoadContent", "ctx", "group", "gIdx");
        var spriteArguments = Method("BeingLooksComp", "GetFacingSprite").GetParameters()
            .Select(h => reader.GetString(reader.GetParameter(h).Name)).ToArray();
        Check(new[] { "part", "facing", "vertical", "ignoreColor" }.All(spriteArguments.Contains),
            "Native hat resolver Harmony parameter bindings");
        Fields("BeingLooksComp", "SpriteSets", "SpriteSetsH", "Looks", "isAltFrame");
        Fields("InventoryItemDef", "All", "Id", "DefId", "SpriteId", "ColorGroupId", "SlotType");
        Fields("BeingLooks", "Parts", "NumParts");
        Fields("LookPart", "SpriteId", "Index", "Scale", "Offset", "PosFilter");
        Fields("BeingDataData", "Looks");
        var sprite = Type("LookPart").GetFields().Select(h => reader.GetFieldDefinition(h))
            .Single(f => reader.GetString(f.Name) == "SpriteId");
        Check(sprite.GetCustomAttributes().Count > 0, "Sprite ID retains native serialization annotation");
        var savedColor = Type("LookPart").GetFields().Select(h => reader.GetFieldDefinition(h))
            .Single(f => reader.GetString(f.Name) == "Color");
        Check(savedColor.GetCustomAttributes().Count > 0, "Selected color retains native serialization annotation");
        var looks = Type("BeingDataData").GetFields().Select(h => reader.GetFieldDefinition(h))
            .Single(f => reader.GetString(f.Name) == "Looks");
        Check(looks.GetCustomAttributes().Count > 0, "Being appearance retains native serialization annotation");

        // Check actual supplied IL, not just the newer reference source. Reload must add
        // species layers before replaying saved parts for the native Hat selection to survive.
        var loadCalls = Calls(pe, reader, Type("BeingDataData")).ToArray();
        Check(loadCalls.Contains("InitializeFor") && loadCalls.Contains("LoadPart"),
            "Save loader initializes looks and restores saved parts");
        Check(loadCalls.ToList().IndexOf("InitializeFor") < loadCalls.ToList().IndexOf("LoadPart"),
            "Initialization runs before saved part restoration");
        Check(Calls(pe, reader, Type("BeingLooks")).Contains("CopyLookFrom"),
            "Saved part restoration copies appearance");
        Check(Calls(pe, reader, Type("BeingAvatar")).Contains("get_Hat"),
            "Native avatar has Hat-specific behavior: portrait support remains deferred");
        Console.WriteLine("PASS: " + checks + " configuration, native API and persistence-path checks. Unity runtime not tested.");
    }

    private static IEnumerable<string> Calls(PEReader pe, MetadataReader reader, TypeDefinition type) {
        var opcodes = typeof(OpCodes).GetFields().Where(f => f.FieldType == typeof(OpCode))
            .Select(f => (OpCode)f.GetValue(null)).ToDictionary(o => unchecked((ushort)o.Value));
        foreach (var handle in type.GetMethods()) {
            var method = reader.GetMethodDefinition(handle);
            if (method.RelativeVirtualAddress == 0) continue;
            var il = pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes();
            for (int i = 0; i < il.Length;) {
                ushort code = il[i++];
                if (code == 0xfe) code = (ushort)(0xfe00 | il[i++]);
                var op = opcodes[code];
                if (op.OperandType == OperandType.InlineMethod) {
                    var token = MetadataTokens.EntityHandle(BitConverter.ToInt32(il, i));
                    if (token.Kind == HandleKind.MethodDefinition)
                        yield return reader.GetString(reader.GetMethodDefinition((MethodDefinitionHandle)token).Name);
                    else if (token.Kind == HandleKind.MemberReference)
                        yield return reader.GetString(reader.GetMemberReference((MemberReferenceHandle)token).Name);
                }
                i += op.OperandType switch {
                    OperandType.InlineNone => 0,
                    OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                    OperandType.InlineVar => 2,
                    OperandType.InlineI8 or OperandType.InlineR => 8,
                    OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(il, i),
                    _ => 4
                };
            }
        }
    }
}
