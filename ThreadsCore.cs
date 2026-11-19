using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.Shared.Enums;
using ExileImGui;
using Hexa.NET.ImGui;
using Keys = System.Windows.Forms.Keys;
using SColor = SharpDX.Color;

namespace Threads;

public class Threads : BaseSettingsPlugin<ThreadsSettings>
{
    private const int SyncFrames = 30;
    private const float Pad = 6f;
    private const string PanelId = "threads";
    private static readonly int[] AreaInfoPath = { 2, 0 };
    private static readonly int[] ModListPath = { 2, 3, 0 };

    private static readonly (GameStat Stat, int Min, string League, string Text)[] ThreadMods =
    {
        (GameStat.MapBlightSpawnFriendlyPustules, 0, "Blight", "Encounters contain friendly Pustules"),
        (GameStat.MapBlightUpToXSuperchargedLanes, 0, "Blight", "Up to {0} Lanes are Supercharged"),
        (GameStat.MapBlightCoreSpawnMiniboss, 0, "Blight", "The Pump spawns a Miniboss"),
        (GameStat.MapBlightTowerCostPct, 0, "Blight", "Towers cost no Currency"),
        (GameStat.MapBlightReplaceXLanesWithGigaLane, 0, "Blight", "{0} Lane(s) replaced with a Giga Lane"),
        (GameStat.MapBlightReplaceXLanesWithCurrencyLane, 0, "Blight", "{0} Lane(s) replaced with a Currency Lane"),
        (GameStat.MapBlightReplaceXLanesWithExileLane, 0, "Blight", "{0} Lane(s) replaced with an Exile Lane"),
        (GameStat.MapBlightCoreReplacePantheonGod, 0, "Blight", "The Pump is replaced by a Pantheon God"),
        (GameStat.MapExpeditionEncounterScalePct, 0, "Expedition", "{0}% larger Encounter, 80% more Explosives, 100% more Monster Markers, +2-3 Relics"),
        (GameStat.MapExpeditionRunicMonstersAtLeastRare, 0, "Expedition", "Runic Monsters are at least Rare"),
        (GameStat.MapExpeditionEncounterSpawnReviveObelisk, 0, "Expedition", "Encounter contains a Revive Obelisk"),
        (GameStat.MapExpeditionLieutenantMarkerCount, 0, "Expedition", "+{0} Runic Lieutenant Markers"),
        (GameStat.MapExpeditionContainsXAdditionalLogbookChests, 0, "Expedition", "{0} additional Logbook Chest(s)"),
        (GameStat.MapExpeditionXAdditionalSpecialRemnantModifiers, 0, "Expedition", "{0} additional special Remnant Modifiers"),
        (GameStat.MapUltimatumRewardsAreInscribed, 0, "Ultimatum", "Rewards are Inscribed Ultimatums"),
        (GameStat.MapUltimatumRewardsAreCatalysts, 0, "Ultimatum", "Rewards are Catalysts"),
        (GameStat.MapUltimatumLeadsToBoss, 0, "Ultimatum", "Encounter leads to the Trialmaster"),
        (GameStat.MapUltimatumSpawnAdrenalineHearts, 0, "Ultimatum", "Spawns Adrenaline Hearts"),
        (GameStat.MapUltimatumCanDoubleDownFromRoundX, 0, "Ultimatum", "Can Double Down from Round {0}, the Trialmaster cannot appear"),
        (GameStat.MapUltimatumSpawnXCursedCurrencyPerRound, 0, "Ultimatum", "{0} Cursed Currency per Round, +20% more each Round"),
        (GameStat.MapUltimatumSpawnSoulCoreObject, 0, "Ultimatum", "Spawns a Soul Core"),
        (GameStat.MapUltimatumXRoundsSpawnOversoul, 0, "Ultimatum", "{0} Round(s) spawn an Oversoul"),
        (GameStat.MapUltimatumEnableSecondaryStats, 0, "Ultimatum", "Modifiers have secondary effects"),
        (GameStat.MapRitualTypeIsNameless, 0, "Ritual", "Rituals are Nameless"),
        (GameStat.RitualAltarsSpawnWisp, 0, "Ritual", "Altars spawn a Wildwood Wisp"),
        (GameStat.MapFinalRitualBodachArena, 0, "Ritual", "Final Ritual is a Bodach arena"),
        (GameStat.MapRitualFinalRitualExpendTributeForBoss, 0, "Ritual", "Final Ritual can spend Tribute to summon a Boss"),
        (GameStat.MapRitualGainBloodShrineOnCompletion, 0, "Ritual", "Completing a Ritual grants a Blood Shrine"),
        (GameStat.MapRitualRewardCountPct, 0, "Ritual", "{0}% increased number of Ritual rewards"),
        (GameStat.MapBetrayalStandaloneCatarina, 0, "Betrayal", "Catarina appears on her own"),
        (GameStat.MapBetrayalEncounterGrantBuffs, 0, "Betrayal", "Encounters grant Buffs"),
        (GameStat.MapBetrayalSpawnCorpseSculpture, 0, "Betrayal", "Spawns a Corpse Sculpture"),
        (GameStat.MapBetrayalMembersXAdditionalMods, 0, "Betrayal", "Syndicate Members have {0} additional Modifiers"),
        (GameStat.MapBetrayalPctChanceVeiledItemsToDropWithFracturedVeiledMod, 0, "Betrayal", "{0}% chance for Veiled Items to drop with a Fractured Veiled Modifier"),
        (GameStat.MapBetrayalSpawnResearchChestClusters, 0, "Betrayal", "{0} Research Chest clusters"),
        ((GameStat)23636, 0, "Betrayal", "Initial Syndicate targets have {0} additional Rare Bodyguards"),
        (GameStat.MapFinalAbyssPitSpawnAbyssMiniboss, 0, "Abyss", "{0}% chance for the final Abyss Pit to spawn a Consort"),
        (GameStat.MapNumExtraAbysses, 0, "Abyss", "{0} additional Abyss(es)"),
        (GameStat.MapAbyssEliteMonstersPaired, 0, "Abyss", "{0}% chance for Elite Monsters to come in pairs"),
        (GameStat.MapAbyssPitsFireMortars, 0, "Abyss", "Abyss Pits fire Mortars"),
        (GameStat.MapAbyssRaresXAdditionalAbyssMods, 0, "Abyss", "Rare Monsters have {0} additional Abyss Modifier(s)"),
        (GameStat.MapIncursionCanHaveGoldenDoors, 0, "Incursion", "Can have Golden Doors"),
        (GameStat.MapIncursionContainsDoorUnlockingObject, 0, "Incursion", "Contains a door-unlocking object"),
        (GameStat.MapIncursionReplacedWithStandaloneArchitects, 0, "Incursion", "Packs replaced with standalone Architects"),
        (GameStat.MapIncursionContainsXVaalVessels, 0, "Incursion", "Contains {0} Vaal Vessels"),
        (GameStat.MapIncursionContainsXGoldPiles, 0, "Incursion", "Contains {0} Gold Piles"),
        (GameStat.MapIncursionContainsTempests, 0, "Incursion", "Contains Tempests"),
        (GameStat.MapAlvaCursedTreasure, 0, "Incursion", "Contains Cursed Treasure"),
        (GameStat.MapIncursionsSpawnSpecialMonster, 0, "Incursion", "Spawns the Flesh Merchant"),
        ((GameStat)23638, 0, "Incursion", "Architects gain {0} Rare Bodyguard(s) per previous Incursion"),
        (GameStat.MapLegionLeagueExtraSpawns, 0, "Legion", "{0} additional Legion encounter(s)"),
        (GameStat.MapLegionMultipleBreakoutChancePct, 0, "Legion", "{0}% chance for Eternal Conflict (multiple Breakouts)"),
        (GameStat.MapLegionLeagueForceGeneral, 0, "Legion", "Always has a General, 50% chance for both Generals"),
        (GameStat.MapLegionInstantBreakoutChancePct, 0, "Legion", "{0}% chance for an instant Breakout"),
        (GameStat.MapLegionAllChestsWarHoardsChancePct, 0, "Legion", "{0}% chance for all Chests to be War Hoards"),
        (GameStat.MapLegionSmallCrystalsChancePct, 0, "Legion", "{0}% chance for Small Crystals"),
        (GameStat.MapLegionInvertedLegionChancePct, 0, "Legion", "{0}% chance for an Inverted Legion"),
        (GameStat.MapLegionUpgradeMonsterRankChancePct, 0, "Legion", "{0}% chance for Monsters to rank up"),
        (GameStat.MapAdditionalRedBeasts, 18, "Bestiary", "Red Beast invasion: {0} additional Red Beasts"),
        (GameStat.MapAdditionalRedBeasts, 2, "Bestiary", "{0} additional Red Beasts"),
        (GameStat.MapBestiaryAreaContainsBlackMorrigan, 0, "Bestiary", "Contains the Black Morrigan"),
        (GameStat.MapXBestiaryPacksAreHarvestBeasts, 0, "Bestiary", "{0} Bestiary Packs are Harvest Beasts"),
        (GameStat.MapBestiaryBeastsHaveXWispsOfAllTypes, 0, "Bestiary", "Beasts are imbued with Wisps of all types"),
        (GameStat.MapBestiaryEinharBloodExplodeAura, 0, "Bestiary", "Einhar has an exploding Blood Aura"),
        (GameStat.MapBestiaryYellowBeastsAreHulks, 0, "Bestiary", "Yellow Beasts are replaced with Azmeri Hulks"),
        (GameStat.MapBestiaryBeastsXAdditionalModifiers, 0, "Bestiary", "Beasts have {0} additional Modifier(s)"),
        (GameStat.MapBreachRareAdditionalWombgiftChancePct, 0, "Breach", "Rare Breach Monsters have {0}% chance to drop an additional Wombgift"),
        (GameStat.MapUnstableBreachSpawnsBoss, 0, "Breach", "Unstable Breaches spawn a Boss"),
        (GameStat.MapBreachHiveIsAlwaysSkyFortress, 0, "Breach", "Hives are always a Sky Fortress"),
        (GameStat.MapContainsAdditionalUnstableBreaches, 0, "Breach", "{0} additional Unstable Breach(es)"),
        (GameStat.MapBreachContainsGenesisTreeSapling, 0, "Breach", "Contains a Genesis Tree Sapling"),
        (GameStat.MapBreachHivesContainSpreadCoffers, 0, "Breach", "Hives contain Spread Coffers"),
        (GameStat.MapBreachRaresXAdditionalBreachMods, 0, "Breach", "Rare Breach Monsters have {0} additional Breach Modifier(s)"),
        (GameStat.MapBreachEmpoweredAilithSkills, 0, "Breach", "Ailith's Skills are Empowered"),
        ((GameStat)23631, 0, "Breach", "Magic Breach Monsters have {0} additional Modifier(s)"),
        ((GameStat)23634, 0, "Breach", "The final Rare Breach Monsters are duplicated"),
    };

    private static readonly string[] ModKeys = ThreadMods.Select(m => $"{m.Stat}:{m.Min}").ToArray();

    private static readonly IGrouping<string, int>[] ModGroups =
        Enumerable.Range(0, ThreadMods.Length).GroupBy(i => ThreadMods[i].League).ToArray();

    private static readonly Regex LeagueStat = new(
        @"^Map(\w+?)(ScarabCount|MonsterFromLeagueItemRarityPctPermyriad|MonsterDifficultyTankinessPct|MonsterDamagePct)$",
        RegexOptions.Compiled);

    private readonly Dictionary<string, (int Count, int Rarity, int Tank, int Dmg)> _leagues = new();
    private readonly List<(int Idx, string Text)> _mods = new();
    private readonly List<(string Text, SColor? Color, float Scale, Vector2 Size)> _rows = new();
    private readonly Overlay _overlay = new() { Locked = true };
    private uint _loggedHash;
    private int _syncCounter;
    private (int Width, float Scale) _layoutSig;

    private string LogPath => Path.Combine(ConfigDirectory, "maps.jsonl");

    public override bool Initialise()
    {
        if (!File.Exists(LogPath)) return true;
        try
        {
            var last = File.ReadLines(LogPath).LastOrDefault(l => l.Length > 0);
            if (last == null) return true;
            using var doc = JsonDocument.Parse(last);
            if (doc.RootElement.TryGetProperty("Hash", out var h)) _loggedHash = h.GetUInt32();
        }
        catch (Exception ex) when (ex is JsonException or IOException) { }
        return true;
    }

    public override void Render()
    {
        if (Settings.ToggleKey.PressedOnce()) Settings.Show.Value = !Settings.Show.Value;
        if (!Settings.Show && !Settings.LogMaps) { _overlay.Release(PanelId); return; }
        var sig = (Settings.Width, Settings.FontScale.Value);
        if (--_syncCounter <= 0 || sig != _layoutSig)
        {
            _syncCounter = SyncFrames;
            _layoutSig = sig;
            Sync();
        }

        if (!Settings.Show) { _overlay.Release(PanelId); return; }
        Draw();
    }

    private void Sync()
    {
        _rows.Clear();
        _leagues.Clear();
        _mods.Clear();
        var stats = GameController.IngameState?.Data?.MapStats;
        if (stats == null) return;
        float scale = Settings.FontScale.Value;

        foreach (var (key, v) in stats)
        {
            if (v == 0) continue;
            var m = LeagueStat.Match(key.ToString());
            if (!m.Success) continue;
            var league = m.Groups[1].Value;
            _leagues.TryGetValue(league, out var e);
            switch (m.Groups[2].Value)
            {
                case "ScarabCount": e.Count = v; break;
                case "MonsterFromLeagueItemRarityPctPermyriad": e.Rarity = v; break;
                case "MonsterDifficultyTankinessPct": e.Tank = v; break;
                default: e.Dmg = v; break;
            }
            _leagues[league] = e;
        }

        GameStat last = default;
        bool lastHit = false;
        for (int i = 0; i < ThreadMods.Length; i++)
        {
            var (stat, min, league, text) = ThreadMods[i];
            if (lastHit && stat == last) continue;
            last = stat;
            lastHit = stats.TryGetValue(stat, out var v) && v != 0 && (min == 0 || v >= min);
            if (!lastHit) continue;
            _mods.Add((i, $"{league} Thread: {text.Replace("{0}", v.ToString())}"));
        }

        if (Settings.LogMaps) Log();

        if (Settings.ShowLeagueCounts)
        {
            foreach (var (league, e) in _leagues)
            {
                var parts = new List<string>(3);
                if (e.Rarity != 0) parts.Add($"{e.Rarity / 100f:+0.#;-0.#}% item rarity");
                if (e.Tank != 0) parts.Add($"{e.Tank:+0;-0}% toughness");
                if (e.Dmg != 0) parts.Add($"{e.Dmg:+0;-0}% damage");
                var head = e.Count > 0 ? $"{league} Threads x{e.Count}" : league;
                Wrap(parts.Count == 0 ? head : $"{head}: monsters {string.Join(", ", parts)}", null, scale);
            }
        }

        if (Settings.ShowThreadMods)
            foreach (var (idx, text) in _mods)
            {
                var s = Settings.Mods.GetValueOrDefault(ModKeys[idx]);
                if (s?.Show == false) continue;
                Wrap(text, s?.Color is int c ? new SColor(c) : null, scale * (s?.Size ?? 1f));
            }
    }

    private void Log()
    {
        var area = GameController.Area.CurrentArea;
        if (area == null || area.IsTown || area.IsHideout || area.Hash == _loggedHash) return;
        var threads = new Dictionary<string, int>();
        var monsters = new Dictionary<string, object>();
        foreach (var (league, e) in _leagues)
        {
            if (e.Count > 0) threads[league] = e.Count;
            monsters[league] = new { Rarity = e.Rarity / 100f, Toughness = e.Tank, Damage = e.Dmg };
        }
        if (threads.Count == 0 && _mods.Count == 0) return;
        _loggedHash = area.Hash;
        var entry = new { Time = DateTime.Now, Area = area.Name, Level = area.RealLevel, Hash = area.Hash, Threads = threads, Monsters = monsters, Mods = _mods.Select(m => m.Text) };
        Directory.CreateDirectory(ConfigDirectory);
        File.AppendAllText(LogPath, JsonSerializer.Serialize(entry) + Environment.NewLine);
    }

    private void Wrap(string text, SColor? color, float scale)
    {
        float wrap = (Settings.Width - Pad * 2) / scale;
        var line = "";
        foreach (var word in text.Split(' '))
        {
            var next = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && ImGui.CalcTextSize(next).X > wrap)
            {
                AddRow(line, color, scale);
                next = word;
            }
            line = next;
        }
        AddRow(line, color, scale);
    }

    private void AddRow(string text, SColor? color, float scale) =>
        _rows.Add((text, color, scale, ImGui.CalcTextSize(text) * scale));

    private unsafe void Draw()
    {
        if (_rows.Count == 0) { _overlay.Release(PanelId); return; }

        float gap = 2f * Settings.FontScale.Value;
        float tall = Pad * 2 - gap;
        foreach (var row in _rows) tall += row.Size.Y + gap;

        var anchor = Settings.AutoAnchor ? AnchorOrigin() : null;
        var origin = anchor ?? new Vector2(Settings.PosX, Settings.PosY);
        var size = new Vector2(Settings.Width, tall);
        if (UiCovers(origin, origin + size)) { _overlay.Release(PanelId); return; }

        bool hovered = false, active = false;
        var edge = Overlay.ResizeEdge.None;
        if (anchor == null)
        {
            _overlay.MoveHeld = Input.GetKeyState(Keys.LShiftKey) || Input.GetKeyState(Keys.RShiftKey);
            (hovered, edge, active) = _overlay.Handle(PanelId, ref origin, size,
                ref Settings.PosX, ref Settings.PosY, ref Settings.Width, 150, 3000);
            if (hovered || active) Overlay.ClickBlocker(PanelId, origin, size);
        }

        var (over, _) = Overlay.HitTest(ImGui.GetMousePos(), origin, size);
        float fade = over ? 1f : Settings.IdleOpacity.Value / 255f;
        if (fade <= 0f) return;

        var dl = ImGui.GetBackgroundDrawList();
        Overlay.Chrome(dl, origin, origin + size, Fade(Settings.BackColor, fade), Fade(Settings.BorderColor, fade),
            Settings.BorderThickness.Value);

        uint baseCol = EColor.U32(Fade(Settings.TextColor, fade));
        var font = ImGui.GetFont();
        float fontSize = ImGui.GetFontSize();
        float y = origin.Y + Pad;
        for (int i = 0; i < _rows.Count; i++)
        {
            var (text, color, rowScale, rowSize) = _rows[i];
            uint col = color is { } c ? EColor.U32(Fade(c, fade)) : baseCol;
            dl.AddText(font, fontSize * rowScale, new Vector2(origin.X + Pad, y), col, text);
            y += rowSize.Y + gap;
        }

        if (anchor == null && _overlay.Movable) Overlay.DragHint(origin, origin + size, active, hovered, edge);
    }

    private Vector2? AnchorOrigin()
    {
        var map = GameController.IngameState?.IngameUi?.Map;
        if (map == null) return null;
        var mods = map.GetChildFromIndices(ModListPath);
        var list = mods is { IsVisible: true } ? mods : map.GetChildFromIndices(AreaInfoPath);
        if (list is not { IsVisible: true }) return null;
        var r = list.GetClientRectCache;
        return r.Width > 0 ? new Vector2(r.Right - Settings.Width, r.Bottom + Pad) : null;
    }

    public override void DrawSettings()
    {
        if (!ImGui.BeginTabBar("##threads_tabs")) return;
        if (ImGui.BeginTabItem("General"))
        {
            base.DrawSettings();
            DrawLogButtons();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Customization"))
        {
            DrawCustomization();
            ImGui.EndTabItem();
        }
        ImGui.EndTabBar();
    }

    private void DrawLogButtons()
    {
        ImGui.SeparatorText("Map log");
        if (ImGui.Button("Open folder"))
        {
            Directory.CreateDirectory(ConfigDirectory);
            Process.Start("explorer.exe", $"\"{ConfigDirectory}\"");
        }
        ImGui.SameLine();
        ImGui.BeginDisabled(!File.Exists(LogPath));
        bool archive = ImGui.Button("Archive");
        ImGui.SetItemTooltip("Renames maps.jsonl to maps-<date>.jsonl - logging starts a fresh file");
        ImGui.SameLine();
        if (ImGui.Button("Delete")) ImGui.OpenPopup("##threads_delete_log");
        ImGui.EndDisabled();
        bool delete = false;
        if (ImGui.BeginPopup("##threads_delete_log"))
        {
            ImGui.TextUnformatted("Delete maps.jsonl? Archives are kept.");
            delete = ImGui.Button("Delete");
            ImGui.SameLine();
            if (ImGui.Button("Cancel") || delete) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
        if (!archive && !delete) return;
        try
        {
            if (archive) File.Move(LogPath, Path.Combine(ConfigDirectory, $"maps-{DateTime.Now:yyyyMMdd-HHmmss}.jsonl"));
            else File.Delete(LogPath);
            _loggedHash = 0;
        }
        catch (Exception ex) { LogError($"Threads map log action failed: {ex.Message}"); }
    }

    private void DrawCustomization()
    {
        ImGui.TextDisabled("Unset color = General text color, size multiplies the General font scale");
        var baseCol = EColor.ToVector4(Settings.TextColor);
        foreach (var group in ModGroups)
        {
            if (!ImGui.CollapsingHeader(group.Key)) continue;
            if (!ImGui.BeginTable($"##mods_{group.Key}", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit)) continue;
            ImGui.TableSetupColumn("Show");
            ImGui.TableSetupColumn("Color");
            ImGui.TableSetupColumn("Size");
            ImGui.TableSetupColumn("Modifier", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("");
            ImGui.TableHeadersRow();
            foreach (var i in group)
            {
                var key = ModKeys[i];
                var s = Settings.Mods.GetValueOrDefault(key) ?? new ModStyle();
                var col = s.Color is int c ? EColor.ToVector4(new SColor(c)) : baseCol;
                bool changed = false, reset = false;
                ImGui.PushID(key);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                changed |= ImGui.Checkbox("##show", ref s.Show);
                ImGui.TableNextColumn();
                if (ImGui.ColorEdit4("##color", ref col, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
                {
                    s.Color = EColor.FromVector4(col).ToRgba();
                    changed = true;
                }
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(110);
                changed |= ImGui.SliderFloat("##size", ref s.Size, 0.5f, 3f, "%.2fx");
                ImGui.TableNextColumn();
                var label = ThreadMods[i].Text.Replace("{0}", "#");
                if (s.Show) ImGui.PushStyleColor(ImGuiCol.Text, col); else ImGui.BeginDisabled();
                ImGui.TextUnformatted(label);
                if (s.Show) ImGui.PopStyleColor(); else ImGui.EndDisabled();
                ImGui.TableNextColumn();
                if (Settings.Mods.ContainsKey(key)) reset = ImGui.SmallButton("Reset");
                ImGui.PopID();

                if (reset || (changed && s.Show && s.Color == null && s.Size == 1f)) Settings.Mods.Remove(key);
                else if (changed) Settings.Mods[key] = s;
                if (reset || changed) _syncCounter = 0;
            }
            ImGui.EndTable();
        }
    }

    private static SColor Fade(SColor c, float f) => f >= 1f ? c : new SColor(c.R, c.G, c.B, (byte)(c.A * f));

    private bool UiCovers(Vector2 min, Vector2 max)
    {
        var ui = GameController.IngameState?.IngameUi;
        if (ui == null) return false;
        if (Overlaps(ui.OpenLeftPanel, min, max) || Overlaps(ui.OpenRightPanel, min, max)) return true;
        foreach (var p in ui.LargePanels) if (p?.IsVisible == true) return true;
        foreach (var p in ui.FullscreenPanels) if (Overlaps(p, min, max)) return true;
        return false;
    }

    private static bool Overlaps(Element e, Vector2 min, Vector2 max)
    {
        if (e == null || !e.IsVisible) return false;
        var r = e.GetClientRectCache;
        return r.Width > 0 && r.Left < max.X && r.Right > min.X && r.Top < max.Y && r.Bottom > min.Y;
    }
}
