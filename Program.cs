using System.Windows.Forms;

namespace Experimental56;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        AppTheme.ApplyGlobal();
        Database.Initialize();
        AppSettingsService.Initialize();
        AppSettingsService.ApplyUiFromSettings();
        SeedData.EnsureSeeded();

        while (true)
        {
            using var login = new LoginForm();
            if (login.ShowDialog() != DialogResult.OK || login.AuthenticatedUser is null) break;
            using var shell = new MainShellForm(login.AuthenticatedUser);
            shell.ShowDialog();
            if (!shell.ShouldRelogin) break;
        }
    }
}
