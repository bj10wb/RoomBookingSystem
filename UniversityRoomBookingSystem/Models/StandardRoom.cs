using System.Text.Json.Serialization;

namespace UniversityRoomBookingSystem.Models
{
    public class StandardRoom : Room
    {
        public StandardRoom()
        {
        }

        public StandardRoom(string id, string name, int capacity, System.Collections.Generic.IEnumerable<Equipment>? equipment = null)
            : base(id, name, capacity, equipment)
        {
        }

        [JsonIgnore]
        public override string RoomType => "Standard Room";
    }
}