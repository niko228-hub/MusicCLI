using Spectre.Console;

namespace MusicCli.Ui;

public static class Theme
{
    public static readonly Color Ink = Color.White;
    public static readonly Color Dim = Color.Grey;
    public static readonly Color Mid = Color.Silver;
    public static readonly Color Accent = Color.Grey93;

    public static string InkText(string text) => $"[white]{Markup.Escape(text)}[/]";
    public static string DimText(string text) => $"[grey]{Markup.Escape(text)}[/]";
    public static string MidText(string text) => $"[silver]{Markup.Escape(text)}[/]";

    public static Style InkStyle => new(Ink);
    public static Style DimStyle => new(Dim);
    public static Style MidStyle => new(Mid);
    public static Style AccentStyle => new(Accent, decoration: Decoration.Bold);
}