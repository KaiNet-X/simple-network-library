using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SimpleNetwork;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace FormsTest
{
    public partial class Form1 : Form
    {
        Server s;
        public Form1()
        {
            InitializeComponent();
            s = new Server(IPAddress.Parse("192.168.0.17"), 9573, 5);
            s.RestartAutomatically = true;            
            s.StartServer();
            s.OnClientConnect += S_OnClientConnect;
        }

        private void S_OnClientConnect(ConnectionInfo inf)
        {
            pictureBox1.Image = GetBitmap(s.WaitForPullFromClient<Color[,]>((byte)(s.ClientCount - 1)));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.ShowDialog();
            Bitmap bmp = (Bitmap)Image.FromFile(ofd.FileName);
            Client c = new Client();
            c.Connect(IPAddress.Parse("192.168.0.17"), 9573);
            Color[,] colorMen = GetPixels(bmp);
            c.SendObject<Color[,]>(colorMen);
            c.Disconnect();
        }

        private Color[,] GetPixels(Bitmap b)
        {
            Color[,] colorMen = new Color[b.Width, b.Height];

            for (int x = 0; x < colorMen.GetLength(0); x++)
            {
                for (int y = 0; y < colorMen.GetLength(1); y++)
                {
                    colorMen[x, y] = b.GetPixel(x, y);
                }
            }

            return colorMen;
        }

        private Bitmap GetBitmap(Color[,] colors)
        {
            Bitmap bmp = new Bitmap(colors.GetLength(0), colors.GetLength(1));

            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    bmp.SetPixel(x, y, colors[x, y]);
                }
            }

            return bmp;
        }
    }
}
