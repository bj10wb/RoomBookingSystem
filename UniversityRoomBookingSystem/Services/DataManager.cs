using System;
using System.Collections.Generic;
using System.IO;
using UniversityRoomBookingSystem.Models;
using UniversityRoomBookingSystem.Repositories;

namespace UniversityRoomBookingSystem.Services
{
    public class DataManager
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly JsonStorageService _storage;
        private readonly FileLogger _logger;

        public string RoomsFilePath { get; }
        public string BookingsFilePath { get; }

        public DataManager(
            IRoomRepository roomRepository,
            IBookingRepository bookingRepository,
            JsonStorageService storage,
            FileLogger logger,
            string roomsFilePath,
            string bookingsFilePath)
        {
            _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
            _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            RoomsFilePath = roomsFilePath ?? throw new ArgumentNullException(nameof(roomsFilePath));
            BookingsFilePath = bookingsFilePath ?? throw new ArgumentNullException(nameof(bookingsFilePath));
        }

        public List<StatusMessage> Initialize()
        {
            var messages = new List<StatusMessage>();

            if (!File.Exists(RoomsFilePath))
            {
                SeedDefaultRooms();
                if (File.Exists(BookingsFilePath))
                {
                    LoadBookingsInto(messages);
                }
                else
                {
                    SeedDefaultBookings();
                }
                messages.Add(new StatusMessage(MessageLevel.Info, "First run: default rooms and sample bookings created."));
                messages.AddRange(Save());
                return messages;
            }

            bool roomsLoaded = LoadRoomsInto(messages);
            bool bookingsLoaded = true;
            if (File.Exists(BookingsFilePath))
            {
                bookingsLoaded = LoadBookingsInto(messages);
            }

            if (!roomsLoaded)
            {
                BackupUnreadableFile(RoomsFilePath, messages);
                SeedDefaultRooms();
                messages.Add(new StatusMessage(MessageLevel.Warning, "Default rooms restored. The unreadable rooms file was backed up."));
            }

            if (!bookingsLoaded)
            {
                BackupUnreadableFile(BookingsFilePath, messages);
            }

            return messages;
        }

        public List<StatusMessage> Save()
        {
            var messages = new List<StatusMessage>();
            var roomResult = _storage.SaveRooms(RoomsFilePath, _roomRepository.GetAll());
            var bookingResult = _storage.SaveBookings(BookingsFilePath, _bookingRepository.GetAll());

            if (roomResult.Success && bookingResult.Success)
            {
                messages.Add(new StatusMessage(MessageLevel.Success, "Data saved to JSON files."));
                _logger.Log("State successfully saved to JSON.");
            }
            else
            {
                string problem = $"{roomResult.Message} | {bookingResult.Message}";
                messages.Add(new StatusMessage(MessageLevel.Error, $"Save problem: {problem}"));
                _logger.Log($"Save warning: {problem}", "ERROR");
            }
            return messages;
        }

        public List<StatusMessage> Reload()
        {
            var messages = new List<StatusMessage>();
            LoadRoomsInto(messages);
            LoadBookingsInto(messages);
            return messages;
        }

        private bool LoadRoomsInto(List<StatusMessage> messages)
        {
            var (success, rooms, message) = _storage.LoadRooms(RoomsFilePath);
            if (success && rooms != null)
            {
                _roomRepository.ReplaceAll(rooms);
                messages.Add(new StatusMessage(MessageLevel.Success, $"Loaded {rooms.Count} rooms."));
                return true;
            }

            messages.Add(new StatusMessage(MessageLevel.Warning, $"Rooms not loaded: {message} (keeping current in-memory rooms)."));
            _logger.Log($"Room load failed: {message}", "WARN");
            return false;
        }

        private bool LoadBookingsInto(List<StatusMessage> messages)
        {
            var (success, bookings, message) = _storage.LoadBookings(BookingsFilePath);
            if (success && bookings != null)
            {
                _bookingRepository.ReplaceAll(bookings);
                messages.Add(new StatusMessage(MessageLevel.Success, $"Loaded {bookings.Count} bookings."));
                return true;
            }

            messages.Add(new StatusMessage(MessageLevel.Warning, $"Bookings not loaded: {message} (keeping current in-memory bookings)."));
            _logger.Log($"Booking load failed: {message}", "WARN");
            return false;
        }

        private void SeedDefaultRooms()
        {
            _roomRepository.ReplaceAll(new Room[]
            {
                new StandardRoom("SR-101", "Seminar Room 101", 30, new[] { Equipment.Projector, Equipment.Whiteboard }),
                new StandardRoom("SR-102", "Small Tutorial Room", 15, new[] { Equipment.Whiteboard }),
                new ComputerLab("CL-201", "Software Engineering Lab", 25, 25, new[] { Equipment.Computers, Equipment.Projector }),
                new ComputerLab("CL-202", "Data Science Lab", 40, 35, new[] { Equipment.Computers, Equipment.Projector, Equipment.Whiteboard }),
                new LectureTheatre("LT-A", "Main Lecture Theatre", 150, new[] { Equipment.Projector, Equipment.Microphone, Equipment.LectureRecording }),
                new LectureTheatre("LT-B", "Faculty Auditorium", 80, new[] { Equipment.Projector, Equipment.Microphone, Equipment.VideoConferencing })
            });
            _logger.Log("Default rooms seeded.");
        }

        private void SeedDefaultBookings()
        {
            var monday = new DateTime(2026, 11, 2);
            _bookingRepository.ReplaceAll(new[]
            {
                new Booking("CL-201", new TeachingSession("CET1001 Programming Lab", 20, new[] { Equipment.Computers }, monday.AddHours(9), monday.AddHours(11))),
                new Booking("LT-A", new TeachingSession("CET2007 Systems Design", 120, new[] { Equipment.Projector }, monday.AddHours(13), monday.AddHours(15))),
                new Booking("SR-102", new TeachingSession("MAT1010 Tutorial", 12, new[] { Equipment.Whiteboard }, monday.AddDays(1).AddHours(10), monday.AddDays(1).AddHours(11)))
            });
            _logger.Log("Sample bookings seeded.");
        }

        private void BackupUnreadableFile(string path, List<StatusMessage> messages)
        {
            try
            {
                if (!File.Exists(path)) return;
                string backupPath = $"{path}.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}";
                File.Copy(path, backupPath, overwrite: true);
                _logger.Log($"Unreadable file backed up to '{backupPath}'.", "WARN");
                messages.Add(new StatusMessage(MessageLevel.Warning, $"Backed up unreadable file to {Path.GetFileName(backupPath)}"));
            }
            catch (Exception ex)
            {
                _logger.Log($"Could not back up '{path}': {ex.Message}", "ERROR");
                messages.Add(new StatusMessage(MessageLevel.Error, $"Could not back up '{Path.GetFileName(path)}': {ex.Message}"));
            }
        }
    }
}