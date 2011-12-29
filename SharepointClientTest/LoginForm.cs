using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.SharePoint.Client;
using Kerwal.SharepointOnline;

namespace SharepointClientTest
{
    public partial class LoginForm : System.Windows.Forms.Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            // attempt to login with user's credentials
            using (ClientContextCreator contextCreator = new ClientContextCreator(Properties.Settings.Default.SiteURL, this.textBox1.Text, this.textBox2.Text))
            {
                StatusForm statusForm = new StatusForm();
                // rig the cancel button
                statusForm.button1.Click += new EventHandler(delegate(object s, EventArgs a)
                    {
                        // cancel the login process
                        contextCreator.CancelLogin();
                    });
                ClientContext clientContext = contextCreator.Login();
                // if it fails then re-prompt user
                if (clientContext == null)
                {
                    statusForm.Close();
                    this.Enabled = true;
                    return;
                }
                // if it succeeds then open main window and pass authenticated Context to it
                MessageBox.Show("Logged in!");
                // if checkbox is set then save username and password before opening window
            }
        }
    }
}
