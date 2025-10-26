using System;
using System.ComponentModel.DataAnnotations;

namespace EventEase.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        public int EventId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name is too long")]
        public string AttendeeName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string AttendeeEmail { get; set; } = string.Empty;

        public DateTime RegistrationDate { get; set; }
        public AttendanceStatus Status { get; set; }
    }

    public enum AttendanceStatus
    {
        Registered,
        Attended,
        Cancelled,
        NoShow
    }
}