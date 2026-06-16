using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetailAppCore.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [StringLength(100, ErrorMessage = "First Name can't be longer than 100 characters.")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Last Name can't be longer than 100 characters.")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255, ErrorMessage = "Email can't be longer than 255 characters.")]
        public string Email { get; set; }

        [Required]
        [Phone]
        [StringLength(10, ErrorMessage = "Phone number can't be longer than 10 characters.")]
        public string PhoneNumber { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string ConfirmPassword { get; set; }
    }
}
