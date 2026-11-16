using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.Shared.Enums;
using ExileImGui;
using Hexa.NET.ImGui;
using Keys = System.Windows.Forms.Keys;

namespace Threads;

public class Threads : BaseSettingsPlugin<ThreadsSettings>
{
    private const int SyncFrames = 30;
    private const float Pad = 6f;

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
        (GameStat.MapBetrayalStandaloneCatarina, 0, "Betrayal", "Catarina appears on her own"),
        (GameStat.MapBetrayalEncounterGrantBuffs, 0, "Betrayal", "Encounters grant Buffs"),
        (GameStat.MapBetrayalSpawnCorpseSculpture, 0, "Betrayal", "Spawns a Corpse Sculpture"),
        (GameStat.MapBetrayalMembersXAdditionalMods, 0, "Betrayal", "Syndicate Members have {0} additional Modifiers"),
        (GameStat.MapBetrayalPctChanceVeiledItemsToDropWithFracturedVeiledMod, 0, "Betrayal", "{0}% chance for Veiled Items to drop with a Fractured Veiled Modifier"),
        (GameStat.MapBetrayalSpawnResearchChestClusters, 0, "Betrayal", "{0} Research Chest clusters"),
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
        (GameStat.MapBreachRareAdditionalWombgiftChancePct, 0, "Breach", "Rare Breach Monsters have {0}% chance to drop an additional Wombgift"),
        (GameStat.MapUnstableBreachSpawnsBoss, 0, "Breach", "Unstable Breaches spawn a Boss"),
        (GameStat.MapBreachHiveIsAlwaysSkyFortress, 0, "Breach", "Hives are always a Sky Fortress"),
        (GameStat.MapContainsAdditionalUnstableBreaches, 0, "Breach", "{0} additional Unstable Breach(es)"),
        (GameStat.MapBreachContainsGenesisTreeSapling, 0, "Breach", "Contains a Genesis Tree Sapling"),
        (GameStat.MapBreachHivesContainSpreadCoffers, 0, "Breach", "Hives contain Spread Coffers"),
        (GameStat.MapBreachRaresXAdditionalBreachMods, 0, "Breach", "Rare Breach Monsters have {0} additional Breach Modifier(s)"),
        (GameStat.MapBreachEmpoweredAilithSkills, 0, "Breach", "Ailith's Skills are Empowered"),
    };

    private static readonly Regex LeagueStat = new(
        @"^Map(\w+?)(ScarabCount|MonsterFromLeagueItemRarityPctPermyriad|MonsterDifficultyTankinessPct|MonsterDamagePct)$",
        RegexOptions.Compiled);

    private readonly Dictionary<string, (int Count, int Rarity, int Tank, int Dmg)> _leagues = new();
    private readonly List<string> _mods = new();
    private readonly List<string> _rows = new();
    private readonly List<Vector2> _sizes = new();
    private readonly Overlay _overlay = new();
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
            if (last != null && JsonDocument.Parse(last).RootElement.TryGetProperty("Hash", out var h)) _loggedHash = h.GetUInt32();
        }
        catch (JsonException) { }
        return true;
    }

    public override void Render()
    {
        if (Settings.ToggleKey.PressedOnce()) Settings.Show.Value = !Settings.Show.Value;
        var sig = (Settings.Width, Settings.FontScale.Value);
        if (--_syncCounter <= 0 || sig != _layoutSig)
        {
            _syncCounter = SyncFrames;
            _layoutSig = sig;
            Sync();
        }

        if (!Settings.Show) { _overlay.Release("threads"); return; }
        Draw();
    }

    private void Sync()
    {
        _rows.Clear();
        _leagues.Clear();
        _mods.Clear();
        var stats = GameController?.IngameState?.Data?.MapStats;
        if (stats == null) return;
        float wrap = (Settings.Width - Pad * 2) / Settings.FontScale.Value;

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
        foreach (var (stat, min, league, text) in ThreadMods)
        {
            if (lastHit && stat == last) continue;
            last = stat;
            lastHit = stats.TryGetValue(stat, out var v) && v != 0 && v >= min;
            if (lastHit) _mods.Add($"{league} Thread: {text.Replace("{0}", v.ToString())}");
        }

        if (Settings.LogMaps) Log();

        if (Settings.ShowLeagueCounts)
        {
            foreach (var (league, e) in _leagues)
            {
                var parts = new List<string>(3);
                if (e.Rarity != 0) parts.Add($"{e.Rarity / 100f:+0.#;-0.#}% item rarity");
                if (e.Tank != 0) parts.Add($"{e.Tank:+0;-0}% tankiness");
                if (e.Dmg != 0) parts.Add($"{e.Dmg:+0;-0}% damage");
                var head = e.Count > 0 ? $"{league} Threads x{e.Count}" : league;
                Wrap(parts.Count == 0 ? head : $"{head}: monsters {string.Join(", ", parts)}", wrap);
            }
        }

        if (Settings.ShowThreadMods)
            foreach (var mod in _mods) Wrap(mod, wrap);
    }

    private void Log()
    {
        var area = GameController.Area.CurrentArea;
        if (area == null || area.IsTown || area.IsHideout || area.Hash == _loggedHash) return;
        var threads = new Dictionary<string, int>();
        foreach (var (league, e) in _leagues) if (e.Count > 0) threads[league] = e.Count;
        if (threads.Count == 0 && _mods.Count == 0) return;
        _loggedHash = area.Hash;
        var entry = new { Time = DateTime.Now, Area = area.Name, Level = area.RealLevel, Hash = area.Hash, Threads = threads, Mods = _mods };
        Directory.CreateDirectory(ConfigDirectory);
        File.AppendAllText(LogPath, JsonSerializer.Serialize(entry) + Environment.NewLine);
    }

    private void Wrap(string text, float wrap)
    {
        if (wrap <= 0f || ImGui.CalcTextSize(text).X <= wrap) { _rows.Add(text); return; }
        var line = new StringBuilder();
        foreach (var word in text.Split(' '))
        {
            if (line.Length == 0) { line.Append(word); continue; }
            int keep = line.Length;
            line.Append(' ').Append(word);
            if (ImGui.CalcTextSize(line.ToString()).X <= wrap) continue;
            line.Length = keep;
            _rows.Add(line.ToString());
            line.Clear().Append(word);
        }
        if (line.Length > 0) _rows.Add(line.ToString());
    }

    private unsafe void Draw()
    {
        if (_rows.Count == 0) { _overlay.Release("threads"); return; }

        float scale = Settings.FontScale.Value;
        float gap = 2f * scale;
        _sizes.Clear();
        float tall = 0f;
        foreach (var row in _rows)
        {
            var sz = ImGui.CalcTextSize(row) * scale;
            _sizes.Add(sz);
            tall += sz.Y + gap;
        }

        var origin = new Vector2(Settings.PosX, Settings.PosY);
        var size = new Vector2(Settings.Width, tall - gap + Pad * 2);
        if (UiCovers(origin, origin + size)) { _overlay.Release("threads"); return; }

        _overlay.Locked = true;
        _overlay.MoveHeld = Input.GetKeyState(Keys.LShiftKey) || Input.GetKeyState(Keys.RShiftKey);
        var (hovered, edge, active) = _overlay.Handle("threads", ref origin, size,
            ref Settings.PosX, ref Settings.PosY, ref Settings.Width, 150, 3000);
        if (hovered || active) Overlay.ClickBlocker("threads", origin, size);

        var mouse = ImGui.GetMousePos();
        bool over = mouse.X >= origin.X && mouse.X <= origin.X + size.X && mouse.Y >= origin.Y && mouse.Y <= origin.Y + size.Y;
        float fade = over ? 1f : Settings.IdleOpacity.Value / 255f;
        if (fade <= 0f) return;

        var dl = ImGui.GetBackgroundDrawList();
        Overlay.Chrome(dl, origin, origin + size, Fade(Settings.BackColor, fade), Fade(Settings.BorderColor, fade),
            Settings.BorderThickness.Value);

        uint col = EColor.U32(Fade(Settings.TextColor, fade));
        var font = ImGui.GetFont();
        float fontSize = ImGui.GetFontSize() * scale;
        float y = origin.Y + Pad;
        for (int i = 0; i < _rows.Count; i++)
        {
            dl.AddText(font, fontSize, new Vector2(origin.X + Pad, y), col, _rows[i]);
            y += _sizes[i].Y + gap;
        }

        if (_overlay.Movable) Overlay.DragHint(origin, origin + size, active, hovered, edge);
    }

    private static SharpDX.Color Fade(SharpDX.Color c, float f) => f >= 1f ? c : new SharpDX.Color(c.R, c.G, c.B, (byte)(c.A * f));

    private bool UiCovers(Vector2 min, Vector2 max)
    {
        var ui = GameController?.IngameState?.IngameUi;
        if (ui == null) return false;
        foreach (var p in ui.LargePanels) if (p?.IsVisible == true) return true;
        foreach (var p in ui.FullscreenPanels) if (Overlaps(p, min, max)) return true;
        return Overlaps(ui.OpenLeftPanel, min, max) || Overlaps(ui.OpenRightPanel, min, max);
    }

    private static bool Overlaps(Element e, Vector2 min, Vector2 max)
    {
        if (e == null || !e.IsVisible) return false;
        var r = e.GetClientRectCache;
        return r.Width > 0 && r.Left < max.X && r.Right > min.X && r.Top < max.Y && r.Bottom > min.Y;
    }
}
