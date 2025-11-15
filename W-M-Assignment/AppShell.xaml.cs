namespace Demo;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("AddPage", typeof(AddPage));
    }

    private void QuitClicked(object sender, EventArgs e)
    {
        // TODO
    }
}
