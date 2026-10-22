using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UniversityRoomBookingSystem.Models;
using UniversityRoomBookingSystem.Services;

namespace UniversityRoomBookingSystem.UI
{
    public class ConsoleView
    {
        private const int IdWidth = 8, TypeWidth = 15, NameWidth = 26, CapacityWidth = 5, DetailsWidth = 44;

        public void Configure()
        {
            try { Console.OutputEncoding = Encoding.UTF8; } catch { /* non-fatal: keep the default encoding */ }
            try { Console.Title = "University Room Booking System"; } catch { /* no title when output is redirected */ }
        }


        public void ShowMainMenu()
        {
            DrawHeader("UNIVERSITY ROOM BOOKING SYSTEM", "CET2007 · Systems Design and Development");

            DrawMenuGroup("ROOMS");
            DrawMenuItem("1", "List all rooms");
            DrawMenuItem("2", "Check room suitability", "students, equipment, optional time");
            DrawMenuItem("4", "Search rooms", "ID, name, type or equipment");
            DrawMenuItem("5", "Sort by capacity", "manual QuickSort");
            DrawMenuItem("6", "Sort by name", "manual Insertion Sort");

            DrawMenuGroup("BOOKINGS");
            DrawMenuItem("3", "Create a teaching booking");
            DrawMenuItem("7", "View all bookings");

            DrawMenuGroup("DATA");
            DrawMenuItem("8", "Save data to JSON");
            DrawMenuItem("9", "Reload data from JSON");

            DrawMenuGroup("SYSTEM");
            DrawMenuItem("0", "Save and exit");
            Console.WriteLine();
        }

        // ------------------------------------------------------------------ messages

        public void ShowMessages(IEnumerable<StatusMessage> messages)
        {
            foreach (var message in messages)
            {
                switch (message.Level)
                {
                    case MessageLevel.Success: PrintSuccess(message.Text); break;
                    case MessageLevel.Warning: PrintWarning(message.Text); break;
                    case MessageLevel.Error: PrintError(message.Text); break;
                    default: PrintInfo(message.Text); break;
                }
            }
        }

        public void PrintSuccess(string message) => WriteColoured($"  ✓ {message}", ConsoleColor.Green);

        public void PrintError(string message) => WriteColoured($"  ✗ {message}", ConsoleColor.Red);

        public void PrintWarning(string message) => WriteColoured($"  ! {message}", ConsoleColor.DarkYellow);

        public void PrintInfo(string message) => WriteColoured($"  · {message}", ConsoleColor.DarkGray);

        public void PrintNote(string message) => WriteColoured($"    {message}", ConsoleColor.DarkGray);

        public void PrintBlankLine() => Console.WriteLine();

        public void PrintSectionTitle(string title)
        {
            Console.WriteLine();
            WriteColoured($"  {title}", ConsoleColor.Cyan);
            WriteColoured($"  {new string('─', Math.Min(Math.Max(title.Length, 18), 80))}", ConsoleColor.DarkGray);
        }

        // ------------------------------------------------------------------ tables

        public void ShowRooms(IEnumerable<Room> rooms, string title)
        {
            var displayedRooms = rooms.ToList();
            PrintSectionTitle(title);

            if (displayedRooms.Count == 0)
            {
                PrintWarning("No rooms to display.");
                return;
            }

            int width = IdWidth + TypeWidth + NameWidth + CapacityWidth + DetailsWidth + 12;
            PrintRule(width);
            WriteColoured($"  {"ID",-IdWidth} │ {"Type",-TypeWidth} │ {"Name",-NameWidth} │ {"Cap",-CapacityWidth} │ {"Details",-DetailsWidth}", ConsoleColor.Cyan);
            PrintRule(width);

            foreach (var room in displayedRooms)
            {
                string equipment = room.Equipment.Count == 0 ? "no equipment" : string.Join(", ", room.Equipment);
                string details = room is ComputerLab lab ? $"{lab.Workstations} PCs · {equipment}" : equipment;
                Console.WriteLine($"  {Clip(room.Id, IdWidth),-IdWidth} │ {Clip(room.RoomType, TypeWidth),-TypeWidth} │ {Clip(room.Name, NameWidth),-NameWidth} │ {room.Capacity,-CapacityWidth} │ {Clip(details, DetailsWidth),-DetailsWidth}");
            }

            PrintRule(width);
            WriteColoured($"  {displayedRooms.Count} room(s)", ConsoleColor.DarkGray);
        }

        public void ShowBookings(IEnumerable<Booking> bookings)
        {
            var displayedBookings = bookings.OrderBy(booking => booking.Session.Start).ToList();
            PrintSectionTitle("All bookings");

            if (displayedBookings.Count == 0)
            {
                PrintWarning("No bookings registered yet. Choose option 3 to create one.");
                return;
            }

            const int refWidth = 8, roomWidth = 8, moduleWidth = 26, studentsWidth = 8, windowWidth = 24;
            int width = refWidth + roomWidth + moduleWidth + studentsWidth + windowWidth + 12;
            PrintRule(width);
            WriteColoured($"  {"Ref",-refWidth} │ {"Room",-roomWidth} │ {"Module",-moduleWidth} │ {"Students",-studentsWidth} │ {"Time window",-windowWidth}", ConsoleColor.Cyan);
            PrintRule(width);

            foreach (var booking in displayedBookings)
            {
                string window = $"{booking.Session.Start:yyyy-MM-dd HH:mm}–{booking.Session.End:HH:mm}";
                Console.WriteLine($"  {ShortReference(booking),-refWidth} │ {Clip(booking.RoomId, roomWidth),-roomWidth} │ {Clip(booking.Session.ModuleName, moduleWidth),-moduleWidth} │ {booking.Session.StudentCount,-studentsWidth} │ {Clip(window, windowWidth),-windowWidth}");
            }

            PrintRule(width);
            WriteColoured($"  {displayedBookings.Count} booking(s)", ConsoleColor.DarkGray);
        }

        public static string ShortReference(Booking booking) => booking.BookingId.ToString("N")[..8].ToUpperInvariant();

        public string ReadLine(string prompt)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(prompt);
            Console.ResetColor();
            string? line = Console.ReadLine();
            if (line is null) throw new EndOfStreamException("Console input was closed.");
            return line;
        }

        public int ReadInt(string prompt, int min, int max, string errorMessage)
        {
            while (true)
            {
                if (int.TryParse(ReadLine(prompt).Trim(), out int value) && value >= min && value <= max)
                    return value;
                PrintError(errorMessage);
            }
        }

        public DateTime? ReadDate(string prompt, bool allowBlank)
        {
            while (true)
            {
                string input = ReadLine(prompt).Trim();
                if (allowBlank && string.IsNullOrEmpty(input)) return null;
                if (DateTime.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    return date.Date;
                PrintError("Invalid date. Use yyyy-MM-dd (e.g. 2026-11-02).");
            }
        }

        public TimeSpan ReadTime(string prompt)
        {
            while (true)
            {
                string input = ReadLine(prompt).Trim();
                if (DateTime.TryParseExact(input, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                    return time.TimeOfDay;
                PrintError("Invalid time. Use 24-hour HH:mm (e.g. 09:30).");
            }
        }

        public TimeSpan ReadEndTime(string prompt, TimeSpan startTime)
        {
            while (true)
            {
                var endTime = ReadTime(prompt);
                if (endTime > startTime) return endTime;
                PrintError($"End time must be later than the start time ({startTime:hh\\:mm}).");
            }
        }

        public HashSet<Equipment> ReadEquipment()
        {
            var options = (Equipment[])Enum.GetValues(typeof(Equipment));

            while (true)
            {
                Console.WriteLine();
                WriteColoured("  Required equipment (numbers or names, comma-separated; Enter for none)", ConsoleColor.DarkYellow);
                for (int i = 0; i < options.Length; i++)
                {
                    DrawMenuItem((i + 1).ToString(), options[i].ToString());
                }

                string input = ReadLine("  Choice(s): ");
                var selected = new HashSet<Equipment>();
                if (string.IsNullOrWhiteSpace(input)) return selected;

                var invalidChoices = new List<string>();
                var choices = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var choice in choices)
                {
                    if (int.TryParse(choice, out int optionNumber))
                    {
                        if (optionNumber >= 1 && optionNumber <= options.Length) selected.Add(options[optionNumber - 1]);
                        else invalidChoices.Add(choice);
                        continue;
                    }

                    if (Enum.GetNames<Equipment>().Any(name => string.Equals(name, choice, StringComparison.OrdinalIgnoreCase)) &&
                        Enum.TryParse<Equipment>(choice, ignoreCase: true, out var equipment))
                    {
                        selected.Add(equipment);
                        continue;
                    }

                    invalidChoices.Add(choice);
                }

                if (invalidChoices.Count == 0) return selected;
                PrintError($"Invalid equipment choices: {string.Join(", ", invalidChoices)}. Use numbers (e.g. 1,4) or names (e.g. Projector,Microphone).");
            }
        }


        private static void WriteColoured(string text, ConsoleColor colour)
        {
            Console.ForegroundColor = colour;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        private static void PrintRule(int width) => WriteColoured($"  {new string('─', width)}", ConsoleColor.DarkGray);

        private static void DrawHeader(string title, string subtitle)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ╭────────────────────────────────────────────────────────────────────╮");
            Console.WriteLine($"  │ {title.PadRight(66)} │");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  │ {subtitle.PadRight(66)} │");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ╰────────────────────────────────────────────────────────────────────╯");
            Console.ResetColor();
        }

        private static void DrawMenuGroup(string title)
        {
            Console.WriteLine();
            WriteColoured($"  {title}", ConsoleColor.DarkYellow);
        }

        private static void DrawMenuItem(string number, string label, string? hint = null)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"    [{number}] ");
            Console.ResetColor();
            Console.Write(label);
            if (!string.IsNullOrWhiteSpace(hint))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"  ·  {hint}");
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        private static string Clip(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value[..Math.Max(0, maxLength - 1)] + "…";
        }
    }
}