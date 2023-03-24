using MinIoCli;
using Spectre.Console;

public static class Ui
{
    private record MenuItem(string Group, List<ActionType> Actions);

    public static void Intro()
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(Fonts.Isometric3);
        writer.Flush();
        stream.Position = 0;
        var font = FigletFont.Load(stream);
        AnsiConsole.Write(
            new FigletText(font, "MIN IO")
                .Color(Color.Orange1));
        var titleRule = new Rule("[orange1]MinIO CLI[/]");
        AnsiConsole.Write(titleRule);
        AnsiConsole.WriteLine("\n");
    }

    public static (string host, string accessKey, string secretKey, bool secure, string connectionName)
        ConfigurationForm()
    {
        var host = AnsiConsole.Prompt(new TextPrompt<string>("HOST:"));
        var accessKey = AnsiConsole.Prompt(new TextPrompt<string>("Access Key:"));
        var secretKey = AnsiConsole.Prompt(new TextPrompt<string>("Secret Key:"));
        var secure = AnsiConsole.Prompt(
            new SelectionPrompt<bool>()
                .Title("Secure:")
                .AddChoices(new[]
                {
                    true,
                    false
                }));
        var connectionName = AnsiConsole.Prompt(new TextPrompt<string>("Give a Name:"));
        return (host, accessKey, secretKey, secure, connectionName);
    }

    public static T Ask<T>(string question) => AnsiConsole.Prompt(new TextPrompt<T>(question));

    public static ActionType Menu(string alias, string host)
    {
        var menu = new List<MenuItem>
        {
            new MenuItem("Connections",
                new List<ActionType>
                    { ActionType.AddConnection, ActionType.ShowConnection, ActionType.ChangeCurrentConnection }),
            new MenuItem("Operations",
                new List<ActionType> { ActionType.CheckMinioConnection, ActionType.CheckMinioConnectionAsInterval })
        };
        var selection = new SelectionPrompt<string>()
            .Title($"Current Connection : [bold][[{alias}]][/] [green]{host}[/]")
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to reveal more ext)[/]");

        foreach (var group in menu)
        {
            selection.AddChoiceGroup<string>(group.Group, group.Actions.Select(Enum.GetName).ToList()!);
        }

        var value = AnsiConsole.Prompt(selection);
        return Enum.Parse<ActionType>(value);
    }

    public static void Status(string title, Action<StatusContext> action)
    {
        AnsiConsole.Status()
            .Start(title, action);
    }

    public static void Text(string text) => AnsiConsole.MarkupLine(text);

    public static void Table(Action<Table> table)
    {
        var tableObject = new Table();
        table.Invoke(tableObject);
        AnsiConsole.Write(tableObject);
    }
}