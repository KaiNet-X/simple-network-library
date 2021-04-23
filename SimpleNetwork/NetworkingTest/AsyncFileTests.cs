using SimpleNetwork;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NetworkingTest
{
    public class AsyncFileTests
    {
        int files = 0;
        bool condition = false;

        [Fact]
        public async void ServerSendsFilesAsync()
        {
            TestDefaults.SetGlobalDefaults();

            Server s = TestDefaults.GetServer();
            s.StartServer();

            Client c = new Client();
            c.OnFileRecieve += ClientFileReceive;
            c.Connect(IPAddress.Loopback, 9090);

            GlobalDefaults.ClearSentFiles();

            string path = Directory.GetCurrentDirectory();
            string f = Directory.GetFiles(path)[0];
            await s.SendFileToAllAsync(f);

            Assert.True(TestDefaults.WaitForCondition(() => files == 1));
        }

        [Fact]
        public async void ClientSendsFileAsync()
        {
            TestDefaults.SetGlobalDefaults();

            Server s = TestDefaults.GetServer();
            s.OnClientRecieveFile = ServerFileRecieve;
            s.StartServer();

            Client c = new Client();
            c.Connect(IPAddress.Loopback, 9090);

            GlobalDefaults.ClearSentFiles();

            string path = Directory.GetCurrentDirectory();
            string f = Directory.GetFiles(path)[0];
            await c.SendFileAsync(f);

            Assert.True(TestDefaults.WaitForCondition(() => files == 1));
        }

        private void ClientFileReceive(SimpleFile file)
        {
            files++;
            file.Delete();
        }

        private void ServerFileRecieve(SimpleFile file, ConnectionInfo inf)
        {
            files++;
            file.Delete();
        }
    }
}
