using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace OCR.Models
{

    public class OcrResult
    {
        public bool Success { get; set; }
        public string DocumentType { get; set; } = "Unknown";
        public string DocumentSide { get; set; } = "Unknown";
        public Dictionary<string, string> Fields { get; set; } = new();
        public string RawText { get; set; } = string.Empty;
        public List<string> Warnings { get; set; } = new();
    }
    public class BothSidesUploadRequest
    {
        [Required]
        public IFormFile? FrontImage { get; set; }

        [Required]
        public IFormFile? BackImage { get; set; }
    }

    public class CitizenshipData
    {
        public string? FullName { get; set; }
        public string? FullNameEnglish { get; set; }
        public string? Gender { get; set; }
        public string? DateOfBirth { get; set; }
        public string? DateOfBirthBS { get; set; }
        public string? CitizenshipNumber { get; set; }
        public string? IssuedDistrict { get; set; }
        public string? IssuedDate { get; set; }
        public string? PermanentAddress { get; set; }
        public string? WardNumber { get; set; }
        public string? Municipality { get; set; }
        public string? District { get; set; }
        public string? Province { get; set; }
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public string? SpouseName { get; set; }
    }
}