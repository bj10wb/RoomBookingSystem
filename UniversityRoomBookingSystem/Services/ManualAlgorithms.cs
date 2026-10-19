using System;
using System.Collections.Generic;
using UniversityRoomBookingSystem.Models;

namespace UniversityRoomBookingSystem.Services
{
    public static class ManualAlgorithms
    {
        public static List<Room> SearchRoomsByName(IList<Room>? rooms, string? searchTerm)
        {
            var matchingRooms = new List<Room>();
            if (rooms == null || rooms.Count == 0 || string.IsNullOrWhiteSpace(searchTerm))
                return matchingRooms;

            string query = searchTerm.Trim().ToLowerInvariant();
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i]?.Name != null && rooms[i].Name.ToLowerInvariant().Contains(query))
                {
                    matchingRooms.Add(rooms[i]);
                }
            }
            return matchingRooms;
        }

        /// <summary>Finds rooms matching an ID, name, room type, or equipment term.</summary>
        public static List<Room> SearchRooms(IList<Room>? rooms, string? searchTerm)
        {
            var matchingRooms = new List<Room>();
            if (rooms == null || rooms.Count == 0 || string.IsNullOrWhiteSpace(searchTerm))
                return matchingRooms;

            string query = searchTerm.Trim().ToLowerInvariant();
            string compactQuery = query.Replace(" ", string.Empty);

            for (int i = 0; i < rooms.Count; i++)
            {
                Room? room = rooms[i];
                if (room == null) continue;

                bool matchesQuery =
                    (room.Id ?? string.Empty).ToLowerInvariant().Contains(query) ||
                    (room.Name ?? string.Empty).ToLowerInvariant().Contains(query) ||
                    room.RoomType.ToLowerInvariant().Replace(" ", string.Empty).Contains(compactQuery);

                if (!matchesQuery)
                {
                    foreach (var equipment in room.Equipment)
                    {
                        if (equipment.ToString().ToLowerInvariant().Contains(compactQuery)) { matchesQuery = true; break; }
                    }
                }

                if (matchesQuery) matchingRooms.Add(room);
            }
            return matchingRooms;
        }

        public static Room? BinarySearchRoomById(IList<Room>? sortedRooms, string? targetId)
        {
            if (sortedRooms == null || sortedRooms.Count == 0 || string.IsNullOrWhiteSpace(targetId))
                return null;

            int low = 0;
            int high = sortedRooms.Count - 1;
            string target = targetId.Trim();

            while (low <= high)
            {
                int mid = low + (high - low) / 2;
                if (sortedRooms[mid] == null) return null;

                int cmp = string.Compare(sortedRooms[mid].Id, target, StringComparison.OrdinalIgnoreCase);
                if (cmp == 0) return sortedRooms[mid];
                if (cmp < 0) low = mid + 1;
                else high = mid - 1;
            }

            return null;
        }

        public static void QuickSortByCapacity(Room[]? array, int left, int right, bool ascending = true)
        {
            if (array == null || array.Length <= 1) return;
            if (left < 0 || right >= array.Length || left >= right) return;

            int pivotIndex = Partition(array, left, right, ascending);
            QuickSortByCapacity(array, left, pivotIndex - 1, ascending);
            QuickSortByCapacity(array, pivotIndex + 1, right, ascending);
        }

        private static int Partition(Room[] array, int left, int right, bool ascending)
        {
            int pivot = array[right].Capacity;
            int i = left - 1;

            for (int j = left; j < right; j++)
            {
                bool belongsBeforePivot = ascending
                    ? array[j].Capacity <= pivot
                    : array[j].Capacity >= pivot;

                if (belongsBeforePivot)
                {
                    i++;
                    (array[i], array[j]) = (array[j], array[i]);
                }
            }
            (array[i + 1], array[right]) = (array[right], array[i + 1]);
            return i + 1;
        }

        public static void InsertionSortByName(Room[]? array)
        {
            if (array == null || array.Length <= 1) return;

            for (int i = 1; i < array.Length; i++)
            {
                Room key = array[i];
                int j = i - 1;

                while (j >= 0 && string.Compare(array[j].Name, key.Name, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    array[j + 1] = array[j];
                    j--;
                }
                array[j + 1] = key;
            }
        }
    }
}