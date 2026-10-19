using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UniversityRoomBookingSystem.Models
{
    public class ComputerLab : Room
    {
        [JsonInclude]
        public int Workstations { get; private set; }

        public ComputerLab() : base()
        {
        }

        public ComputerLab(string id, string name, int capacity, int workstations, IEnumerable<Equipment>? equipment = null)
            : base(id, name, capacity, equipment)
        {
            if (workstations <= 0)
                throw new ArgumentOutOfRangeException(nameof(workstations), "Workstations count must be a positive integer.");

            Workstations = workstations;
        }

        public override bool IsSuitable(int studentCount, IEnumerable<Equipment>? requiredEquipment)
        {
            if (!base.IsSuitable(studentCount, requiredEquipment)) return false;
            if (studentCount > Workstations) return false;
            return true;
        }

        [JsonIgnore]
        public override string RoomType => "Computer Lab";
    }
}