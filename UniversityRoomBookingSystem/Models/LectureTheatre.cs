using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UniversityRoomBookingSystem.Models
{
    public class LectureTheatre : Room
    {
        public LectureTheatre()
        {
        }

        public LectureTheatre(string id, string name, int capacity, IEnumerable<Equipment>? equipment = null)
            : base(id, name, capacity, equipment)
        {
        }

        [JsonIgnore]
        public override string RoomType => "Lecture Theatre";
    }
}