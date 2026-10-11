using Spectre.Console;

namespace MusicCli.Ui;

public static class Splash
{
    private const string Logo = """
 ███╗   ███╗██╗   ██╗███████╗██╗ ██████╗
 ████╗ ████║██║   ██║██╔════╝██║██╔════╝
 ██╔████╔██║██║   ██║███████╗██║██║
 ██║╚██╔╝██║██║   ██║╚════██║██║██║
 ██║ ╚═╝ ██║╚██████╔╝███████║██║╚██████╗
 ╚═╝     ╚═╝ ╚═════╝ ╚══════╝╚═╝ ╚═════╝
""";

    public static void Show()
    {
        AnsiConsole.Clear();
        AnsiConsole.Cursor.Hide();

        var lines = Logo.Replace("\r", "").Split('\n');

        foreach (var line in lines)
        {
            AnsiConsole.MarkupLine($"[white]{Markup.Escape(line)}[/]");
            Thread.Sleep(45);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]                 C L I   P L A Y E R[/]");
        AnsiConsole.MarkupLine("[grey]                 ·  monochrome  ·[/]");
        AnsiConsole.WriteLine();

        DrawEqualizer();
        BootSequence();

        AnsiConsole.Cursor.Show();
        Thread.Sleep(250);
        AnsiConsole.Clear();
    }

    private static void DrawEqualizer()
    {
        var bars = new[] { 2, 5, 3, 7, 4, 8, 3, 6, 2, 5, 7, 3 };
        var frames = 8;

        for (var f = 0; f < frames; f++)
        {
            var visual = string.Join(" ", bars.Select(h =>
            {
                var height = 1 + (h + f * 2) % 8;
                return height switch
                {
                    >= 7 => "[white]█[/]",
                    >= 5 => "[silver]▓[/]",
                    >= 3 => "[grey]▒[/]",
                    _ => "[grey23]░[/]"
                };
            }));

            AnsiConsole.Markup($"\r                 {visual}   ");
            Thread.Sleep(90);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
    }

    private static void BootSequence()
    {
        var steps = new[]
        {
            "инициализация звукового движка",
            "сканирование библиотеки",
            "подключение клавиатуры",
            "готово к работе"
        };

        foreach (var step in steps)
        {
            AnsiConsole.Markup($"[grey]  > [/][silver]{Markup.Escape(step)}[/]");
            Thread.Sleep(180);
            AnsiConsole.MarkupLine("  [white]ok[/]");
        }
    }
}