using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UniversityRoomBookingSystem.Models;

namespace UniversityRoomBookingSystem.Services
{
    public class JsonStorageService
    {
        private readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public (bool Success, string Message) SaveRooms(string filePath, IEnumerable<Room> rooms)
        {
            try
            {
                string json = JsonSerializer.Serialize(rooms, _options);
                string tempFile = filePath + ".tmp";
                File.WriteAllText(tempFile, json);
                File.Move(tempFile, filePath, overwrite: true);
                return (true, "Rooms successfully saved.");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to save rooms to '{filePath}': {ex.Message}");
            }
        }

        public (bool Success, List<Room>? Rooms, string Message) LoadRooms(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return (false, null, $"File '{filePath}' does not exist.");
            }

            try
            {
                string json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return (false, null, "Rooms file is empty.");
                }

                var loadedRooms = JsonSerializer.Deserialize<List<Room>>(json, _options);
                if (loadedRooms is null)
                {
                    return (false, null, "Rooms file contained no valid room data.");
                }

                for (int i = 0; i < loadedRooms.Count; i++)
                {
                    var room = loadedRooms[i];
                    if (room is null || string.IsNullOrWhiteSpace(room.Id) || string.IsNullOrWhiteSpace(room.Name) || room.Capacity <= 0)
                    {
                        return (false, null, $"Room at index {i} has invalid core invariants.");
                    }
                    if (room.Equipment is null)
                    {
                        return (false, null, $"Room '{room.Id}' has no equipment list.");
                    }
                    if (room is ComputerLab lab && lab.Workstations <= 0)
                    {
                        return (false, null, $"Computer lab '{lab.Id}' has invalid workstation count ({lab.Workstations}).");
                    }
                }

                return (true, loadedRooms, "Rooms successfully loaded.");
            }
            catch (JsonException ex)
            {
                return (false, null, $"Malformed JSON in rooms file: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, null, $"I/O error loading rooms from '{filePath}': {ex.Message}");
            }
        }

        public (bool Success, string Message) SaveBookings(string filePath, IEnumerable<Booking> bookings)
        {
            try
            {
                string json = JsonSerializer.Serialize(bookings, _options);
                string tempFile = filePath + ".tmp";
                File.WriteAllText(tempFile, json);
                File.Move(tempFile, filePath, overwrite: true);
                return (true, "Bookings successfully saved.");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to save bookings to '{filePath}': {ex.Message}");
            }
        }

        public (bool Success, List<Booking>? Bookings, string Message) LoadBookings(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return (false, null, $"File '{filePath}' does not exist.");
            }

            try
            {
                string json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return (false, null, "Bookings file is empty.");
                }

                var loadedBookings = JsonSerializer.Deserialize<List<Booking>>(json, _options);
                if (loadedBookings is null)
                {
                    return (false, null, "Bookings file contained no valid booking data.");
                }

                for (int i = 0; i < loadedBookings.Count; i++)
                {
                    var booking = loadedBookings[i];
                    if (booking is null || booking.BookingId == Guid.Empty || string.IsNullOrWhiteSpace(booking.RoomId) || booking.Session is null)
                    {
                        return (false, null, $"Booking at index {i} has invalid core invariants.");
                    }

                    if (booking.Session.Start >= booking.Session.End || booking.Session.StudentCount <= 0)
                    {
                        return (false, null, $"Booking '{booking.BookingId}' contains an invalid session window or student count.");
                    }
                    if (booking.Session.RequiredEquipment is null)
                    {
                        return (false, null, $"Booking '{booking.BookingId}' has no required-equipment list.");
                    }
                }

                return (true, loadedBookings, "Bookings successfully loaded.");
            }
            catch (JsonException ex)
            {
                return (false, null, $"Malformed JSON in bookings file: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, null, $"I/O error loading bookings from '{filePath}': {ex.Message}");
            }
        }
    }
}