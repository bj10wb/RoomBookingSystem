using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace UniversityRoomBookingSystem.Models
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(StandardRoom), "standard")]
    [JsonDerivedType(typeof(ComputerLab), "computerlab")]
    [JsonDerivedType(typeof(LectureTheatre), "lecturetheatre")]
    public abstract class Room
    {
        [JsonInclude]
        public string Id { get; protected set; }

        [JsonInclude]
        public string Name { get; protected set; }

        [JsonInclude]
        public int Capacity { get; protected set; }

        [JsonInclude]
        public HashSet<Equipment> Equipment { get; protected set; }

        protected Room()
        {
            Id = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            Name = string.Empty;
            Capacity = 1;
            Equipment = new HashSet<Equipment>();
        }

        protected Room(string id, string name, int capacity, IEnumerable<Equipment>? equipment = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Room name cannot be null or whitespace.", nameof(name));

            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be a positive integer.");

            Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N")[..8].ToUpperInvariant() : id.Trim();
            Name = name.Trim();
            Capacity = capacity;
            Equipment = equipment is null ? new HashSet<Equipment>() : new HashSet<Equipment>(equipment);
        }

        [JsonIgnore]
        public virtual string RoomType => "Room";

        public bool HasEquipment(IEnumerable<Equipment>? required)
        {
            if (required is null) return true;
            return required.All(r => Equipment.Contains(r));
        }

        public virtual bool IsSuitable(int studentCount, IEnumerable<Equipment>? requiredEquipment)
        {
            if (studentCount <= 0) return false;
            if (studentCount > Capacity) return false;
            if (!HasEquipment(requiredEquipment)) return false;
            return true;
        }
    }
}