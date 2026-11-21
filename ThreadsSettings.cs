using System.Collections.Generic;
using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace Threads;

public class ThreadsSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(true);

    [Menu("Show overlay")]
    public ToggleNode Show { get; set; } = new ToggleNode(true);

    [Menu("Toggle key")]
    public HotkeyNodeV2 ToggleKey { get; set; } = new HotkeyNodeV2(Keys.None);

    [Menu("Anchor under game modifiers", "Right-aligned below the game's area modifier list, follows it as it grows. Turn off to move (Shift+drag) or resize (Shift+drag right edge) freely")]
    public ToggleNode AutoAnchor { get; set; } = new ToggleNode(true);

    [Menu("League thread counts", "Threads xN plus the monster rarity / toughness / damage they add")]
    public ToggleNode ShowLeagueCounts { get; set; } = new ToggleNode(true);

    [Menu("Thread modifiers")]
    public ToggleNode ShowThreadMods { get; set; } = new ToggleNode(true);

    [Menu("Log maps", "one line per map (area, level, threads, monster rarity / toughness / damage, thread mods) to maps.jsonl in the plugin's config folder")]
    public ToggleNode LogMaps { get; set; } = new ToggleNode(false);

    [Menu("Font scale")]
    public RangeNode<float> FontScale { get; set; } = new RangeNode<float>(1.2f, 0.5f, 3f);

    [Menu("Idle opacity", "full while the mouse is over it, 255 = never fade")]
    public RangeNode<int> IdleOpacity { get; set; } = new RangeNode<int>(255, 0, 255);

    [Menu("Text")]
    public ColorNode TextColor { get; set; } = new ColorNode(new Color(255, 200, 90, 255));

    [Menu("Background")]
    public ColorNode BackColor { get; set; } = new ColorNode(new Color(0, 0, 0, 160));

    [Menu("Border")]
    public ColorNode BorderColor { get; set; } = new ColorNode(new Color(128, 158, 191, 255));

    [Menu("Border thickness")]
    public RangeNode<float> BorderThickness { get; set; } = new RangeNode<float>(1f, 0f, 8f);

    public int PosX = 1500;
    public int PosY = 300;
    public int Width = 500;
    public Dictionary<string, ModStyle> Mods = new();
}

public class ModStyle
{
    public bool Show = true;
    public int? Color;
    public float Size = 1f;
}
