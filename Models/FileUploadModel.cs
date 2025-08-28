using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ABCRetail.Models
{
    public class FileUploadModel
    {
        [Required]
        [Display(Name = "Select File")]
        public IFormFile? File { get; set; }

        public List<string>? UploadedFiles { get; set; }
    }
}
