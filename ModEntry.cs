using System.Reflection;
using System.Runtime.InteropServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

[ModInitializer("Initialize")]
public class ModEntry
{
    private const string TargetEpochId = "NECROBINDER1_EPOCH";
    private const string ReplacementImagePath = "res://images/custom_epoch.png";

    public static Texture2D? ReplacementTexture { get; private set; }

    [DllImport("libdl.so.2", EntryPoint = "dlopen")]
    private static extern IntPtr Dlopen(string filename, int flags);

    private const int RTLD_NOW = 2;
    private const int RTLD_GLOBAL = 256;

    private static void PreloadLibgcc()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return;
        if (Dlopen("libgcc_s.so.1", RTLD_NOW | RTLD_GLOBAL) == IntPtr.Zero)
            GD.PrintErr("[EpochReplacer] Warning: could not preload libgcc_s.so.1 — Harmony patches may fail");
    }

    public static void Initialize()
    {
        ReplacementTexture = LoadReplacementTexture();
        if (ReplacementTexture == null)
        {
            GD.PrintErr("[EpochReplacer] Failed to load replacement texture, mod disabled.");
            return;
        }

        PreloadLibgcc();
        var harmony = new Harmony("com.themagickoala.epochreplacer");
        harmony.PatchAll();
        GD.Print("[EpochReplacer] Harmony patches applied.");
    }

    private static Texture2D? LoadReplacementTexture()
    {
        if (ResourceLoader.Exists(ReplacementImagePath))
            return GD.Load<Texture2D>(ReplacementImagePath);

        var modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var imagePath = Path.Combine(modDir, "custom_epoch.png");

        if (File.Exists(imagePath))
        {
            var image = new Image();
            image.Load(imagePath);
            return ImageTexture.CreateFromImage(image);
        }

        GD.PrintErr("[EpochReplacer] Image not found at '" +
                     ReplacementImagePath + "' or '" + imagePath + "'");
        return null;
    }
}

// NEpochSlot.SetState() assigns portrait textures for all three epoch slot states.
// model is set in Create() before AddChild, so it's always available here.
[HarmonyPatch(typeof(NEpochSlot), nameof(NEpochSlot.SetState))]
static class NEpochSlotSetStatePatch
{
    static void Postfix(NEpochSlot __instance)
    {
        if (__instance.model?.Id != "NECROBINDER1_EPOCH") return;
        if (ModEntry.ReplacementTexture == null) return;

        if (__instance.State is EpochSlotState.Complete or EpochSlotState.Obtained)
        {
            var portrait = __instance.GetNodeOrNull<TextureRect>("%Portrait");
            if (portrait != null) portrait.Texture = ModEntry.ReplacementTexture;
        }
        else if (__instance.State == EpochSlotState.NotObtained)
        {
            // In NotObtained state, %Portrait shows a SubViewport texture (blur effect);
            // only %BlurPortrait is set from model.Portrait.
            var blurPortrait = __instance.GetNodeOrNull<TextureRect>("%BlurPortrait");
            if (blurPortrait != null) blurPortrait.Texture = ModEntry.ReplacementTexture;
        }
    }
}

// NEpochCard._Ready() only initialises _mask; the portrait texture is set in Init().
[HarmonyPatch(typeof(NEpochCard), nameof(NEpochCard.Init))]
static class NEpochCardInitPatch
{
    static void Postfix(NEpochCard __instance, EpochModel epochModel)
    {
        if (epochModel.Id != "NECROBINDER1_EPOCH") return;
        if (ModEntry.ReplacementTexture == null) return;

        var portrait = __instance.GetNodeOrNull<TextureRect>("%Portrait");
        if (portrait != null) portrait.Texture = ModEntry.ReplacementTexture;
    }
}

// _portrait.Texture is assigned at the start of Open() before the first await,
// so the postfix fires after it's already set.
[HarmonyPatch(typeof(NEpochInspectScreen), "Open")]
static class NEpochInspectScreenOpenPatch
{
    static void Postfix(NEpochInspectScreen __instance, EpochModel epoch)
    {
        if (epoch.Id != "NECROBINDER1_EPOCH") return;
        if (ModEntry.ReplacementTexture == null) return;

        var portrait = __instance.GetNodeOrNull<TextureRect>("%Portrait");
        if (portrait != null) portrait.Texture = ModEntry.ReplacementTexture;
    }
}

// OpenViaPaginator is the private method called when the user pages between epochs
// while the inspect screen is already open.
[HarmonyPatch(typeof(NEpochInspectScreen), "OpenViaPaginator")]
static class NEpochInspectScreenOpenViaPaginatorPatch
{
    static void Postfix(NEpochInspectScreen __instance, EpochModel epoch)
    {
        if (epoch.Id != "NECROBINDER1_EPOCH") return;
        if (ModEntry.ReplacementTexture == null) return;

        var portrait = __instance.GetNodeOrNull<TextureRect>("%Portrait");
        if (portrait != null) portrait.Texture = ModEntry.ReplacementTexture;
    }
}
