using System.Diagnostics;
using Minio;
using MinIoCli;
using MinIoCli.Models;
using Newtonsoft.Json;
using Spectre.Console;

public static class Logic
{
    private static string AppDir => Path.Combine(Directory.GetCurrentDirectory(), "cli");
    private static string ConfigFileName => "connections.json";
    private static string ConfigFilePath => Path.Combine(AppDir, ConfigFileName);
    private static Connection? _currentConnection;
    private static MinioClient? _client;

    #region [ Connection ]

    public static List<Connection>? ReadConnectionConfig()
    {
        if (!File.Exists(ConfigFilePath)) return null;
        var content = File.ReadAllText(ConfigFilePath);
        return JsonConvert.DeserializeObject<List<Connection>>(content);
    }

    public static void SetConnectionConfig(List<Connection> data)
    {
        var content = JsonConvert.SerializeObject(data);
        File.WriteAllText(ConfigFilePath, content);
    }

    public static void SetCurrentConnection(Connection connection)
    {
        _client = new MinioClient(connection.Host, connection.AccessKey, connection.SecretKey);
        if (connection.Secure) _client.WithSSL();
        _currentConnection = connection;
    }

    public static bool HasAnyConfig()
    {
        var data = ReadConnectionConfig();
        return data is { Count: > 0 };
    }

    public static void AddConnection()
    {
        var form = Ui.ConfigurationForm();
        Ui.Status("Save Connection", (context) =>
        {
            context.Status("Generating Model.");
            var connection = new Connection(Guid.NewGuid().ToString(), form.host, form.accessKey, form.secretKey,
                form.secure, form.connectionName);
            if (!Directory.Exists(AppDir))
            {
                context.Status("Application DIR Creating.");
                Directory.CreateDirectory(AppDir);
            }

            context.Status("Reading Data.");
            var data = ReadConnectionConfig() ?? new List<Connection>();
            data.Add(connection);
            context.Status("Writing Data.");
            SetConnectionConfig(data);
            SetCurrentConnection(connection);
        });
    }

    public static void ListConnections()
    {
        var data = ReadConnectionConfig() ?? new List<Connection>();
        Ui.Table((table) =>
        {
            table.Title = new TableTitle("Connections");
            table.AddColumn(new TableColumn("Alias").Centered());
            table.AddColumn(new TableColumn("Host").Centered());
            table.AddColumn("Access Key");
            table.AddColumn("Secret Key");
            table.AddColumn("HTTPS");
            foreach (var item in data)
            {
                table.AddRow
                (
                    new Markup($"[bold]{item.Alias}[/]"),
                    new Markup($"[bold blue]{item.Host}[/]"),
                    new Markup(item.AccessKey),
                    new Markup(item.SecretKey),
                    new Markup(item.Secure.ToString())
                );
            }

            table.Border(TableBorder.Rounded);
        });
    }

    public static void ChangeConnection()
    {
        var data = ReadConnectionConfig();
        if (data is null) throw new Exception("Connections are empty.");
        var selection = new SelectionPrompt<string>().Title("Select a [bold]connection[/]")
            .AddChoices(data.Select(x => x.Host));
        var result = AnsiConsole.Prompt(selection);
        var item = data.FirstOrDefault(x => x.Host == result)!;
        SetCurrentConnection(item);
    }

    #endregion

    #region [ MinIO ]

    private static void CheckMinIoConnection()
    {
        AnsiConsole.Status()
            .Start("Connection Checking...", ctx =>
            {
                try
                {
                    Task.Delay(1000).Wait();
                    var watch = new Stopwatch();
                    watch.Start();
                    _client!.ListBucketsAsync().Wait();
                    watch.Stop();
                    ctx.Status($"[green]Connection Successfully.[/] Duration {watch.ElapsedMilliseconds} ms.");
                    GC.SuppressFinalize(watch);
                }
                catch (Exception e)
                {
                    ctx.Status($"Connection has been [bold red]failed[/]. Message: '{e.Message}'.");
                    Task.Delay(5000).Wait();
                }
            });
    }

    private static void CheckMinIoConnectionAlive()
    {
        var interval = Ui.Ask<int>("Check Interval [bold](seconds)[/]:");
        AnsiConsole.Status()
            .Start("Connection Checking...", ctx =>
            {
                while (true)
                {
                    ctx.Spinner(Spinner.Known.Circle);
                    var watch = new Stopwatch();
                    try
                    {
                        ctx.Status($"Connection Starting...");
                        watch.Start();
                        _client!.ListBucketsAsync().Wait();
                        watch.Stop();
                        ctx.Status($"[green]Connection Successfully.[/] Duration {watch.ElapsedMilliseconds} ms.");
                    }
                    catch (Exception e)
                    {
                        watch.Stop();
                        ctx.Status($"Connection has been [bold red]failed[/]. Duration {watch.ElapsedMilliseconds} ms. Message: '[italic]{e.Message}[/]'.");
                    }

                    GC.SuppressFinalize(watch);
                    ctx.Spinner(Spinner.Known.Clock);
                    Task.Delay(interval * 1000).Wait();
                }
            });
    }

    #endregion

    public static void Start()
    {
        selection:
        var select = Ui.Menu(_currentConnection!.Alias, _currentConnection.Host);
        try
        {
            switch (select)
            {
                case ActionType.AddConnection:
                    AddConnection();
                    break;
                case ActionType.ShowConnection:
                    ListConnections();
                    break;
                case ActionType.ChangeCurrentConnection:
                    ChangeConnection();
                    break;
                case ActionType.CheckMinioConnection:
                    CheckMinIoConnection();
                    break;
                case ActionType.CheckMinioConnectionAsInterval:
                    CheckMinIoConnectionAlive();
                    break;
                default:
                    Ui.Text("[red]Unsupported[/]");
                    break;
            }
        }
        catch (Exception e)
        {
            AnsiConsole.WriteException(e);
        }

        goto selection;
    }
}