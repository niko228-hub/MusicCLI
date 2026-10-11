using MusicCli.Player;
using Spectre.Console;

namespace MusicCli.Ui;

public static class TrackPicker
{
    public static int Run(TrackLibrary library)
    {
        var tracks = library.Tracks;
        var index = library.SelectedIndex >= 0 ? library.SelectedIndex : 0;

        AnsiConsole.Cursor.Hide();
        try
        {
            while (true)
            {
                Draw(tracks, index);

                var key = Console.ReadKey(intercept: true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.K:
                        index = (index - 1 + tracks.Count) % tracks.Count;
                        break;

                    case ConsoleKey.DownArrow:
                    case ConsoleKey.J:
                        index = (index + 1) % tracks.Count;
                        break;

                    case ConsoleKey.Home:
                        index = 0;
                        break;

                    case ConsoleKey.End:
                        index = tracks.Count - 1;
                        break;

                    case ConsoleKey.PageUp:
                        index = Math.Max(0, index - VisibleRows());
                        break;

                    case ConsoleKey.PageDown:
                        index = Math.Min(tracks.Count - 1, index + VisibleRows());
                        break;

                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        AnsiConsole.Clear();
                        return index;

                    case ConsoleKey.Escape:
                    case ConsoleKey.Q:
                        AnsiConsole.Clear();
                        return -1;
                }
            }
        }
        finally
        {
            AnsiConsole.Cursor.Show();
        }
    }

    private static int VisibleRows()
    {
        var h = 12;
        try { h = Math.Clamp(Console.WindowHeight - 8, 6, 20); }
        catch { }
        return h;
    }

    private static void Draw(IReadOnlyList<string> tracks, int index)
    {
        var window = VisibleRows();
        var start = Math.Max(0, index - window / 2);
        start = Math.Min(start, Math.Max(0, tracks.Count - window));
        var end = Math.Min(tracks.Count, start + window);

        AnsiConsole.Clear();

        AnsiConsole.MarkupLine("[white]ВЫБОР КОМПОЗИЦИИ[/]");
        AnsiConsole.MarkupLine("[grey]↑ ↓  перемещение    Enter  воспроизвести    Esc  отмена[/]");
        AnsiConsole.Write(new Rule { Style = Theme.DimStyle });

        if (start > 0)
            AnsiConsole.MarkupLine("[grey]  · · ·[/]");

        for (var i = start; i < end; i++)
        {
            var name = Path.GetFileName(tracks[i]);
            var num = (i + 1).ToString().PadLeft(3);

            if (i == index)
                AnsiConsole.MarkupLine($"[black on white] ▶ {num}  {Markup.Escape(name)} [/]");
            else
                AnsiConsole.MarkupLine($"[grey]   {num}  {Markup.Escape(name)}[/]");
        }

        if (end < tracks.Count)
            AnsiConsole.MarkupLine("[grey]  · · ·[/]");

        AnsiConsole.Write(new Rule { Style = Theme.DimStyle });
        AnsiConsole.MarkupLine($"[grey]{index + 1} / {tracks.Count}[/]");
    }
}