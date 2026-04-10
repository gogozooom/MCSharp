using BoboEngine;
using BoboEngine.UI;
using Minecraft.Entites;

namespace Minecraft.UI;

public class UIManager : ObjectBehavior
{
    public UIRenderer crosshairRenderer { get; private set; }
    public UIRenderer hotbarRenderer { get; private set; }
    public UIRenderer hotbarSelectionRenderer { get; private set; }

    public int guiSize = 3;

    public override void Start()
    {
        base.Start();

        // Create Crosshair
        var crosshairTexture = new Texture2D(TextureManager.GetPathToTexture("gui\\sprites\\hud\\crosshair.png"), "Crosshair", TextureSampleType.Nearest);
        var crosshair = new GameObject("CrosshairUI");
        crosshair.transform.parent = transform;
        crosshairRenderer = crosshair.AddComponent<UIRenderer>();
        crosshairRenderer.material = new Material("defaultUI", crosshairTexture, true, false, BlendMode.Invert, renderOrder: 1000);
        crosshairRenderer.uiTransform.pivot = Float2.one * 0.46f; // Bottom left pixel of center
        crosshairRenderer.uiTransform.scale = new Float2(crosshairTexture.width, crosshairTexture.height) * guiSize; 

        // Create Hotbar
        var hotbarTexture = new Texture2D(TextureManager.GetPathToTexture("gui\\sprites\\hud\\hotbar.png"), "Hotbar", TextureSampleType.Nearest);
        var hotbar = new GameObject("HotbarUI");
        hotbar.transform.parent = transform;
        hotbarRenderer = hotbar.AddComponent<UIRenderer>();
        hotbarRenderer.material = new Material("defaultUI", hotbarTexture, true, false, renderOrder: 1000);
        hotbarRenderer.uiTransform.anchor = new(0.5f, 0, 0.5f, 0);
        hotbarRenderer.uiTransform.pivot = new(0.5f, 0);
        hotbarRenderer.uiTransform.scale = new Float2(hotbarTexture.width, hotbarTexture.height) * guiSize; 

        // Create Hotbar Selection
        var hotbarSelectionTexture = new Texture2D(TextureManager.GetPathToTexture("gui\\sprites\\hud\\hotbar_selection.png"), "HotbarSelection", TextureSampleType.Nearest);
        var hotbarSelection = new GameObject("HotbarSelection");
        hotbarSelection.transform.parent = transform;
        hotbarSelectionRenderer = hotbarSelection.AddComponent<UIRenderer>();
        hotbarSelectionRenderer.material = new Material("defaultUI", hotbarSelectionTexture, true, false, renderOrder: 1001);
        hotbarSelectionRenderer.uiTransform.anchor = new(0.5f, 0, 0.5f, 0);
        hotbarSelectionRenderer.uiTransform.pivot = new(0, 0);
        hotbarSelectionRenderer.uiTransform.scale = new Float2(hotbarSelectionTexture.width, hotbarSelectionTexture.height) * guiSize; 
    }



    public override void Update()
    {
        base.Update();

        hotbarSelectionRenderer.uiTransform.position = new Float2(20 * (Player.slotSelected - 4.5f) - 2, 0) * guiSize;
    }
}
