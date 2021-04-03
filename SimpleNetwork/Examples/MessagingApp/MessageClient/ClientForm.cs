using System;
using System.Windows.Forms;
using SimpleNetwork;

namespace MessageClient
{
    public partial class ClientForm : Form
    {
        public Client client = new Client();
        public string Username;

        public ClientForm()
        {
            InitializeComponent();
            client.OnRecieveObject += OnObjectRecieve;
            client.OnDisconnect += OnDisconnect;
            Login lf = new Login(this);
            GlobalDefaults.ObjectEncodingType = GlobalDefaults.EncodingType.JSON;
            lf.ShowDialog();
        }

        private void OnDisconnect(DisconnectionContext ctx, ConnectionInfo inf)
        {
            Application.Restart();
        }

        private void OnObjectRecieve(object obj)
        {
            if (obj.GetType() == typeof(SendMessage))
            {
                SendMessage msg = obj as SendMessage;
                Invoke(new MethodInvoker(() => MessagingBox.AppendText($"{msg.Username}: {msg.Content} -----{msg.Time}\n")));
            }
        }

        private async void SubmitButton_Click(object sender, EventArgs e)
        {
            SendMessage msg = new SendMessage(Username, SendText.Text, DateTime.Now);
            SendText.Clear();
            await client.SendObjectAsync(msg);
        }

        private void SendText_TextChanged(object sender, EventArgs e)
        {
            SubmitButton.Enabled = SendText.TextLength > 0;
        }
    }
}
