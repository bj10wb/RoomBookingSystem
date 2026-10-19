using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UniversityRoomBookingSystem.Models
{
    public class TeachingSession
    {
        [JsonInclude]
        public string ModuleName { get; private set; }

        [JsonInclude]
        public int StudentCount { get; private set; }

        [JsonInclude]
        public HashSet<Equipment> RequiredEquipment { get; private set; }

        [JsonInclude]
        public DateTime Start { get; private set; }

        [JsonInclude]
        public DateTime End { get; private set; }

        public TeachingSession()
        {
            ModuleName = string.Empty;
            StudentCount = 0;
            RequiredEquipment = new HashSet<Equipment>();
        }

        public TeachingSession(string moduleName, int studentCount, IEnumerable<Equipment>? requiredEquipment, DateTime start, DateTime end)
        {
            ModuleName = moduleName ?? string.Empty;
            StudentCount = studentCount;
            RequiredEquipment = requiredEquipment is null ? new HashSet<Equipment>() : new HashSet<Equipment>(requiredEquipment);
            Start = start;
            End = end;
        }
    }
}