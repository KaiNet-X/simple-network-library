using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace SimpleNetwork
{
    internal class PacketHeader
    {
        [JsonProperty]
        public readonly string UniqueTypeIdentifyer = "0xFFDD6969";
        [JsonProperty]
        public readonly int Index;
        [JsonProperty]
        public readonly bool FinalPacket;
        [JsonProperty]
        public readonly string Type;

        //[JsonConstructor]
        internal PacketHeader(int Index, bool FinalPacket, string Type)
        {
            this.Index = Index;
            this.FinalPacket = FinalPacket;
            this.Type = Type;
        }
        private PacketHeader()
        {

        }
        internal static byte[] AddHeadders(byte[] Data, Type t)
        {
            int packetCount = Data.Length / 65536 + (Data.Length % 65536 > 0 ? 1 : 0);
            int headersLength = 0;

            List<string> headersJson = new List<string>();

            for (int i = 0; i < packetCount; i++)
            {
                string s = JsonConvert.SerializeObject(new PacketHeader(i, i == packetCount - 1, t.FullName));
                //string s = ObjectParser.Serialize(new PacketHeader(i, i == packetCount - 1, t.FullName));
                headersLength += s.Length;
                headersJson.Add(s);
            }
            int newCount = (Data.Length + headersLength) / 65536 + ((Data.Length + headersLength) % 65536 > 0 ? 1 : 0);
            
            if (newCount > packetCount)
            {
                packetCount = newCount;
                headersJson.RemoveAt(headersJson.Count - 1);
                headersJson.Add(JsonConvert.SerializeObject(new PacketHeader((byte)headersJson.Count, false, t.FullName)));
                headersJson.Add(JsonConvert.SerializeObject(new PacketHeader((byte)headersJson.Count, true, t.FullName)));
                //headersJson.Add(ObjectParser.Serialize(new PacketHeader((byte)headersJson.Count, false, t.FullName)));
                //headersJson.Add(ObjectParser.Serialize(new PacketHeader((byte)headersJson.Count, false, t.FullName)));
            }
            List<byte> FullObject = new List<byte>(Data);
            for (int i = 0; i < packetCount; i++)
            {
                byte[] header = Encoding.UTF8.GetBytes(headersJson[i]);
                FullObject.InsertRange(i * 65536, header);
            }
            //for (int i = 0; i < packetCount; i++)
            //{
            //    string s = Encoding.UTF8.GetString(FullObject.GetRange(i * 65536, 110).ToArray());
            //}
            return FullObject.ToArray();
        }

        internal static PacketHeader GetHeader(byte[] Packet)
        {
            List<byte> HeaderBytes = new List<byte>();

            for (int i = 0; i < Packet.Length; i++)
            {
                HeaderBytes.Add(Packet[i]);
                if (Packet[i] == 125)
                {
                    break;
                }
            }
            string test = Encoding.UTF8.GetString(Packet);
            string test2 = Encoding.UTF8.GetString(HeaderBytes.ToArray());
            return JsonConvert.DeserializeObject<PacketHeader>(Encoding.UTF8.GetString(HeaderBytes.ToArray()));
            //return ObjectParser.Deserialize<PacketHeader>(Encoding.UTF8.GetString(HeaderBytes.ToArray()));
        }

        internal static byte[] RemoveHeader(byte[] Packet)
        {
            int HeaderLength = 0;

            for (int i = 0; i < Packet.Length; i++)
            {
                HeaderLength++;
                if (Packet[i] == 125)
                {
                    break;
                }
            }

            List<byte> Headerless = new List<byte>(Packet);
            Headerless.RemoveRange(0, HeaderLength);

            return Headerless.ToArray();
        }

        internal static List<byte[]> GetObjects1(byte[] bytes, out List<PacketHeader> headers)
        {
            List<byte[]> Objects = new List<byte[]>();
            headers = new List<PacketHeader>();
            string Stringified = Encoding.UTF8.GetString(bytes);

            while (Stringified.Contains("{\"UniqueTypeIdentifyer\":\"0xFFDD6969\""))
            {
                int EndOfObject = Stringified.IndexOf("{\"UniqueTypeIdentifyer\":\"0xFFDD6969\"", 1);

                byte[] newObj;

                if (EndOfObject != -1)
                {
                    newObj = new List<byte>(bytes).GetRange(bytes.Length - Stringified.Length, bytes.Length - Stringified.Length + EndOfObject).ToArray();
                }
                else
                {
                    newObj = new List<byte>(bytes).GetRange(bytes.Length - Stringified.Length, bytes.Length - (bytes.Length - Stringified.Length)).ToArray();
                }
                
                headers.Add(GetHeader(newObj));
                newObj = RemoveHeader(newObj);
                Objects.Add(newObj);

                if (EndOfObject != -1)
                    Stringified = Stringified.Substring(EndOfObject);
                else
                    Stringified = "";
            }

            return Objects;
        }

        internal static List<byte[]> GetObjects(byte[] bytes, out List<PacketHeader> headers)
        {
            List<byte[]> Objects = new List<byte[]>();
            headers = new List<PacketHeader>();

            byte[] Full = bytes;

            byte[] searchBytes = Encoding.UTF8.GetBytes("{\"UniqueTypeIdentifyer\":\"0xFFDD6969\"");
            //byte[] searchBytes = Encoding.UTF8.GetBytes("{UniqueTypeIdentifyer:\"0xFFDD6969\"");
            
            string test = Encoding.UTF8.GetString(bytes);

            while (Utilities.ByteArrayContains(Full, searchBytes))
            {
                int EndOfObject = Utilities.IndexInByteArray(Full, searchBytes, 1);

                byte[] newObj;

                if (EndOfObject != -1)
                {
                    newObj = new List<byte>(Full).GetRange(0, EndOfObject).ToArray();
                }
                else
                {
                    newObj = Full;
                }

                headers.Add(GetHeader(newObj));
                newObj = RemoveHeader(newObj);
                Objects.Add(newObj);

                if (EndOfObject != -1)
                    Full = new List<byte>(Full).GetRange(EndOfObject, Full.Length - EndOfObject).ToArray();//Stringified.Substring(EndOfObject);
                else
                    Full = new byte[] { 0 };
            }

            return Objects;
        }
    }
}
