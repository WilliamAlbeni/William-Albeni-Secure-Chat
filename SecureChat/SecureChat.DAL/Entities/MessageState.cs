namespace SecureChat.DAL.Entities
{
    public enum MessageState // for MITM
    {
        Pending = 0,     // Waiting for Attacker
        Intercepted = 1, // intercepted by attacker and it's been modifying
        Delivered = 2    // Deliverd to receiver
    }
}