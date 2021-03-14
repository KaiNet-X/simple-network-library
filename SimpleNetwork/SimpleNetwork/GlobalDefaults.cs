using System.IO;

namespace SimpleNetwork
{
    public static class GlobalDefaults
    {
        public static ForcibleDisconnectBehavior ForcibleDisconnectMode = ForcibleDisconnectBehavior.REMOVE;
        public static EncodingType ObjectEncodingType = EncodingType.JSON;
        public static bool RunServerClientsOnOneThread = true;
        public static bool OverwritePreviousOfTypeInQueue = false;
        public static bool UseEncryption = true;
        public static MessagePack.MessagePackSerializerOptions Serializer = MessagePack.Resolvers.ContractlessStandardResolver.Options;
        public static string FilePath = "\\Temp\\";
        public static string FileDirectory { get; set; } = Directory.GetCurrentDirectory() + "\\SentFiles";

        public static void ClearSentFiles()
        {
            Directory.Delete(FileDirectory, true);
        }

        public enum ForcibleDisconnectBehavior
        {
            REMOVE,
            KEEP
        }

        public enum EncodingType
        {
            JSON,
            MESSAGE_PACK
        }
    }
}
