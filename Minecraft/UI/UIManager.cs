using BoboEngine;
using BoboEngine.UI;

namespace Minecraft.UI;

public class UIManager : ObjectBehavior
{
    public UIRenderer crosshairRenderer { get; private set; }
    public override void Start()
    {
        base.Start();

        // Create Crosshair
        var crosshairTexture = new Texture2D(TextureManager.GetPathToTexture("gui\\sprites\\hud\\crosshair.png"), "Crosshair", TextureSampleType.Nearest);
        var crosshair = new GameObject("Crosshair");
        crosshair.transform.parent = transform;
        crosshairRenderer = crosshair.AddComponent<UIRenderer>();
        crosshairRenderer.material = new Material("defaultUI", crosshairTexture, true, false, BlendMode.Invert, renderOrder: 1000);
        crosshairRenderer.uvTransform.pivot = Float2.one * 0.45f; // Bottom left pixel of center
        crosshairRenderer.uvTransform.scale = new Float2(crosshairTexture.width, crosshairTexture.height) * 3; // TODO: replace 3 with GUI scale
    }
}
