using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;

namespace AetherCurrentHelper;

public static class Utils
{
    public static void OpenMapLocation(Level level)
    {
        var map = level?.Map?.Value;
        var terr = map?.TerritoryType?.Value;

        if (terr == null)
            return;

        Service.GameGui.OpenMapWithMapLink(new MapLinkPayload(
            terr.RowId,
            map!.RowId,
            (int)(level!.X * 1_000f),
            (int)(level.Z * 1_000f)
        ));
    }

    public static float GetDistance(AetherCurrentEntry entry)
    {
        if (Service.ClientState.LocalPlayer == null || entry.Level == null || entry.Level.Territory.Row != Service.ClientState.TerritoryType)
        {
            return float.MaxValue; // far, far away
        }

        return Vector2.Distance(
            new Vector2(
                Service.ClientState.LocalPlayer.Position.X,
                Service.ClientState.LocalPlayer.Position.Z
            ),
            new Vector2(
                entry.Level.X,
                entry.Level.Z
            )
        );
    }

    public static string ToClearString(Lumina.Text.SeString? str)
    {
        return str == null ? "" : SeString.Parse(str.RawData).ToString();
    }

    public static string Capitalize(string str)
    {
        return Regex.Replace(str, @"\b\p{L}", (match) => match.Value.ToUpperInvariant());
    }

    public static Vector2 GetLevelPos(Level level)
    {
        var map = level.Map.Value;
        var c = map!.SizeFactor / 100.0f;
        var x = 41.0f / c * (((level.X + map.OffsetX) * c + 1024.0f) / 2048.0f) + 1f;
        var y = 41.0f / c * (((level.Z + map.OffsetY) * c + 1024.0f) / 2048.0f) + 1f;
        return new(x, y);
    }

    public static string GetHumanReadableCoords(Level level)
    {
        var coords = GetLevelPos(level);
        return $"X: {coords.X.ToString("0.0", CultureInfo.InvariantCulture)}, Y: {coords.Y.ToString("0.0", CultureInfo.InvariantCulture)}";
    }
}
