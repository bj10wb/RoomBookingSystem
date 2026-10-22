using System;
using System.Collections.Generic;
using System.Linq;
using UniversityRoomBookingSystem.Models;
using UniversityRoomBookingSystem.Repositories;

namespace UniversityRoomBookingSystem.Services
{
    public class BookingService
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IBookingRepository _bookingRepository;

        public BookingService(IRoomRepository roomRepository, IBookingRepository bookingRepository)
        {
            _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
            _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
        }

        public (bool Success, Booking? Booking, string? Error) TryCreateBooking(string roomId, TeachingSession session)
        {
            if (string.IsNullOrWhiteSpace(roomId)) return (false, null, "RoomId must be provided.");
            if (session is null) return (false, null, "Teaching session must be provided.");
            if (string.IsNullOrWhiteSpace(session.ModuleName)) return (false, null, "Module name must be provided.");
            if (session.StudentCount <= 0) return (false, null, "Student count must be a positive integer.");
            if (session.Start >= session.End) return (false, null, "Session start must be earlier than end.");

            var room = _roomRepository.GetById(roomId);
            if (room is null) return (false, null, "Room not found.");

            if (session.StudentCount > room.Capacity)
            {
                return (false, null, $"Room capacity ({room.Capacity}) is insufficient for {session.StudentCount} students.");
            }

            var missingEquipment = session.RequiredEquipment.Where(equipment => !room.Equipment.Contains(equipment)).ToArray();
            if (missingEquipment.Length > 0)
            {
                var equipmentNames = string.Join(", ", missingEquipment);
                return (false, null, $"Room is missing required equipment: {equipmentNames}.");
            }

            if (room is ComputerLab lab)
            {
                if (session.StudentCount > lab.Workstations)
                {
                    return (false, null, $"Computer lab has only {lab.Workstations} workstations for {session.StudentCount} students.");
                }
            }

            if (HasOverlap(roomId, session.Start, session.End))
            {
                return (false, null, "Requested session overlaps an existing booking for this room.");
            }

            var booking = new Booking(roomId, session);
            _bookingRepository.Add(booking);
            return (true, booking, null);
        }
        public List<Room> FindSuitableRooms(int studentCount, IEnumerable<Equipment>? requiredEquipment, DateTime? start = null, DateTime? end = null)
        {
            var suitableRooms = new List<Room>();
            var required = requiredEquipment?.ToList();

            foreach (var room in _roomRepository.GetAll())
            {
                if (!room.IsSuitable(studentCount, required)) continue;
                if (start.HasValue && end.HasValue && HasOverlap(room.Id, start.Value, end.Value)) continue;
                suitableRooms.Add(room);
            }

            var roomArray = suitableRooms.ToArray();
            ManualAlgorithms.QuickSortByCapacity(roomArray, 0, roomArray.Length - 1, ascending: true);
            return roomArray.ToList();
        }

        private bool HasOverlap(string roomId, DateTime start, DateTime end)
        {
            foreach (var existingBooking in _bookingRepository.GetByRoomId(roomId))
            {
                var existingSession = existingBooking.Session;
                if (start < existingSession.End && existingSession.Start < end)
                {
                    return true;
                }
            }
            return false;
        }
    }
}