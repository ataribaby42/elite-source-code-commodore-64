using System.Text;

namespace EliteSaveEditor;

internal sealed record MenuItem(string Label, string? Description = null, bool BlankLineAfter = false);

internal static class ConsoleUi
{
    public static int? Select(
        string title,
        IReadOnlyList<MenuItem> items,
        string? summary = null,
        int selected = 0,
        bool allowCancel = true)
    {
        if (items.Count == 0)
        {
            return null;
        }

        selected = Math.Clamp(selected, 0, items.Count - 1);
        while (true)
        {
            var height = SafeWindowHeight();
            var summaryLines = string.IsNullOrEmpty(summary) ? 0 : summary.Split('\n').Length + 1;
            var visibleCount = Math.Max(4, height - summaryLines - 7);
            var first = Math.Clamp(selected - visibleCount / 2, 0, Math.Max(0, items.Count - visibleCount));
            var last = Math.Min(items.Count, first + visibleCount);

            Console.Clear();
            WriteHeading(title);
            if (!string.IsNullOrWhiteSpace(summary))
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(summary);
                Console.ResetColor();
                Console.WriteLine();
            }

            if (first > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  ... {first} more above ...");
                Console.ResetColor();
            }

            for (var index = first; index < last; index++)
            {
                var active = index == selected;
                if (active)
                {
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.ForegroundColor = ConsoleColor.White;
                }

                var prefix = active ? "> " : "  ";
                Console.Write(prefix);
                Console.Write(items[index].Label);
                Console.WriteLine();
                Console.ResetColor();
                if (items[index].BlankLineAfter)
                {
                    Console.WriteLine();
                }
            }

            if (last < items.Count)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  ... {items.Count - last} more below ...");
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(allowCancel
                ? "Up/Down: move   Enter: select   Esc: back"
                : "Up/Down: move   Enter: select");
            Console.ResetColor();

            var key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    selected = selected == 0 ? items.Count - 1 : selected - 1;
                    break;
                case ConsoleKey.DownArrow:
                    selected = selected == items.Count - 1 ? 0 : selected + 1;
                    break;
                case ConsoleKey.PageUp:
                    selected = Math.Max(0, selected - visibleCount);
                    break;
                case ConsoleKey.PageDown:
                    selected = Math.Min(items.Count - 1, selected + visibleCount);
                    break;
                case ConsoleKey.Home:
                    selected = 0;
                    break;
                case ConsoleKey.End:
                    selected = items.Count - 1;
                    break;
                case ConsoleKey.Enter:
                    return selected;
                case ConsoleKey.Escape when allowCancel:
                    return null;
            }
        }
    }

    public static string? ReadText(
        string title,
        string prompt,
        string initialValue = "",
        int maximumLength = 260,
        bool allowEmpty = false)
    {
        var value = new StringBuilder(initialValue);
        while (true)
        {
            Console.Clear();
            WriteHeading(title);
            Console.WriteLine(prompt);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(value.ToString());
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("Enter: accept   Esc: cancel   Backspace: erase   Ctrl+A: clear");
            Console.ResetColor();

            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape)
            {
                return null;
            }

            if (key.Key == ConsoleKey.Enter)
            {
                var result = value.ToString();
                if (allowEmpty || !string.IsNullOrWhiteSpace(result))
                {
                    return result;
                }

                Console.Beep();
                continue;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (value.Length > 0)
                {
                    value.Length--;
                }

                continue;
            }

            if (key.Key == ConsoleKey.A && key.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                value.Clear();
                continue;
            }

            if (!char.IsControl(key.KeyChar) && value.Length < maximumLength)
            {
                value.Append(key.KeyChar);
            }
        }
    }

    public static bool Confirm(string title, string question, bool defaultYes = false)
    {
        var result = Select(
            title,
            [new MenuItem("Yes"), new MenuItem("No")],
            question,
            defaultYes ? 0 : 1);
        return result == 0;
    }

    public static void Message(string title, params string[] lines)
    {
        Console.Clear();
        WriteHeading(title);
        foreach (var line in lines)
        {
            Console.WriteLine(line);
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("Press any key to continue.");
        Console.ResetColor();
        Console.ReadKey(true);
    }

    public static void WriteHeading(string title)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(title);
        Console.WriteLine(new string('=', Math.Min(title.Length, 78)));
        Console.ResetColor();
        Console.WriteLine();
    }

    private static int SafeWindowHeight()
    {
        try
        {
            return Math.Max(Console.WindowHeight, 15);
        }
        catch
        {
            return 25;
        }
    }
}
