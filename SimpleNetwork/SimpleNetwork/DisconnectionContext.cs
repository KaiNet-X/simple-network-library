namespace SimpleNetwork
{
    public class DisconnectionContext
    {
        public DisconnectionType type = DisconnectionType.CLOSE_CONNECTION;

        public DisconnectionContext() { }
        public DisconnectionContext(DisconnectionType type) => this.type = type;

        public enum DisconnectionType
        {
            CLOSE_CONNECTION,
            REMOVE,
            FORCIBLE
        }
    }
}
