using System;
using System.Collections.Generic;
using UniversityRoomBookingSystem.Models;

namespace UniversityRoomBookingSystem.Repositories
{
    public interface IBookingRepository
    {
        IEnumerable<Booking> GetAll();
        IEnumerable<Booking> GetByRoomId(string roomId);
        void Add(Booking booking);
        bool Remove(Guid bookingId);
        void Clear();
        void ReplaceAll(IEnumerable<Booking> bookings);
    }
}