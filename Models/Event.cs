using System;
using System.ComponentModel.DataAnnotations;

namespace EventEase.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Event name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        [Display(Name = "Event Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Event date is required")]
        [Display(Name = "Event Date")]
        [FutureDate(ErrorMessage = "Event date must be in the future")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [StringLength(200, ErrorMessage = "Location cannot be longer than 200 characters")]
        public string Location { get; set; } = string.Empty;
    }

    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.Date >= DateTime.Now.Date;
            }
            return false;
        }
    }
}