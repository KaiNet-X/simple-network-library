namespace SimpleNetwork
{
    public class DisconnectionContext
    {
        public DisconnectionType type = DisconnectionType.CLOSE_CONNECTION;

        public enum DisconnectionType
        {
            CLOSE_CONNECTION,
            REMOVE,
            FORCIBLE
        }
    }
}
