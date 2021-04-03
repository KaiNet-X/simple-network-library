using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MessageClient
{
    public partial class Login : Form
    {
        ClientForm Main;
        bool finished = false;

        public Login(Form mainForm)
        {
            InitializeComponent();
            Main = mainForm as ClientForm;
            Main.client.OnConnectError += (SocketException Ex, uint Reps) =>
            {
                Invoke(new MethodInvoker(AddressBox.Clear));
                MessageBox.Show(Ex.Message);
                return false;
            };
            SubmitButton.Enabled = false;
        }

        private void Login_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!finished)
                Application.Exit();
        }

        private void AddressBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                IPAddress.Parse(AddressBox.Text);
                if (UsernameBox.Text.Length > 0)
                    SubmitButton.Enabled = true;
                else
                    SubmitButton.Enabled = false;
            }
            catch (FormatException) { SubmitButton.Enabled = false; }
        }

        private void UsernameBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                IPAddress.Parse(AddressBox.Text);
                if (UsernameBox.Text.Length > 0)
                    SubmitButton.Enabled = true;
                else
                    SubmitButton.Enabled = false;
            }
            catch (FormatException) { SubmitButton.Enabled = false; }
        }

        private async void SubmitButton_Click(object sender, EventArgs e)
        {
            progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.MarqueeAnimationSpeed = 50;

            await Main.client.ConnectAsync(IPAddress.Parse(AddressBox.Text), 12233).ConfigureAwait(true);
            if (Main.client.IsConnected)
            {
                Main.Username = UsernameBox.Text;
                finished = true;
                Close();
            }
            else
            {
                progressBar1.MarqueeAnimationSpeed = 0;
                progressBar1.Style = ProgressBarStyle.Blocks;
                progressBar1.Value = progressBar1.Minimum;
            }
        }
    }
}
