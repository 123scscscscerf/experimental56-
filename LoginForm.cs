using System.Windows.Forms;

namespace Experimental56;

public sealed class LoginForm : Form
{
    private readonly TextBox _login = new() { PlaceholderText = "Login", Width = 260 };
    private readonly TextBox _password = new() { PlaceholderText = "Password", Width = 260, UseSystemPasswordChar = true };
    public SessionUser? AuthenticatedUser { get; private set; }

    public LoginForm()
    {
        Text = "Login";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(380, 260);
        BackColor = AppTheme.Bg;
        Font = AppTheme.Font;

        var card = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30), BackColor = AppTheme.Bg };
        var inner = new Panel { Dock = DockStyle.Top, Height = 160, BackColor = AppTheme.Panel, Padding = new Padding(20) };
        var loginBtn = AppTheme.FlatButton("Войти");
        loginBtn.Width = 120;
        loginBtn.Click += (_, _) => DoLogin();

        var fl = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        fl.Controls.Add(new Label { Text = "Платформа тестирования", AutoSize = true, Font = new Font(AppTheme.Font.FontFamily, 13, FontStyle.Bold) });
        fl.Controls.Add(_login); fl.Controls.Add(_password); fl.Controls.Add(loginBtn);
        inner.Controls.Add(fl); card.Controls.Add(inner); Controls.Add(card);
        AcceptButton = loginBtn;
    }

    private void DoLogin()
    {
        var user = AuthService.Login(_login.Text.Trim(), _password.Text);
        if (user is null)
        {
            MessageBox.Show("Неверные данные");
            return;
        }
        AuthenticatedUser = user;
        DialogResult = DialogResult.OK;
        Close();
    }
}
