using System;
using System.Collections.Generic;
using System.Linq;
using UniversityRoomBookingSystem.Models;

namespace UniversityRoomBookingSystem.Repositories
{
    public class InMemoryBookingRepository : IBookingRepository
    {
        private readonly List<Booking> _bookings = new();
        private readonly object _sync = new();

        public IEnumerable<Booking> GetAll()
        {
            lock (_sync)
            {
                return _bookings.ToList();
            }
        }

        public IEnumerable<Booking> GetByRoomId(string roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId)) return Array.Empty<Booking>();
            lock (_sync)
            {
                return _bookings.Where(b => string.Equals(b.RoomId, roomId.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }

        public void Add(Booking booking)
        {
            if (booking is null) return;
            lock (_sync)
            {
                if (!_bookings.Any(b => b.BookingId == booking.BookingId))
                {
                    _bookings.Add(booking);
                }
            }
        }

        public bool Remove(Guid bookingId)
        {
            lock (_sync)
            {
                var existing = _bookings.FirstOrDefault(b => b.BookingId == bookingId);
                if (existing is null) return false;
                return _bookings.Remove(existing);
            }
        }

        public void Clear()
        {
            lock (_sync)
            {
                _bookings.Clear();
            }
        }

        public void ReplaceAll(IEnumerable<Booking> bookings)
        {
            if (bookings is null) throw new ArgumentNullException(nameof(bookings));
            lock (_sync)
            {
                _bookings.Clear();
                foreach (var booking in bookings)
                {
                    if (booking is not null && !_bookings.Any(b => b.BookingId == booking.BookingId))
                    {
                        _bookings.Add(booking);
                    }
                }
            }
        }
    }
}