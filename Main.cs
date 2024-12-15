#pragma warning disable CA1812 // Avoid uninstantiated internal classes

using BepInEx;
using System.Collections.Generic;
using HarmonyLib;
using System.Reflection.Emit;

namespace ElinGuaranteedRecipes;

[BepInPlugin("0B5DECC2-16E9-4CBB-ACC5-EC3F3143A697", "elinguaranteedrecipes", "1.1")]
internal sealed class Mod : BaseUnityPlugin
{
  private static Harmony? s_harmony;
  private static Mod? s_instance;

  internal static void Error(object data)
  {
    s_instance!.Logger.LogError(data);
  }

#pragma warning disable IDE0051 // Remove unused private members
  private void Start()
  {
    s_instance = this;
    s_harmony = new Harmony("ryan.elinguaranteedrecipes");
    s_harmony.PatchAll();
  }
#pragma warning restore IDE0051
}

[HarmonyPatch(typeof(RecipeManager))]
[HarmonyPatch(nameof(RecipeManager.ComeUpWithRecipe))]
internal sealed class TraitLightSource_LightRadius
{
  public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var needle = new CodeMatch[] {
      new(OpCodes.Ldc_I4_S, (sbyte)10),
      new(OpCodes.Call, AccessTools.Method(
        type: typeof(EClass),
        name: nameof(EClass.rnd)
      )),
      new(OpCodes.Brfalse),

      new(OpCodes.Call, AccessTools.PropertyGetter(
        type: typeof(EClass),
        name: nameof(EClass.debug)
      )),
      new(OpCodes.Ldfld, AccessTools.Field(
        type: typeof(CoreDebug),
        name: nameof(CoreDebug.enable)
      )),
      new(OpCodes.Brtrue),

      new(OpCodes.Ret),
    };

    var matcher = new CodeMatcher(instructions);
    _ = matcher.MatchStartForward(needle);
    if (matcher.IsInvalid) {
      Mod.Error("Failed to match ZoneEventHarvest patch");
      return instructions;
    }

    var labels = matcher.Labels;
    var result = matcher
      .RemoveInstructions(needle.Length)
      .Insert([
        new(OpCodes.Ldarg_2) { labels = labels },
        new(OpCodes.Ldc_I4, 10),
        new(OpCodes.Mul),
        new(OpCodes.Starg_S, (byte)2),
      ])
      .Instructions();
    return result;
  }
}
