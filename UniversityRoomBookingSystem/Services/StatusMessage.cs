namespace UniversityRoomBookingSystem.Services
{
    public enum MessageLevel
    {
        Info,
        Success,
        Warning,
        Error
    }
    public record StatusMessage(MessageLevel Level, string Text);
}