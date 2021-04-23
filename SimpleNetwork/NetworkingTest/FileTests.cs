using SimpleNetwork;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Xunit;

namespace NetworkingTest
{
    public class FileTests
    {
        int files = 0;
        bool condition = false;

        [Fact]
        public void ServerSendsFiles()
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
            s.SendFileToAll(f);

            Assert.True(TestDefaults.WaitForCondition(() => files == 1));
        }

        [Fact]
        public void ClientSendsFile()
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
            c.SendFile(f);

            Assert.True(TestDefaults.WaitForCondition(() => files == 1));
        }

        [Fact]
        public void CanCopyFile() => FileTest(CopiesFile);

        [Fact]
        public void CanMoveFile() => FileTest(MovesFile);

        [Fact]
        public void CanDeleteFile() => FileTest(DeletesFile);

        [Fact]
        public void CanOverwriteCopyFile() => FileTest(CopiesAndOverwries);

        [Fact]
        public void CanOverriteMoveFile() => FileTest(MovesAndOverrites);

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


        private void FileTest(Server.RecievedFile t)
        {
            TestDefaults.SetGlobalDefaults();

            Server s = TestDefaults.GetServer();
            s.OnClientRecieveFile = t;
            s.StartServer();

            Client c = new Client();
            c.Connect(IPAddress.Loopback, 9090);

            string path = Directory.GetCurrentDirectory();
            string f = Directory.GetFiles(path)[0];
            c.SendFile(f);

            Assert.True(TestDefaults.WaitForCondition(() => condition));
        }

        private void DeletesFile(SimpleFile file, ConnectionInfo inf)
        {
            file.Delete();

            if (Deleted())
                condition = true;
        }
        private void CopiesFile(SimpleFile file, ConnectionInfo inf)
        {
            string path = Directory.GetCurrentDirectory() + @"\Dir";

            DirectoryInfo info = Directory.CreateDirectory(path);
            info.Attributes = info.Attributes & ~FileAttributes.ReadOnly;
            try
            {
                var fi = file.CopyToPath(path);

                if (Copied(path, fi.FullPath) && !Deleted()) 
                    condition = true;

                fi.Delete();
                fi.Dispose();
                file.Delete();
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }
        private void MovesFile(SimpleFile file, ConnectionInfo inf)
        {
            string path = Directory.GetCurrentDirectory() + @"\Dir";
            Directory.CreateDirectory(path);
            var fi = file.MoveToPath(path);

            if (Copied(path, fi.FullPath) && Deleted())
                condition = true;

            fi.Delete();
            fi.Dispose();

            Directory.Delete(path, true);
        }
        private void CopiesAndOverwries(SimpleFile file, ConnectionInfo inf)
        {
            string path = Directory.GetCurrentDirectory() + @"\Dir";

            Directory.CreateDirectory(path);
            file.CopyToPath(path, OverwriteFile: true).Dispose();
            var fi = file.CopyToPath(path, OverwriteFile:true);

            if (Copied(path, fi.FullPath) && !Deleted())
                condition = true;

            fi.Delete();
            fi.Dispose();
            file.Delete();

            Directory.Delete(path, true);
        }
        private void MovesAndOverrites(SimpleFile file, ConnectionInfo inf)
        {
            string path = Directory.GetCurrentDirectory() + @"\Dir";
            Directory.CreateDirectory(path);
            file.CopyToPath(path).Dispose();
            var fi = file.MoveToPath(path, OverwriteFile:true);

            if (Copied(path, fi.FullPath) && Deleted())
                condition = true;

            fi.Delete();
            fi.Dispose();

            Directory.Delete(path, true);
        }

        private bool Deleted() => Directory.GetFiles(GlobalDefaults.FileDirectory).Length == 0;
        private bool Copied(string path, string name) => Directory.GetFiles(path)[0] == name;
    }
}
