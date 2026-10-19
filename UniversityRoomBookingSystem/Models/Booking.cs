using System;
using System.Text.Json.Serialization;

namespace UniversityRoomBookingSystem.Models
{
    public class Booking
    {
        public Booking()
        {
            BookingId = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }

        public Booking(string roomId, TeachingSession session)
        {
            BookingId = Guid.NewGuid();
            RoomId = roomId ?? string.Empty;
            Session = session ?? throw new ArgumentNullException(nameof(session));
            CreatedAt = DateTime.UtcNow;
        }

        [JsonInclude]
        public Guid BookingId { get; private set; }

        [JsonInclude]
        public string RoomId { get; private set; } = string.Empty;

        [JsonInclude]
        public TeachingSession Session { get; private set; } = new TeachingSession();

        [JsonInclude]
        public DateTime CreatedAt { get; private set; }
    }
}
