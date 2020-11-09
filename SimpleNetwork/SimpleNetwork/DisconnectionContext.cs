namespace SimpleNetwork
{
    public class DisconnectionContext
    {
        //public static DisconnectionContext Default => new DisconnectionContext { type = DisconnectionType.CloseConnection };

        public DisconnectionType type = DisconnectionType.CLOSE_CONNECTION;

        public enum DisconnectionType
        {
            CLOSE_CONNECTION,
            REMOVE,
            FORCIBLE
        }
    }
}
