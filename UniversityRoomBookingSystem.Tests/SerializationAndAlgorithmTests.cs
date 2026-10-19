using System;
using System.Collections.Generic;
using System.Text.Json;
using UniversityRoomBookingSystem.Models;
using UniversityRoomBookingSystem.Services;
using Xunit;

namespace UniversityRoomBookingSystem.Tests
{
    public class SerializationAndAlgorithmTests
    {
        private readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        [Fact]
        public void ComputerLab_Workstations_SurvivesJsonRoundTrip()
        {
            Room lab = new ComputerLab("LAB01", "Advanced Computing Lab", 30, workstations: 28, new[] { Equipment.Computers, Equipment.Projector });

            string json = JsonSerializer.Serialize(lab, _options);
            Room? deserialized = JsonSerializer.Deserialize<Room>(json, _options);

            Assert.NotNull(deserialized);
            var typedLab = Assert.IsType<ComputerLab>(deserialized);
            Assert.Equal("LAB01", typedLab.Id);
            Assert.Equal(30, typedLab.Capacity);
            Assert.Equal(28, typedLab.Workstations);
            Assert.Contains(Equipment.Computers, typedLab.Equipment);
        }

        [Fact]
        public void PolymorphicList_AllDerivedTypes_DeserializeCorrectly()
        {
            var list = new List<Room>
            {
                new StandardRoom("SR1", "Standard Room 1", 20),
                new ComputerLab("CL1", "Lab 1", 25, 20),
                new LectureTheatre("LT1", "Theatre 1", 100)
            };

            string json = JsonSerializer.Serialize(list, _options);
            var deserializedList = JsonSerializer.Deserialize<List<Room>>(json, _options);

            Assert.NotNull(deserializedList);
            Assert.Equal(3, deserializedList.Count);
            Assert.IsType<StandardRoom>(deserializedList[0]);
            Assert.IsType<ComputerLab>(deserializedList[1]);
            Assert.IsType<LectureTheatre>(deserializedList[2]);
        }

        [Fact]
        public void Room_InvalidArguments_ThrowsExpectedExceptions()
        {
            Assert.Throws<ArgumentException>(() => new StandardRoom("ID1", "   ", 20));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StandardRoom("ID1", "Valid Name", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ComputerLab("ID2", "Lab Name", 20, workstations: -5));
        }

        [Fact]
        public void ManualLinearSearch_FindsMatchingSubstrings()
        {
            var rooms = new List<Room>
            {
                new StandardRoom("R1", "Physics Lab Prep", 20),
                new StandardRoom("R2", "Chemistry Hall", 40),
                new StandardRoom("R3", "Physics Lecture Room", 60)
            };

            var matches = ManualAlgorithms.SearchRoomsByName(rooms, "physics");
            Assert.Equal(2, matches.Count);
        }

        [Fact]
        public void ManualQuickSort_OrdersByCapacityAscending()
        {
            var rooms = new Room[]
            {
                new StandardRoom("R1", "Medium", 50),
                new StandardRoom("R2", "Large", 120),
                new StandardRoom("R3", "Small", 10),
                new StandardRoom("R4", "Tiny", 5)
            };

            ManualAlgorithms.QuickSortByCapacity(rooms, 0, rooms.Length - 1, ascending: true);

            Assert.Equal(5, rooms[0].Capacity);
            Assert.Equal(10, rooms[1].Capacity);
            Assert.Equal(50, rooms[2].Capacity);
            Assert.Equal(120, rooms[3].Capacity);
        }
    }
}