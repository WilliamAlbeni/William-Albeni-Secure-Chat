namespace SecureChat.DAL.Entities
{
    public enum DeliveryStatus
    {
        SentToServer = 1,       // ont tick
        DeliveredToDevice = 2,  // two ticks
        ReadByUser = 3          // Blue ticks
    }
}