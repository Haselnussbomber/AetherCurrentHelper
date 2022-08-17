using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace AetherCurrentHelper;

public class PluginWindow : Window
{
    private AetherCurrentEntry[]? aetherCurrents = null;

    public PluginWindow() : base("AetherCurrentHelper")
    {
        base.Size = new Vector2(350, 450);
        base.SizeCondition = ImGuiCond.FirstUseEver;

        base.SizeConstraints = new()
        {
            MinimumSize = new Vector2(350, 450),
            MaximumSize = new Vector2(4096, 2160)
        };

        base.Flags |= ImGuiWindowFlags.MenuBar;
    }

    public override bool DrawConditions()
    {
        return Service.ClientState.IsLoggedIn && Service.Data.IsDataReady;
    }

    public override void OnOpen()
    {
        if (aetherCurrents != null || !Service.Data.IsDataReady)
            return;

        var AetherCurrentSheet = Service.Data.GetExcelSheet<AetherCurrent>()!;
        var EObjSheet = Service.Data.GetExcelSheet<EObj>()!;
        var LevelSheet = Service.Data.GetExcelSheet<Level>()!;

        var eobjDataDict = new Dictionary<uint, EObj>();
        foreach (var row in EObjSheet)
        {
            if (row.Data != 0 && !eobjDataDict.ContainsKey(row.Data))
            {
                eobjDataDict.Add(row.Data, row);
            }
        }

        var levelObjectDict = new Dictionary<uint, Level>();
        foreach (var row in LevelSheet)
        {
            if (row.Object != 0 && !levelObjectDict.ContainsKey(row.Object))
            {
                levelObjectDict.Add(row.Object, row);
            }
        }

        aetherCurrents = AetherCurrentSheet
            .Select(aetherCurrent =>
            {
                var entry = new AetherCurrentEntry
                {
                    Entry = aetherCurrent,
                    Type = aetherCurrent.Quest.Row > 0 ? AetherCurrentType.Quest : AetherCurrentType.Object
                };

                switch (entry.Type)
                {
                    case AetherCurrentType.Quest:
                        var quest = aetherCurrent.Quest.Value;
                        if (quest != null)
                        {
                            entry.QuestName = Regex.Replace(Utils.ToClearString(quest.Name), @"^[\ue000-\uf8ff]+ ", "");

                            if (quest.IssuerStart > 0)
                            {
                                entry.QuestIssuerName = Service.GameFunctions.GetENpcResidentName(quest.IssuerStart);
                            }

                            entry.Level = quest.IssuerLocation.Value!;
                        }
                        break;

                    case AetherCurrentType.Object:
                        var obj = eobjDataDict.ContainsKey(aetherCurrent.RowId) ? eobjDataDict[aetherCurrent.RowId] : null;
                        if (obj != null)
                        {
                            entry.ObjectName = Service.GameFunctions.GetEObjName(obj.RowId);
                            entry.Level = levelObjectDict.ContainsKey(obj.RowId) ? levelObjectDict[obj.RowId] : null;
                        }
                        break;
                }

                return entry;
            })
            .Where(x => x.Level != null && x.Level.RowId != 0)
            .OrderByDescending(x => -x.Level!.Territory.Row)
            .ThenByDescending(x => x.Type)
            .ThenByDescending(x => x.Entry.RowId)
            .ToArray();
    }

    public override void Draw()
    {
        if (aetherCurrents == null) return;

        var Config = Configuration.Instance;

        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("Visibility"))
            {
                var showFinished = Config.ShowFinished;
                if (ImGui.MenuItem("Show finished", null, ref showFinished))
                {
                    Config.ShowFinished = showFinished;
                    Configuration.Save();
                }

                var onlyCurrent = Config.OnlyShowCurrentZone;
                if (ImGui.MenuItem("Show current zone only", null, ref onlyCurrent))
                {
                    Config.OnlyShowCurrentZone = onlyCurrent;
                    Configuration.Save();
                }

                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }

        Map? lastMap = null;
        var lastTree = false;
        var lastType = AetherCurrentType.Quest;
        var index = 1;

        foreach (var entry in aetherCurrents)
        {
            var sameZone = entry.Level!.Territory.Row == Service.ClientState.TerritoryType;
            if (Config.OnlyShowCurrentZone && !sameZone)
            {
                continue;
            }

            var isUnlocked = entry.IsUnlocked;

            if (!Config.ShowFinished && isUnlocked)
            {
                continue;
            }

            var map = entry.Level.Map.Value;
            if (map != null && lastMap != map)
            {
                index = 1;
                if (lastMap != null)
                {
                    ImGui.TreePop();
                }

                var sameMap = map.TerritoryType.Row == Service.ClientState.TerritoryType;
                lastTree = ImGui.CollapsingHeader(map.PlaceName.Value!.Name, sameMap ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None);

                ImGui.TreePush();
            }

            lastMap = map;

            if (!lastTree)
            {
                continue;
            }

            if (lastType != entry.Type)
            {
                index = 1;
                lastType = entry.Type;
            }

            var coords = Utils.GetHumanReadableCoords(entry.Level);
            var distance = Utils.GetDistance(entry);
            var distanceText = !sameZone || distance == float.MaxValue ? string.Empty : $" ({distance:0}y)";
            var name = entry.Type switch
            {
                AetherCurrentType.Object => entry.ObjectName,
                AetherCurrentType.Quest => "Quest",
                _ => "Unknown"
            };

            if (isUnlocked)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 1f, 0f, 1f));
            }

            var header = ImGui.CollapsingHeader($"[#{index++}] {name}{distanceText}###entry-{entry.Entry.RowId}");

            if (isUnlocked)
            {
                ImGui.PopStyleColor();
            }

            if (!header)
            {
                continue;
            }

            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, ImGui.CalcTextSize("Location").X + ImGui.GetStyle().ItemSpacing.X * 2);

            if (entry.Type == AetherCurrentType.Quest)
            {
                ImGui.TextUnformatted("Name");
                ImGui.NextColumn();
                ImGui.TextUnformatted(entry.QuestName);
                ImGui.NextColumn();

                ImGui.TextUnformatted("Issuer");
                ImGui.NextColumn();
                ImGui.TextUnformatted(entry.QuestIssuerName);
                ImGui.NextColumn();
            }

            ImGui.TextUnformatted("Location"); // TODO: add name
            ImGui.NextColumn();

            ImGui.TextUnformatted(coords);
            ImGui.SameLine();
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0f, 0f, 1f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1f, 0f, 0f, 1f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.6f, 0f, 0f, 1f));
                ImGui.PushFont(UiBuilder.IconFont);

                if (ImGui.Button($"{(char)FontAwesomeIcon.MapPin}##aethercurrents-map-{entry.Entry.RowId}"))
                {
                    Utils.OpenMapLocation(entry.Level);
                }

                ImGui.PopFont();
                ImGui.PopStyleColor(3);
            }
            ImGui.NextColumn();

            ImGui.Columns();
        }
    }
}
