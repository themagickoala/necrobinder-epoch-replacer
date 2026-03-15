using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;

[ModInitializer("Initialize")]
public class ModEntry
{
    private const string TargetEpochId = "NECROBINDER1_EPOCH";
    private const string ReplacementImagePath = "res://images/custom_epoch.png";

    private static Texture2D? _cachedTexture;
    private static Texture2D? _originalPortrait;
    private static Texture2D? _originalBigPortrait;

    public static void Initialize()
    {
        // Load replacement texture
        _cachedTexture = LoadReplacementTexture();
        if (_cachedTexture == null)
        {
            GD.PrintErr("[EpochReplacer] Failed to load replacement texture, mod disabled.");
            return;
        }

        // Cache original textures so we can identify what to replace
        var epoch = EpochModel.Get(TargetEpochId);
        _originalPortrait = epoch.Portrait;
        _originalBigPortrait = epoch.BigPortrait;

        GD.Print("[EpochReplacer] Loaded replacement texture. Originals cached.");

        // Hook into the scene tree's process frame signal
        var sceneTree = Engine.GetMainLoop() as SceneTree;
        if (sceneTree != null)
        {
            sceneTree.ProcessFrame += OnProcessFrame;
            GD.Print("[EpochReplacer] Hooked into ProcessFrame.");
        }
        else
        {
            GD.PrintErr("[EpochReplacer] Could not get SceneTree!");
        }
    }

    private static void OnProcessFrame()
    {
        var sceneTree = Engine.GetMainLoop() as SceneTree;
        var root = sceneTree?.Root;
        if (root == null)
            return;

        ReplaceInSubtree(root);
    }

    private static void ReplaceInSubtree(Node node)
    {
        if (node is NEpochSlot slot && slot.model?.Id == TargetEpochId)
        {
            ReplaceTextureRect(slot, "%Portrait");
            ReplaceTextureRect(slot, "%BlurPortrait");
        }
        else if (node is NEpochCard)
        {
            ReplaceTextureRect(node, "%Portrait");
        }
        else if (node is NEpochInspectScreen)
        {
            ReplaceTextureRect(node, "%Portrait");
        }

        foreach (var child in node.GetChildren())
        {
            ReplaceInSubtree(child);
        }
    }

    private static void ReplaceTextureRect(Node parent, string nodePath)
    {
        var rect = parent.GetNodeOrNull<TextureRect>(nodePath);
        if (rect == null)
            return;

        if (rect.Texture == _originalPortrait || rect.Texture == _originalBigPortrait)
            rect.Texture = _cachedTexture;
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
