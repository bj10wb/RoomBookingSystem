using System;
using System.Collections.Generic;
using System.Linq;
using UniversityRoomBookingSystem.Models;

namespace UniversityRoomBookingSystem.Repositories
{
    public class InMemoryRoomRepository : IRoomRepository
    {
        private readonly List<Room> _rooms = new();
        private readonly object _sync = new();

        public IEnumerable<Room> GetAll()
        {
            lock (_sync)
            {
                return _rooms.ToList();
            }
        }

        public Room? GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            lock (_sync)
            {
                return _rooms.FirstOrDefault(r => string.Equals(r.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
            }
        }

        public void Add(Room room)
        {
            if (room is null) return;
            lock (_sync)
            {
                if (!_rooms.Any(r => string.Equals(r.Id, room.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    _rooms.Add(room);
                }
            }
        }

        public bool Remove(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            lock (_sync)
            {
                var existing = _rooms.FirstOrDefault(r => string.Equals(r.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
                if (existing is null) return false;
                return _rooms.Remove(existing);
            }
        }

        public void Clear()
        {
            lock (_sync)
            {
                _rooms.Clear();
            }
        }

        public void ReplaceAll(IEnumerable<Room> rooms)
        {
            if (rooms is null) throw new ArgumentNullException(nameof(rooms));
            lock (_sync)
            {
                _rooms.Clear();
                foreach (var room in rooms)
                {
                    if (room is not null && !_rooms.Any(r => string.Equals(r.Id, room.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        _rooms.Add(room);
                    }
                }
            }
        }
    }
}