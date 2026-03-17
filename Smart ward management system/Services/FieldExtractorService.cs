using OCR.Utilities;
using System.Text.RegularExpressions;

namespace OCR.Services
{
    public class FieldExtractorService
    {
        public dynamic ExtractFields(string ocrText)
        {
            var data = new Dictionary<string, string>();
            var genderMap = new Dictionary<string, string>
{
    { "पुरुष", "Male" },
    { "महिला", "Female" },
    { "अन्य", "Other" }  // optional, if applicable
};

            // Detect document type
            string docType = "Unknown";
            if (ocrText.Contains("नागरिकता") || ocrText.Contains("Citizen")) docType = "Nepal Citizenship";
            else if (ocrText.Contains("Passport")) docType = "Passport";

            // Extract fields with simple rules / AI placeholder
            // Citizenship example:
            if (docType == "Nepal Citizenship")
            {
                var nameMatch = Regex.Match(ocrText, @"नाम\s*(?:थर)?\s*[:\-]?\s*([\u0900-\u097F\s]+?)(?=\s+लिङ्ग|\s+पुरुष|\s+महिला|[:\n])");
                var genderMatch = Regex.Match(ocrText, @"लिङ्ग[:\s]*([^\s]+)");
                string nepaliGender = genderMatch.Success ? genderMatch.Groups[1].Value.Trim() : "";
                var citizenshipMatch = Regex.Match(ocrText, @"\d{1,2}-\d{1,2}-\d{1,2}-\d+");
                var districtMatch = Regex.Match(ocrText, @"जिल्ला\s*:\s*([^\s]+)");
                var municipalityMatch = Regex.Match(ocrText, @"L\.\s*[:]*\s*([^\s]+)");
                var wardMatch = Regex.Match(ocrText, @"वडा\s*(\d+)");
                var dobMatch = Regex.Match(ocrText, @"साल[:\s]*(\d+)\s*महिना[:\s]*(\d+)\s*गते[:\s]*(\d+)");

                data["fullName"] = nameMatch.Success ? nameMatch.Groups[1].Value.Trim() : "";
                data["gender"] = genderMap.ContainsKey(nepaliGender) ? genderMap[nepaliGender] : "Unknown";
                data["citizenshipNumber"] = citizenshipMatch.Success ? NumberConversion.ConvertNepaliDigits(citizenshipMatch.Value) : "";
                data["district"] = districtMatch.Success ? districtMatch.Groups[1].Value.Trim() : "";
                data["municipality"] = municipalityMatch.Success ? municipalityMatch.Groups[1].Value.Trim() : "";
                data["ward"] = wardMatch.Success ? NumberConversion.ConvertNepaliDigits(wardMatch.Groups[1].Value) : "";
                if (dobMatch.Success)
                {
                    var year = NumberConversion.ConvertNepaliDigits(dobMatch.Groups[1].Value);
                    var month = NumberConversion.ConvertNepaliDigits(dobMatch.Groups[2].Value);
                    var day = NumberConversion.ConvertNepaliDigits(dobMatch.Groups[3].Value);
                    data["dateOfBirthBS"] = $"{year}-{month}-{day}";
                }
            }

            return new
            {
                documentType = docType,
                fields = data
            };
        }
    }
}
