namespace MinIoCli;

public static class Program
{
    private static void Main(string[] args)
    {
        Ui.Intro();
        if (!Logic.HasAnyConfig())
        {
            Ui.Text("You have not any [bold yellow]Connection[/].");
            Logic.AddConnection();
        }
        else Logic.ChangeConnection();

        Logic.Start();
    }
}