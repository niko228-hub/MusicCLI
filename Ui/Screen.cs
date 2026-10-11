using MusicCli.Player;
using Spectre.Console;

namespace MusicCli.Ui;

public static class Screen
{
    public static void DrawHeader(TrackLibrary library, MusicPlayer player)
    {
        var rule = new Rule("[white]MUSIC CLI[/]")
        {
            Style = Theme.DimStyle,
            Justification = Justify.Left
        };
        AnsiConsole.Write(rule);

        var state = player.State switch
        {
            PlaybackState.Playing => "PLAYING",
            PlaybackState.Paused => "PAUSED",
            _ => "STOPPED"
        };

        var now = player.CurrentName ?? library.SelectedName ?? "—";
        var folder = ShortPath(library.Folder);

        var table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("k").Width(10))
            .AddColumn("v");

        table.AddRow(Theme.DimText("folder"), Theme.MidText(folder));
        table.AddRow(Theme.DimText("track"), Theme.InkText(now));
        table.AddRow(Theme.DimText("state"), $"[{StateColor(player.State)}]{state}[/]");
        table.AddRow(Theme.DimText("engine"), Theme.DimText(player.Backend));
        table.AddRow(Theme.DimText("files"), Theme.DimText($"{library.Tracks.Count}"));

        AnsiConsole.Write(table);
        AnsiConsole.Write(new Rule { Style = Theme.DimStyle });
    }

    public static void DrawHelp()
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow(
            HelpPair("play", "воспроизвести"),
            HelpPair("pause", "приостановить"),
            HelpPair("select", "выбрать композицию"),
            HelpPair("cd", "выбрать папку")
        );
        grid.AddRow(
            HelpPair("stop", "остановить"),
            HelpPair("list", "список композиций"),
            HelpPair("help", "справка"),
            HelpPair("exit", "завершить работу")
        );

        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine();
    }

    public static void Info(string message) =>
        AnsiConsole.MarkupLine($"[silver]{Markup.Escape(message)}[/]");

    public static void Dim(string message) =>
        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(message)}[/]");

    public static void Error(string message) =>
        AnsiConsole.MarkupLine($"[white]![/] [grey]{Markup.Escape(message)}[/]");

    public static void RenderTrackList(TrackLibrary library)
    {
        if (library.Tracks.Count == 0)
        {
            Dim("В этой папке нет аудиофайлов.");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderStyle(Theme.DimStyle)
            .AddColumn(new TableColumn(Theme.DimText("#")).RightAligned().Width(4))
            .AddColumn(Theme.DimText("track"));

        for (var i = 0; i < library.Tracks.Count; i++)
        {
            var name = Path.GetFileName(library.Tracks[i]);
            var mark = i == library.SelectedIndex;
            var index = mark ? $"[white]{i + 1}[/]" : $"[grey]{i + 1}[/]";
            var title = mark ? $"[white]▶ {Markup.Escape(name)}[/]" : $"[grey]  {Markup.Escape(name)}[/]";
            table.AddRow(index, title);
        }

        AnsiConsole.Write(table);
    }

    private static string HelpPair(string cmd, string desc) =>
        $"[white]{cmd}[/] [grey]{Markup.Escape(desc)}[/]";

    private static string StateColor(PlaybackState state) => state switch
    {
        PlaybackState.Playing => "white",
        PlaybackState.Paused => "silver",
        _ => "grey"
    };

    private static string ShortPath(string path)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(home) && path.StartsWith(home, StringComparison.OrdinalIgnoreCase))
            return "~" + path[home.Length..];
        return path;
    }
}