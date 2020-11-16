namespace SimpleNetwork
{
    public static class GlobalDefaults
    {
        public static ForcibleDisconnectBehavior ForcibleDisconnectMode = ForcibleDisconnectBehavior.REMOVE;
        public static EncodingType ObjectEncodingType = EncodingType.MESSAGE_PACK;

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
