using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetwork
{
    public class SimpleFile : IDisposable
    {
        private bool disposedValue;

        public FileStream Stream { get; }
        public string FullPath => Stream.Name;
        public string Name => Path.GetFileNameWithoutExtension(FullPath);
        public string Extension => Path.GetExtension(FullPath);

        internal SimpleFile(FileStream stream) => Stream = stream;

        public void Delete()
        {
            Stream.Close();
            File.Delete(FullPath);
        }

        public SimpleFile CopyToPath(string NewPath, string Name = null, bool OverwriteFile = false)
        {
            bool? val = null;

            if (Directory.Exists(NewPath))
                val = true;
            else if (File.Exists(NewPath))
                val = false;

            if (val == null) throw new IOException($"No such directory \"{NewPath}\"");
            else if (val == true)
            {
                if (Name != null)
                {
                    if (val == true)
                        NewPath += $@"\{Name}.{Extension}";
                }
                else
                    NewPath += $@"\{this.Name}{Extension}";
            }

            if (OverwriteFile)
            {
                using (FileStream fs = new FileStream(NewPath, FileMode.Create))
                {
                    Stream.CopyTo(fs);
                    fs.Flush();
                    return new SimpleFile(fs);
                }
            }
            else
            {
                using (FileStream fs = new FileStream(NewPath, FileMode.CreateNew))
                {
                    Stream.CopyTo(fs);
                    fs.Flush();
                    return new SimpleFile(fs);
                }
            }
        }

        public SimpleFile MoveToPath(string NewPath, string Name = null, bool OverwriteFile = false)
        {
            var file = CopyToPath(NewPath, Name, OverwriteFile);
            Delete();
            return file;
        }

        //public async Task DeleteAsync()
        //{
        //    Stream.Close();
        //    await Task.Run(() => File.Delete(FullPath));
        //}

        //public async Task<SimpleFile> CopyToPathAsync(string NewPath, string Name = null, bool OverwriteFile = false)
        //{
        //    bool? val = null;

        //    if (Directory.Exists(NewPath))
        //        val = true;
        //    else if (File.Exists(NewPath))
        //        val = false;

        //    if (val == null) throw new IOException($"No such directory \"{NewPath}\"");
        //    else if (val == true)
        //    {
        //        if (Name != null)
        //        {
        //            if (val == true)
        //                NewPath += $@"\{Name}.{Extension}";
        //        }
        //        else
        //            NewPath += $@"\{this.Name}{Extension}";
        //    }

        //    if (OverwriteFile)
        //    {
        //        using (FileStream fs = new FileStream(NewPath, FileMode.Create))
        //        {
        //            await Stream.CopyToAsync(fs);
        //            fs.Flush();
        //            return new SimpleFile(fs);
        //        }
        //    }
        //    else
        //    {
        //        using (FileStream fs = new FileStream(NewPath, FileMode.CreateNew))
        //        {
        //            await Stream.CopyToAsync(fs);
        //            fs.Flush();
        //            return new SimpleFile(fs);
        //        }
        //    }
        //}

        //public async Task<SimpleFile> MoveToPathAsync(string NewPath, string Name = null, bool OverwriteFile = false)
        //{
        //    var stream = await CopyToPathAsync(NewPath, Name, OverwriteFile);
        //    Delete();
        //    return stream;
        //}

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                Stream.Dispose();
                disposedValue = true;
            }
        }

        ~SimpleFile()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public static implicit operator FileStream(SimpleFile f) => f.Stream;
    }
}
