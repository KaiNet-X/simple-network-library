namespace SimpleNetwork
{
    public static class GlobalDefaults
    {
        public static ForcibleDisconnectBehavior ForcibleDisconnectMode = ForcibleDisconnectBehavior.REMOVE;
        public static EncodingType ObjectEncodingType = EncodingType.JSON;
        public static bool RunServerClientsOnOneThread = false;
        public static MessagePack.MessagePackSerializerOptions Serializer = MessagePack.Resolvers.ContractlessStandardResolver.Options;

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
