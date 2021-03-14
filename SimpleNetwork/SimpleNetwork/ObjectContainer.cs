using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetwork
{
    internal class ObjectContainer
    {
        [JsonProperty(Order = -2)] internal const string UniqueStartIdentifier = "0xFFDD6969";
        [JsonProperty] internal readonly string Type;
        [JsonProperty] internal readonly FileInfo fileInfo;
        [JsonIgnore] internal byte[] content { get; private set; }

        internal ObjectContainer(string Type)
        {
            this.Type = Type;
        }

        internal ObjectContainer(FileInfo Inf)
        {
            fileInfo = Inf;
        }

        internal ObjectContainer()
        {

        }

        internal static byte[] Encapsulate(byte[] Bytes, string Type)
        {
            List<byte> full = new List<byte>();

            ObjectContainer c = new ObjectContainer(Type);
            End e = End.Default();

            string head = JsonConvert.SerializeObject(c);
            string tail = JsonConvert.SerializeObject(e);

            full.AddRange(Encoding.UTF8.GetBytes(head));
            full.AddRange(Bytes);
            full.AddRange(Encoding.UTF8.GetBytes(tail));

            return full.ToArray();
        }

        internal static byte[] Encapsulate(byte[] Bytes, string Name, long Length)
        {
            List<byte> full = new List<byte>();

            ObjectContainer c = new ObjectContainer(new FileInfo(Name, Length));
            End e = End.Default();

            string head = JsonConvert.SerializeObject(c);
            string tail = JsonConvert.SerializeObject(e);

            full.AddRange(Encoding.UTF8.GetBytes(head));
            full.AddRange(Bytes);
            full.AddRange(Encoding.UTF8.GetBytes(tail));

            return full.ToArray();
        }

        internal static ObjectContainer[] GetPackets(ref byte[] Bytes)
        {
            List<byte> Check = new List<byte>(Bytes);
            List<ObjectContainer> Objects = new List<ObjectContainer>();

            byte[] SearchBytes1 = Encoding.UTF8.GetBytes("{\"UniqueStartIdentifier\":\"0xFFDD6969\"");
            byte[] SearchBytes2 = Encoding.UTF8.GetBytes("{\"UniqueEndIdentifier\":\"0o42006888\"}");

            int sIndex;
            int eIndex;

            byte[] Partial = null;

            while ((sIndex = Utilities.IndexInByteArray(Check.ToArray(), SearchBytes1)) > -1)
            {
                eIndex = Utilities.IndexInByteArray(Check.ToArray(), SearchBytes2);

                if (eIndex == -1)
                {
                    Bytes = Check.ToArray();
                    if (Bytes.Length == 0) Bytes = null;
                    return Objects.ToArray();
                }

                if (sIndex > 0)
                {
                    Partial = Check.GetRange(0, sIndex + 1).ToArray();
                    Check.RemoveRange(0, sIndex + 1);
                    Bytes = Check.ToArray();
                }

                ObjectContainer cont = GetHeader(Bytes);

                Check = new List<byte>(RemoveHeader(Bytes));

                byte[] content = Check.GetRange(0, (eIndex = Utilities.IndexInByteArray(Check.ToArray(), SearchBytes2))).ToArray();

                Check.RemoveRange(0, eIndex + SearchBytes2.Length);
                Bytes = Check.ToArray();

                cont.content = content;

                Objects.Add(cont);
            }
            if (Bytes.Length == 0) Bytes = null;
            return Objects.ToArray();
        }

        private static ObjectContainer GetHeader(byte[] Packet)
        {
            List<byte> HeaderBytes = new List<byte>();
            int depth = 0;

            for (int i = 0; i < Packet.Length; i++)
            {
                HeaderBytes.Add(Packet[i]);
                if (Packet[i] == 123)
                {
                    if (i > 0)
                        depth++;
                }
                if (Packet[i] == 125)
                {
                    depth--;
                }
                if (depth < 0) break;
            }
            string head = Encoding.UTF8.GetString(HeaderBytes.ToArray());
            return JsonConvert.DeserializeObject<ObjectContainer>(head);
        }

        private static byte[] RemoveHeader(byte[] Packet)
        {
            int HeaderLength = 0;
            int depth = 0;

            for (int i = 0; i < Packet.Length; i++)
            {
                HeaderLength++;
                if (Packet[i] == 123)
                {
                    if (i > 0)
                        depth++;
                }
                if (Packet[i] == 125)
                {
                    depth--;
                }
                if (depth < 0) break;
            }

            List<byte> Headerless = new List<byte>(Packet);
            Headerless.RemoveRange(0, HeaderLength);

            return Headerless.ToArray();
        }

        internal class End
        {
            [JsonProperty] public const string UniqueEndIdentifier = "0o42006888";

            internal static End Default() => new End();
        }

        internal class FileInfo
        {
            [JsonProperty] public readonly string Name;
            [JsonProperty] public readonly long Length;

            internal FileInfo(string Name, long Length)
            {
                this.Name = Name;
                this.Length = Length;
            }

            internal FileInfo()
            {

            }
        }
    }
}
