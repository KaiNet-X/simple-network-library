using System;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using SimpleNetwork;
using System.Diagnostics;

namespace FormsTest
{
    public partial class Form1 : Form
    {
        Server s;
        Stopwatch sw = new Stopwatch();
        public Form1()
        {
            InitializeComponent();
            GlobalDefaults.ObjectEncodingType = GlobalDefaults.EncodingType.MESSAGE_PACK;
            s = new Server(IPAddress.Parse("192.168.0.17"), 9573, 1);
            s.RestartAutomatically = true;
            s.StartServer();
            s.OnClientConnect += S_OnClientConnect;
        }

        private void S_OnClientConnect(ConnectionInfo inf)
        {
            pictureBox1.Image = GetBitmap(s.WaitForPullFromClient<color[,]>((ushort)(s.ClientCount - 1)));
            MessageBox.Show(((float)sw.ElapsedMilliseconds / 1000).ToString());
            sw.Stop();
            sw.Reset();
            MessageBox.Show(s.WaitForPullFromClient<string>((ushort)(s.ClientCount - 1)));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            s.ClearDisconnectedClients();
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.ShowDialog();
            Bitmap bmp = (Bitmap)Image.FromFile(ofd.FileName);
            Client c = new Client();
            c.Connect(IPAddress.Parse("192.168.0.17"), 9573);
            color[,] colorMen = GetPixels(bmp);
            sw.Start();
            c.SendObject<color[,]>(colorMen);
            c.SendObject("HALOOOOO");
            c.Disconnect();
        }

        private color[,] GetPixels(Bitmap b)
        {
            color[,] colorMen = new color[b.Width, b.Height];

            for (int x = 0; x < colorMen.GetLength(0); x++)
            {
                for (int y = 0; y < colorMen.GetLength(1); y++)
                {
                    Color c = b.GetPixel(x, y);
                    colorMen[x, y] = new color { r = c.R, b = c.B, g = c.G };
                }
            }

            return colorMen;
        }

        private Bitmap GetBitmap(color[,] colors)
        {
            Bitmap bmp = new Bitmap(colors.GetLength(0), colors.GetLength(1));

            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    bmp.SetPixel(x, y, colors[x, y].getColor());
                }
            }

            return bmp;
        }

        public class color
        {
            public byte r;
            public byte g;
            public byte b;

            public Color getColor() => Color.FromArgb(r, g, b);
        }
    }
}
