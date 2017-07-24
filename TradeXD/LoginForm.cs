using System;
using System.Windows.Forms;

namespace TradeXD
{
    public partial class LoginForm : Form
    {
        private readonly OverlayForm _parent;

        public LoginForm(OverlayForm parent)
        {
            InitializeComponent();
            _parent = parent;
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            loginButton.Enabled = false;
            if (emailTextbox.Text.Length > 0)
            {
                if (passwordTextbox.Text.Length > 0)
                {
                    if (_parent.Login(emailTextbox.Text, passwordTextbox.Text))
                        PoEHelper.SaveCredentials(emailTextbox.Text, passwordTextbox.Text);
                    passwordTextbox.Text = "";
                    Dispose();
                }
                //else
                //{
                //    if (_parent.Login(emailTextbox.Text))
                //    {
                //        PoEHelper.SaveCredentials(emailTextbox.Text);
                //    }
                //    passwordTextbox.Text = "";
                //    Dispose();
                //}
            }
            else
            {
                passwordTextbox.Text = "";
            }
            loginButton.Enabled = true;
        }
    }
}