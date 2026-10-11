using MusicCli.Player;
using MusicCli.Ui;
using Spectre.Console;

namespace MusicCli;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "MUSIC CLI";
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Clear();

        Splash.Show();

        var settings = AppSettings.Load();
        var startFolder = ResolveStartFolder(args, settings);

        using var player = new MusicPlayer();
        var library = new TrackLibrary(startFolder);
        RestoreSelection(library, settings);
        settings.Remember(library.Folder, library.SelectedTrack);

        using var cts = new CancellationTokenSource();
        _ = AdvanceLoop(library, player, settings, cts.Token);

        AnsiConsole.Clear();
        Screen.DrawHeader(library, player);
        Screen.DrawHelp();
        Screen.Dim("Введите команду и нажмите Enter. Для завершения работы воспользуйтесь командой exit.");
        if (!string.IsNullOrWhiteSpace(settings.Folder))
            Screen.Dim($"Восстановлена последняя папка: {library.Folder}");
        AnsiConsole.WriteLine();

        while (true)
        {
            player.RefreshState();

            var raw = AnsiConsole.Prompt(
                new TextPrompt<string>("[white]>[/]")
                    .PromptStyle("silver")
                    .AllowEmpty());

            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = SplitArgs(line);
            var cmd = parts[0].ToLowerInvariant();
            var rest = parts.Count > 1 ? parts[1] : "";

            try
            {
                switch (cmd)
                {
                    case "play":
                    case "p":
                        await PlayAsync(library, player, settings, rest);
                        break;

                    case "pause":
                        await PauseAsync(player);
                        break;

                    case "resume":
                        await ResumeAsync(player);
                        break;

                    case "stop":
                        await player.StopAsync();
                        Screen.Info("Воспроизведение остановлено.");
                        break;

                    case "select":
                    case "s":
                        await SelectTrackAsync(library, player, settings);
                        break;

                    case "cd":
                        ChangeDirectory(library, settings, rest);
                        break;

                    case "list":
                    case "ls":
                        Screen.RenderTrackList(library);
                        break;

                    case "help":
                    case "?":
                        Screen.DrawHelp();
                        break;

                    case "clear":
                    case "cls":
                        player.RefreshState();
                        AnsiConsole.Clear();
                        Screen.DrawHeader(library, player);
                        break;

                    case "status":
                        player.RefreshState();
                        Screen.DrawHeader(library, player);
                        break;

                    case "exit":
                    case "quit":
                    case "q":
                        settings.Remember(library.Folder, library.SelectedTrack);
                        cts.Cancel();
                        await player.StopAsync();
                        Screen.Dim("Работа программы завершена. До свидания.");
                        return 0;

                    default:
                        Screen.Error($"Команда «{cmd}» неизвестна. Воспользуйтесь help для списка доступных команд.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Screen.Error(ex.Message);
            }
        }
    }

    private static string ResolveStartFolder(string[] args, AppSettings settings)
    {
        if (args.Length > 0 && Directory.Exists(args[0]))
            return Path.GetFullPath(args[0]);

        if (!string.IsNullOrWhiteSpace(settings.Folder) && Directory.Exists(settings.Folder))
            return Path.GetFullPath(settings.Folder);

        return Directory.GetCurrentDirectory();
    }

    private static void RestoreSelection(TrackLibrary library, AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SelectedFile))
            return;

        if (File.Exists(settings.SelectedFile))
            library.SelectByPath(settings.SelectedFile);
        else
            library.SelectByFileName(Path.GetFileName(settings.SelectedFile));
    }

    private static async Task AdvanceLoop(TrackLibrary library, MusicPlayer player, AppSettings settings, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(400, token);
                if (!player.Continuous)
                    continue;

                player.RefreshState();
                if (player.State != PlaybackState.Stopped)
                    continue;

                if (!library.SelectNext() || library.SelectedTrack is null)
                    continue;

                await player.PlayAsync(library.SelectedTrack);
                settings.Remember(library.Folder, library.SelectedTrack);
                Screen.Info($"Следующая композиция: {library.SelectedName}");
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
    }

    private static async Task PlayAsync(TrackLibrary library, MusicPlayer player, AppSettings settings, string arg)
    {
        if (!string.IsNullOrWhiteSpace(arg))
        {
            var path = ResolveTrack(library, arg);
            if (path is null)
            {
                Screen.Error("Указанная композиция не найдена.");
                return;
            }

            library.SelectByPath(path);
            await player.PlayAsync(path);
            settings.Remember(library.Folder, library.SelectedTrack);
            Screen.Info($"Воспроизводится: {Path.GetFileName(path)}");
            return;
        }

        if (player.State == PlaybackState.Paused)
        {
            await player.ResumeAsync();
            Screen.Info($"Воспроизведение продолжено: {player.CurrentName}");
            return;
        }

        if (library.SelectedTrack is null)
        {
            Screen.Error("Сначала выберите композицию командой select.");
            return;
        }

        await player.PlayAsync(library.SelectedTrack);
        settings.Remember(library.Folder, library.SelectedTrack);
        Screen.Info($"Воспроизводится: {library.SelectedName}");
    }

    private static async Task PauseAsync(MusicPlayer player)
    {
        if (player.State != PlaybackState.Playing)
        {
            Screen.Dim("В данный момент ничего не воспроизводится.");
            return;
        }

        await player.PauseAsync();
        Screen.Info($"Воспроизведение приостановлено: {player.CurrentName}");
    }

    private static async Task ResumeAsync(MusicPlayer player)
    {
        if (player.State != PlaybackState.Paused)
        {
            Screen.Dim("Нет приостановленной композиции для продолжения.");
            return;
        }

        await player.ResumeAsync();
        Screen.Info($"Воспроизведение продолжено: {player.CurrentName}");
    }

    private static async Task SelectTrackAsync(TrackLibrary library, MusicPlayer player, AppSettings settings)
    {
        library.Rescan();

        if (library.Tracks.Count == 0)
        {
            Screen.Error($"В выбранной папке нет музыкальных файлов: {library.Folder}");
            Screen.Dim("Поддерживаются форматы: mp3, wav, ogg, flac, m4a, aac, wma.");
            return;
        }

        var index = TrackPicker.Run(library);
        if (index < 0)
        {
            Screen.Dim("Выбор композиции отменён.");
            return;
        }

        library.Select(index);
        settings.Remember(library.Folder, library.SelectedTrack);

        if (library.SelectedTrack is null)
            return;

        await player.PlayAsync(library.SelectedTrack);
        Screen.Info($"Выбрана и воспроизводится: {library.SelectedName}");
        Screen.Dim("Для паузы введите команду pause.");
    }

    private static void ChangeDirectory(TrackLibrary library, AppSettings settings, string arg)
    {
        string? target = string.IsNullOrWhiteSpace(arg) ? AskFolder(library.Folder) : ExpandPath(arg);

        if (string.IsNullOrWhiteSpace(target))
        {
            Screen.Dim("Папка не изменена.");
            return;
        }

        if (!library.ChangeFolder(target))
        {
            Screen.Error($"Указанная папка не найдена: {target}");
            return;
        }

        settings.Remember(library.Folder, library.SelectedTrack);
        Screen.Info($"Выбрана папка: {library.Folder}");
        Screen.Dim($"Найдено композиций: {library.Tracks.Count}");
        if (library.Tracks.Count > 0)
            Screen.RenderTrackList(library);
    }

    private static string? AskFolder(string current)
    {
        var input = AnsiConsole.Prompt(
            new TextPrompt<string>("[grey]Укажите путь к папке[/] [white]>[/]")
                .PromptStyle("silver")
                .DefaultValue(current)
                .AllowEmpty());

        return string.IsNullOrWhiteSpace(input) ? null : ExpandPath(input);
    }

    private static string ExpandPath(string path)
    {
        path = path.Trim().Trim('"');
        if (path.StartsWith("~"))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = Path.Combine(home, path.TrimStart('~').TrimStart('/', '\\'));
        }

        return Path.GetFullPath(path);
    }

    private static string? ResolveTrack(TrackLibrary library, string arg)
    {
        arg = arg.Trim().Trim('"');

        if (int.TryParse(arg, out var number) && number >= 1 && number <= library.Tracks.Count)
            return library.Tracks[number - 1];

        var exact = library.Tracks.FirstOrDefault(t =>
            string.Equals(Path.GetFileName(t), arg, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
            return exact;

        var partial = library.Tracks.FirstOrDefault(t =>
            Path.GetFileName(t).Contains(arg, StringComparison.OrdinalIgnoreCase));
        if (partial is not null)
            return partial;

        var full = ExpandPath(arg);
        return File.Exists(full) ? full : null;
    }

    private static List<string> SplitArgs(string line)
    {
        var space = line.IndexOf(' ');
        if (space < 0)
            return [line];

        return [line[..space], line[(space + 1)..].Trim()];
    }
}