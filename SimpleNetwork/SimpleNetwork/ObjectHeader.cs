using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleNetwork
{
    internal class ObjectHeader
    {
        [JsonProperty] public readonly string UniqueTypeIdentifyer = "0xFFDD6969";
        [JsonProperty] public readonly string Type;
        [JsonProperty] public readonly long Length;
        [JsonProperty] public readonly bool IsFile;
        [JsonProperty] public readonly long FileLength;

        //[JsonConstructor]
        internal ObjectHeader(string Type, long Length, bool IsFile = false, long FileLength = 0)
        {
            this.Type = Type;
            this.Length = Length;
            this.IsFile = IsFile;
            this.FileLength = FileLength;
        }

        private ObjectHeader()
        {

        }

        internal static byte[] AddHeadder(byte[] Data, Type t)
        {
            List<byte> full = new List<byte>(Data);
            full.InsertRange(0, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new ObjectHeader(t.Name, Data.Length))));

            return full.ToArray();
        }

        internal static byte[] AddHeadderFile(byte[] Data, string Extension, long Length)
        {
            List<byte> full = new List<byte>(Data);
            full.InsertRange(0, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new ObjectHeader(Extension, Data.Length, true, Length))));

            return full.ToArray();
        }

        internal static ObjectHeader GetHeader(byte[] Packet)
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
            string head = Encoding.UTF8.GetString(HeaderBytes.ToArray());
            return JsonConvert.DeserializeObject<ObjectHeader>(head);
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

        internal static List<byte[]> GetObjects(ref byte[] bytes, out List<ObjectHeader> headers)
        {
            List<byte[]> Objects = new List<byte[]>();
            headers = new List<ObjectHeader>();

            byte[] Full = bytes;

            byte[] searchBytes = Encoding.UTF8.GetBytes("{\"UniqueTypeIdentifyer\":\"0xFFDD6969\"");

            byte[] Partial = new byte[0];

            //bool HasHeader = Utilities.ByteArrayContains(Full, searchBytes);

            int HeaderIndex = Utilities.IndexInByteArray(Full, searchBytes);

            if (HeaderIndex == -1) 
                return new List<byte[]>(new List<byte[]> { bytes });

            while (HeaderIndex > -1)
            {
                //int HeaderIndex = Utilities.IndexInByteArray(Full, searchBytes);

                List<byte> full = new List<byte>(Full);

                byte[] NewObj;

                if (HeaderIndex > 0)
                {
                    Partial = full.GetRange(0, HeaderIndex).ToArray();
                    full.RemoveRange(0, HeaderIndex);
                    Full = full.ToArray();
                    HeaderIndex = Utilities.IndexInByteArray(Full, searchBytes);
                    continue;
                }

                int EndOfObject = Utilities.IndexInByteArray(Full, searchBytes, 1);

                if (EndOfObject != -1)
                {
                    NewObj = new List<byte>(Full).GetRange(0, EndOfObject).ToArray();
                }
                else
                {
                    NewObj = Full;
                }

                try
                {
                    ObjectHeader h = GetHeader(Full);
                    headers.Add(h);
                }
                catch (JsonReaderException)
                {
                    if (Partial.Length > 0) Objects.Add(Partial);
                    bytes = Full;
                    return Objects;
                }
                catch (JsonSerializationException)
                {
                    if (Partial.Length > 0) Objects.Add(Partial);
                    bytes = Full;
                    return Objects;
                }

                Objects.Add(RemoveHeader(NewObj));

                if(EndOfObject != -1)
                    Full = new List<byte>(Full).GetRange(EndOfObject, Full.Length - EndOfObject).ToArray();
                else
                    Full = new byte[] { 0 };

                HeaderIndex = Utilities.IndexInByteArray(Full, searchBytes);
            }

            if (Partial.Length > 0) Objects.Add(Partial);
            bytes = null;
            return Objects;
        }
    }
}