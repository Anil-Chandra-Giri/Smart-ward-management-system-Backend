using OCR.Utilities;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace OCR.Services
{
    public class FieldExtractorService
    {
        //public dynamic ExtractFields(string ocrText);
        private readonly Dictionary<string, string> genderMap = new Dictionary<string, string>
        {
            //var data = new Dictionary<string, string>();
            //var genderMap = new Dictionary<string, string>
    
        { "पुरुष", "Male" },
        { "महिला", "Female" },
        { "अन्य", "Other" },  // optional, if applicable
                { "अन्य", "Other" }
        };

        private readonly Dictionary<string, string> nepaliToEnglishMonth = new Dictionary<string, string>
        {
            { "बैशाख", "01" }, { "वैशाख", "01" },
            { "जेठ", "02" }, { "जेष्ठ", "02" },
            { "असार", "03" }, { "आषाढ", "03" },
            { "साउन", "04" }, { "श्रावण", "04" },
            { "भदौ", "05" }, { "भाद्र", "05" },
            { "असोज", "06" }, { "आश्विन", "06" },
            { "कात्तिक", "07" }, { "कार्तिक", "07" },
            { "मंसिर", "08" }, { "मार्ग", "08" },
            { "पुस", "09" }, { "पौष", "09" },
            { "माघ", "10" },
            { "फागुन", "11" }, { "फाल्गुन", "11" },
            { "चैत", "12" }, { "चैत्र", "12" }
        };

        public dynamic ExtractFields(string ocrText, bool isFrontSide = true)
        {
            var data = new Dictionary<string, string>();

            // Clean the OCR text
            ocrText = CleanOcrText(ocrText);

            // Detect document type
            string docType = "Unknown";
            if (ocrText.Contains("नागरिकता") || ocrText.Contains("Citizen")) docType = "Nepal Citizenship";
            else if (ocrText.Contains("Passport")) docType = "Passport";
            //string docType = DetectDocumentType(ocrText);

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
                if (isFrontSide)
                {
                    var year = NumberConversion.ConvertNepaliDigits(dobMatch.Groups[1].Value);
                    var month = NumberConversion.ConvertNepaliDigits(dobMatch.Groups[2].Value);
                    var day = NumberConversion.ConvertNepaliDigits(dobMatch.Groups[3].Value);
                    data["dateOfBirthBS"] = $"{year}-{month}-{day}";
                    ExtractFrontSideFields(ocrText, data);
                }
                else
                {
                    ExtractBackSideFields(ocrText, data);
            }
            }

            return new
            {
                documentType = docType,
                fields = data
            };
        }

        private string CleanOcrText(string ocrText)
        {
            // Remove excessive whitespace
            ocrText = Regex.Replace(ocrText, @"\s+", " ");

            // Fix common OCR artifacts
            ocrText = Regex.Replace(ocrText, @"[|•·]", "");

            return ocrText.Trim();
    }

        private string DetectDocumentType(string ocrText)
        {
            if (ocrText.Contains("नागरिकता") ||
                ocrText.Contains("Citizenship") ||
                ocrText.Contains("राष्ट्रिय परिचय"))
                return "Nepal Citizenship";

            if (ocrText.Contains("Passport") || ocrText.Contains("राहदानी"))
                return "Passport";

            if (ocrText.Contains("Driving License") || ocrText.Contains("सवारी चालक"))
                return "Driving License";

            return "Unknown";
}

        private void ExtractFrontSideFields(string ocrText, Dictionary<string, string> data)
        {
            // 1. Extract Full Name (Nepali)
            var namePatterns = new[]
            {
                @"नाम\s*[:\-]?\s*([\u0900-\u097F\s]+?)(?=\s+लिङ्ग|\s+पुरुष|\s+महिला|\s+जन्म|\n|$)",
                @"नाम\s*थर\s*[:\-]?\s*([\u0900-\u097F\s]+?)(?=\s+लिङ्ग|\n|$)",
                @"नाम\s*([\u0900-\u097F]{2,}(?:\s+[\u0900-\u097F]{2,})*)"
            };

            foreach (var pattern in namePatterns)
            {
                var match = Regex.Match(ocrText, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    data["fullName"] = CleanNepaliText(match.Groups[1].Value.Trim());
                    break;
                }
            }

            // 2. Extract Full Name (English) - if available
            var englishNameMatch = Regex.Match(ocrText, @"Name\s*[:\-]?\s*([A-Za-z\s]+?)(?=\s+Gender|\n|$)", RegexOptions.IgnoreCase);
            if (englishNameMatch.Success)
            {
                data["fullNameEnglish"] = englishNameMatch.Groups[1].Value.Trim();
            }

            // 3. Extract Gender
            var genderMatch = Regex.Match(ocrText, @"लिङ्ग\s*[:\-]?\s*([\u0900-\u097F]+)");
            if (genderMatch.Success)
            {
                var nepaliGender = genderMatch.Groups[1].Value.Trim();
                data["gender"] = genderMap.ContainsKey(nepaliGender) ? genderMap[nepaliGender] : nepaliGender;
            }

            // 4. Extract Date of Birth
            ExtractDateOfBirth(ocrText, data);

            // 5. Extract Citizenship Number
            var citizenshipPatterns = new[]
            {
                @"(\d{1,2}[-/]\d{1,2}[-/]\d{1,2}[-/]\d+)",
                @"नं\.?\s*[:\-]?\s*(\d{1,2}[-/]\d{1,2}[-/]\d{1,2}[-/]\d+)",
                @"प्रमाणपत्र\s*नं\.?\s*[:\-]?\s*(\d{1,2}[-/]\d{1,2}[-/]\d{1,2}[-/]\d+)"
            };

            foreach (var pattern in citizenshipPatterns)
            {
                var match = Regex.Match(ocrText, pattern);
                if (match.Success)
                {
                    data["citizenshipNumber"] = NumberConversion.ConvertNepaliDigits(match.Groups[1].Value);
                    break;
                }
            }
        }

        private void ExtractBackSideFields(string ocrText, Dictionary<string, string> data)
        {
            // 1. Extract Issued District (जारी जिल्ला)
            var issuedDistrictPatterns = new[]
            {
                @"जारी\s*जिल्ला\s*[:\-]?\s*([\u0900-\u097F]+)",
                @"जिल्ला\s*[:\-]?\s*([\u0900-\u097F]+)",
                @"जारी\s*[:\-]?\s*([\u0900-\u097F]+)"
            };

            foreach (var pattern in issuedDistrictPatterns)
            {
                var match = Regex.Match(ocrText, pattern);
                if (match.Success)
                {
                    data["citizenshipIssuedDistrict"] = CleanNepaliText(match.Groups[1].Value.Trim());
                    break;
                }
            }

            // 2. Extract Issued Date
            ExtractIssuedDate(ocrText, data);

            // 3. Extract Permanent Address
            var addressPatterns = new[]
            {
                @"स्थायी\s*ठेगाना\s*[:\-]?\s*([\u0900-\u097F,\s\d]+?)(?=\s+जिल्ला|\s+वडा|\n|$)",
                @"ठेगाना\s*[:\-]?\s*([\u0900-\u097F,\s\d]+?)(?=\s+जिल्ला|\n|$)",
                @"स्थायी\s*[:\-]?\s*([\u0900-\u097F,\s\d]+)"
            };

            foreach (var pattern in addressPatterns)
            {
                var match = Regex.Match(ocrText, pattern);
                if (match.Success)
                {
                    data["permanentAddress"] = CleanNepaliText(match.Groups[1].Value.Trim());
                    break;
                }
            }

            // 4. Extract Ward Number
            var wardPatterns = new[]
            {
                @"वडा\s*नं\.?\s*[:\-]?\s*(\d+)",
                @"वडा\s*(\d+)",
                @"Ward\s*No\.?\s*[:\-]?\s*(\d+)"
            };

            foreach (var pattern in wardPatterns)
            {
                var match = Regex.Match(ocrText, pattern);
                if (match.Success)
                {
                    data["wardNumber"] = NumberConversion.ConvertNepaliDigits(match.Groups[1].Value);
                    break;
                }
            }

            // 5. Extract Municipality
            var municipalityPatterns = new[]
            {
                @"([\u0900-\u097F]+)\s*नगर\s*पालिका",
                @"([\u0900-\u097F]+)\s*गाउँ\s*पालिका",
                @"([\u0900-\u097F]+)\s*Municipality"
            };

            foreach (var pattern in municipalityPatterns)
            {
                var match = Regex.Match(ocrText, pattern);
                if (match.Success)
                {
                    data["municipality"] = CleanNepaliText(match.Groups[1].Value.Trim()) +
                        (pattern.Contains("नगर") ? " नगरपालिका" :
                         pattern.Contains("गाउँ") ? " गाउँपालिका" : "");
                    break;
                }
            }

            // 6. Extract District
            var districtPatterns = new[]
            {
                @"जिल्ला\s*[:\-]?\s*([\u0900-\u097F]+)",
                @"District\s*[:\-]?\s*([A-Za-z]+)"
            };

            foreach (var pattern in districtPatterns)
            {
                var match = Regex.Match(ocrText, pattern);
                if (match.Success)
                {
                    data["district"] = match.Groups[1].Value.Trim();
                    break;
                }
            }

            // 7. Extract Province
            var provinceMatch = Regex.Match(ocrText, @"प्रदेश\s*नं\.?\s*[:\-]?\s*(\d+)");
            if (provinceMatch.Success)
            {
                data["province"] = "प्रदेश " + NumberConversion.ConvertNepaliDigits(provinceMatch.Groups[1].Value);
            }
        }

        private void ExtractDateOfBirth(string ocrText, Dictionary<string, string> data)
        {
            // Pattern for BS date format: वर्ष महिना गते
            var dobPatterns = new[]
            {
                @"जन्म\s*मिति\s*[:\-]?\s*(\d{4})\s*[-/]?\s*(\d{1,2})\s*[-/]?\s*(\d{1,2})",
                @"साल\s*[:\-]?\s*(\d+)\s*महिना\s*[:\-]?\s*(\d+)\s*गते\s*[:\-]?\s*(\d+)",
                @"(\d{4})\s*[-/]\s*(\d{1,2})\s*[-/]\s*(\d{1,2})",
                @"मिति\s*[:\-]?\s*(\d{2,4})[-\s](\d{1,2})[-\s](\d{1,2})"
            };

            foreach (var pattern in dobPatterns)
            {
                var match = Regex.Match(ocrText, pattern);
                if (match.Success && match.Groups.Count >= 4)
                {
                    var year = NumberConversion.ConvertNepaliDigits(match.Groups[1].Value);
                    var month = NumberConversion.ConvertNepaliDigits(match.Groups[2].Value).PadLeft(2, '0');
                    var day = NumberConversion.ConvertNepaliDigits(match.Groups[3].Value).PadLeft(2, '0');

                    // Ensure year is 4 digits
                    if (year.Length == 2) year = "20" + year;
                    else if (year.Length == 3) year = "1" + year;

                    data["dateOfBirth"] = $"{year}-{month}-{day}";
                    data["dateOfBirthBS"] = $"{year}-{month}-{day}";
                    break;
                }
            }
        }

        private void ExtractIssuedDate(string ocrText, Dictionary<string, string> data)
        {
            var issuedDatePatterns = new[]
            {
                @"जारी\s*मिति\s*[:\-]?\s*(\d{4})\s*[-/]?\s*(\d{1,2})\s*[-/]?\s*(\d{1,2})",
                @"प्रमाणपत्र\s*मिति\s*[:\-]?\s*(\d{4})\s*[-/]?\s*(\d{1,2})\s*[-/]?\s*(\d{1,2})",
                @"Issued\s*Date\s*[:\-]?\s*(\d{4})[-/](\d{1,2})[-/](\d{1,2})"
            };

            foreach (var pattern in issuedDatePatterns)
            {
                var match = Regex.Match(ocrText, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count >= 4)
                {
                    var year = NumberConversion.ConvertNepaliDigits(match.Groups[1].Value);
                    var month = NumberConversion.ConvertNepaliDigits(match.Groups[2].Value).PadLeft(2, '0');
                    var day = NumberConversion.ConvertNepaliDigits(match.Groups[3].Value).PadLeft(2, '0');

                    data["citizenshipIssuedDate"] = $"{year}-{month}-{day}";
                    break;
                }
            }
        }

        private string CleanNepaliText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            // Remove non-Nepali characters and extra spaces
            text = Regex.Replace(text, @"[^\u0900-\u097F\s]", " ");
            text = Regex.Replace(text, @"\s+", " ");

            return text.Trim();
        }
    }
}

//using OCR.Utilities;
//using System.Text.RegularExpressions;
//using System.Collections.Generic;
//using System.Linq;

//namespace OCR.Services
//{
//    public class FieldExtractorService
//    {
//        private readonly Dictionary<string, string> genderMap = new()
//        {
//            { "पुरुष", "Male" },
//            { "महिला", "Female" },
//            { "अन्य", "Other" },
//            { "पु", "Male" },
//            { "म", "Female" }
//        };

//        private readonly Dictionary<string, string> nepaliToEnglishMonth = new()
//        {
//            { "बैशाख", "01" }, { "वैशाख", "01" }, { "बैसाख", "01" },
//            { "जेठ", "02" }, { "जेष्ठ", "02" }, { "जे", "02" },
//            { "असार", "03" }, { "आषाढ", "03" }, { "अ", "03" },
//            { "साउन", "04" }, { "श्रावण", "04" }, { "सा", "04" },
//            { "भदौ", "05" }, { "भाद्र", "05" }, { "भ", "05" },
//            { "असोज", "06" }, { "आश्विन", "06" }, { "अ", "06" },
//            { "कात्तिक", "07" }, { "कार्तिक", "07" }, { "का", "07" },
//            { "मंसिर", "08" }, { "मार्ग", "08" }, { "मं", "08" },
//            { "पुस", "09" }, { "पौष", "09" }, { "पु", "09" },
//            { "माघ", "10" }, { "मा", "10" },
//            { "फागुन", "11" }, { "फाल्गुन", "11" }, { "फा", "11" },
//            { "चैत", "12" }, { "चैत्र", "12" }, { "चै", "12" }
//        };

//        private readonly Dictionary<string, string> englishMonthMap = new()
//        {
//            { "JAN", "01" }, { "JANUARY", "01" },
//            { "FEB", "02" }, { "FEBRUARY", "02" },
//            { "MAR", "03" }, { "MARCH", "03" },
//            { "APR", "04" }, { "APRIL", "04" },
//            { "MAY", "05" },
//            { "JUN", "06" }, { "JUNE", "06" },
//            { "JUL", "07" }, { "JULY", "07" },
//            { "AUG", "08" }, { "AUGUST", "08" },
//            { "SEP", "09" }, { "SEPTEMBER", "09" },
//            { "OCT", "10" }, { "OCTOBER", "10" },
//            { "NOV", "11" }, { "NOVEMBER", "11" },
//            { "DEC", "12" }, { "DECEMBER", "12" }
//        };

//        public (string DocumentType, string DocumentSide, Dictionary<string, string> Fields) ExtractFields(string ocrText, string? specifiedSide = null)
//        {
//            var fields = new Dictionary<string, string>();

//            // Clean the OCR text
//            ocrText = CleanOcrText(ocrText);

//            // Detect document type
//            string docType = DetectDocumentType(ocrText);

//            // Detect which side of the document this is
//            string documentSide = specifiedSide ?? DetectDocumentSide(ocrText);

//            // Extract fields based on document type and side
//            if (docType == "Nepal Citizenship")
//            {
//                ExtractNepalCitizenshipFields(ocrText, documentSide, fields);
//            }
//            else if (docType == "Passport")
//            {
//                ExtractPassportFields(ocrText, fields);
//            }
//            else if (docType == "Driving License")
//            {
//                ExtractDrivingLicenseFields(ocrText, fields);
//            }

//            return (docType, documentSide, fields);
//        }

//        private string CleanOcrText(string ocrText)
//        {
//            if (string.IsNullOrEmpty(ocrText)) return "";

//            // Remove excessive whitespace
//            ocrText = Regex.Replace(ocrText, @"\s+", " ");

//            // Fix common OCR artifacts
//            ocrText = Regex.Replace(ocrText, @"[|•·]", " ");
//            ocrText = Regex.Replace(ocrText, @"[\[\]{}()]", " ");

//            // Fix common misreads
//            ocrText = ocrText.Replace("लिङ्‍ग", "लिङ्ग")
//                             .Replace("नागरिकत", "नागरिकता")
//                             .Replace("प्रमाणपत", "प्रमाणपत्र");

//            return ocrText.Trim();
//        }

//        private string DetectDocumentType(string ocrText)
//{
//    // Change from Dictionary<string, string> to Dictionary<string, string[]>
//    var typeIndicators = new Dictionary<string, string[]>
//    {
//        { "Nepal Citizenship", new string[] { "नागरिकता", "Citizenship", "नागरिक", "राष्ट्रिय परिचय", "नागरिकता प्रमाणपत्र" } },
//        { "Passport", new string[] { "Passport", "राहदानी", "PASSPORT", "राहदानी नं" } },
//        { "Driving License", new string[] { "Driving License", "सवारी चालक", "DRIVING LICENSE", "चालक अनुमतिपत्र" } }
//    };

//    foreach (var type in typeIndicators)
//    {
//        foreach (var indicator in type.Value)
//        {
//            if (ocrText.Contains(indicator, StringComparison.OrdinalIgnoreCase))
//            {
//                return type.Key;
//            }
//        }
//    }

//    return "Unknown";
//}

//        private string DetectDocumentSide(string ocrText)
//        {
//            // Front side indicators
//            string[] frontIndicators = {
//                "नाम", "Name", "लिङ्ग", "Gender", "जन्म मिति", "Date of Birth",
//                "नागरिकता नं", "Citizenship No", "प्रमाणपत्र नं",
//                "पुरुष", "महिला", "Male", "Female"
//            };

//            // Back side indicators
//            string[] backIndicators = {
//                "जारी", "Issued", "स्थायी ठेगाना", "Permanent Address",
//                "जिल्ला", "District", "वडा", "Ward", "प्रदेश", "Province",
//                "औँठा", "Thumb", "दस्तखत", "Signature", "ठेगाना",
//                "नगरपालिका", "गाउँपालिका", "Municipality", "Rural"
//            };

//            int frontScore = frontIndicators.Count(indicator =>
//                ocrText.Contains(indicator, StringComparison.OrdinalIgnoreCase));

//            int backScore = backIndicators.Count(indicator =>
//                ocrText.Contains(indicator, StringComparison.OrdinalIgnoreCase));

//            // Check for specific patterns that strongly indicate back side
//            if (Regex.IsMatch(ocrText, @"(?:जारी|Issued).{0,20}(?:मिति|Date)", RegexOptions.IgnoreCase))
//                backScore += 3;

//            if (Regex.IsMatch(ocrText, @"(?:स्थायी|Permanent).{0,20}(?:ठेगाना|Address)", RegexOptions.IgnoreCase))
//                backScore += 3;

//            return backScore > frontScore ? "back" : "front";
//        }

//        private void ExtractNepalCitizenshipFields(string ocrText, string side, Dictionary<string, string> fields)
//        {
//            // Common fields for both sides
//            ExtractCitizenshipNumber(ocrText, fields);

//            if (side == "front")
//            {
//                ExtractFrontSideFields(ocrText, fields);
//            }
//            else
//            {
//                ExtractBackSideFields(ocrText, fields);
//            }

//            // Always try to extract these regardless of side
//            ExtractNames(ocrText, fields);
//            ExtractDates(ocrText, fields);
//        }

//        private void ExtractFrontSideFields(string ocrText, Dictionary<string, string> fields)
//        {
//            // Extract Full Name (Nepali)
//            var namePatterns = new[]
//            {
//                @"नाम\s*[:\-]?\s*([\u0900-\u097F\s]{3,50}?)(?=\s+(?:लिङ्ग|पुरुष|महिला|जन्म|मिति|को|$))",
//                @"नाम\s*थर\s*[:\-]?\s*([\u0900-\u097F\s]{3,50}?)(?=\s+(?:लिङ्ग|पुरुष|महिला|$))",
//                @"नाम\s*:\s*([\u0900-\u097F\s]{3,50})"
//            };

//            foreach (var pattern in namePatterns)
//            {
//                var match = Regex.Match(ocrText, pattern, RegexOptions.IgnoreCase);
//                if (match.Success)
//                {
//                    fields["fullName"] = CleanNepaliText(match.Groups[1].Value);
//                    break;
//                }
//            }

//            // Extract Full Name (English)
//            var englishNameMatch = Regex.Match(ocrText, @"Name\s*[:\-]?\s*([A-Za-z\s]{3,50}?)(?=\s+(?:Gender|Sex|Date|$))", RegexOptions.IgnoreCase);
//            if (englishNameMatch.Success)
//            {
//                fields["fullNameEnglish"] = englishNameMatch.Groups[1].Value.Trim();
//            }

//            // Extract Gender
//            ExtractGender(ocrText, fields);

//            // Extract Date of Birth
//            ExtractDateOfBirth(ocrText, fields);
//        }

//        private void ExtractBackSideFields(string ocrText, Dictionary<string, string> fields)
//        {
//            // Extract Issued District (जारी जिल्ला)
//            var issuedDistrictPatterns = new[]
//            {
//                @"(?:जारी|Issued).{0,20}(?:जिल्ला|District)\s*[:\-]?\s*([\u0900-\u097F\s]{3,30})",
//                @"जारी\s*[:\-]?\s*([\u0900-\u097F\s]{3,30})",
//                @"District\s*[:\-]?\s*([\u0900-\u097F\s]{3,30})",
//                @"जिल्ला\s*[:\-]?\s*([\u0900-\u097F\s]{3,30})"
//            };

//            foreach (var pattern in issuedDistrictPatterns)
//            {
//                var match = Regex.Match(ocrText, pattern, RegexOptions.IgnoreCase);
//                if (match.Success)
//                {
//                    fields["citizenshipIssuedDistrict"] = CleanNepaliText(match.Groups[1].Value);
//                    break;
//                }
//            }

//            // Extract Issued Date
//            ExtractIssuedDate(ocrText, fields);

//            // Extract Permanent Address
//            var addressPatterns = new[]
//            {
//                @"(?:स्थायी|Permanent).{0,20}(?:ठेगाना|Address)\s*[:\-]?\s*([\u0900-\u097F,\s\d]{5,100}?)(?=\s+(?:जिल्ला|District|वडा|Ward|$))",
//                @"ठेगाना\s*[:\-]?\s*([\u0900-\u097F,\s\d]{5,100}?)(?=\s+(?:जिल्ला|District|$))",
//                @"Address\s*[:\-]?\s*([^,.]{5,100})"
//            };

//            foreach (var pattern in addressPatterns)
//            {
//                var match = Regex.Match(ocrText, pattern, RegexOptions.IgnoreCase);
//                if (match.Success)
//                {
//                    fields["permanentAddress"] = CleanNepaliText(match.Groups[1].Value);
//                    break;
//                }
//            }

//            // Extract Ward Number
//            var wardPatterns = new[]
//            {
//                @"(?:वडा|Ward)\s*(?:नं\.?|No\.?)?\s*[:\-]?\s*(\d+)",
//                @"(?:वडा|Ward)\s*[:\-]?\s*(\d+)",
//                @"[Ww]ard\s*No\.?\s*[:\-]?\s*(\d+)"
//            };

//            foreach (var pattern in wardPatterns)
//            {
//                var match = Regex.Match(ocrText, pattern, RegexOptions.IgnoreCase);
//                if (match.Success)
//                {
//                    fields["wardNumber"] = NumberConversion.ConvertNepaliDigits(match.Groups[1].Value);
//                    break;
//                }
//            }

//            // Extract Municipality
//            var municipalityPatterns = new[]
//            {
//                @"([\u0900-\u097F]{3,30})\s*(?:नगर|गाउँ)\s*पालिका",
//                @"([\u0900-\u097F]{3,30})\s*(?:Municipality|Rural)",
//                @"(?:नगर|गाउँ)\s*पालिका\s*[:\-]?\s*([\u0900-\u097F]{3,30})"
//            };

//            foreach (var pattern in municipalityPatterns)
//            {
//                var match = Regex.Match(ocrText, pattern, RegexOptions.IgnoreCase);
//                if (match.Success)
//                {
//                    fields["municipality"] = CleanNepaliText(match.Groups[1].Value);
//                    break;
//                }
//            }

//            // Extract District (from address)
//            var districtPatterns = new[]
//            {
//                @"(?:जिल्ला|District)\s*[:\-]?\s*([\u0900-\u097F]{3,30})",
//                @"District\s*[:\-]?\s*([A-Za-z]{3,30})"
//            };

//            foreach (var pattern in districtPatterns)
//            {
//                var match = Regex.Match(ocrText, pattern, RegexOptions.IgnoreCase);
//                if (match.Success)
//                {
//                    fields["district"] = match.Groups[1].Value.Trim();
//                    break;
//                }
//            }

//            // Extract Province
//            var provinceMatch = Regex.Match(ocrText, @"(?:प्रदेश|Province)\s*(?:नं\.?|No\.?)?\s*[:\-]?\s*(\d+)", RegexOptions.IgnoreCase);
//            if (provinceMatch.Success)
//            {
//                fields["province"] = "Province " + NumberConversion.ConvertNepaliDigits(provinceMatch.Groups[1].Value);
//            }

//            // Look for thumb impression/signature indicators
//            if (ocrText.Contains("औँठा") || ocrText.Contains("Thumb") ||
//                ocrText.Contains("दस्तखत") || ocrText.Contains("Signature"))
//            {
//                fields["hasThumbImpression"] = "true";
//            }
//        }

//        private void ExtractCitizenshipNumber(string ocrText, Dictionary<string, string> fields)
//        {
//            var citizenshipPatterns = new[]
//            {
//                @"(?:नागरिकता|Citizenship).{0,20}(?:नं\.?|No\.?|संख्या)\s*[:\-]?\s*([\d\-]{8,20})",
//                @"(?:प्रमाणपत्र|Certificate).{0,20}(?:नं\.?|No\.?)\s*[:\-]?\s*([\d\-]{8,20})",
//                @"([\d]{1,2}-[\d]{1,2}-[\d]{1,2}-[\d]{4,5})",
//                @"(\d{2}-\d{2}-\d{2}-\d{5})",
//                @"नं\.?\s*[:\-]?\s*(\d{1,2}[-/]\d{1,2}[-/]\d{1,2}[-/]\d{4,5})"
//            };

//            foreach (var pattern in citizenshipPatterns)
//            {
//                var match = Regex.Match(ocrText, pattern, RegexOptions.IgnoreCase);
//                if (match.Success)
//                {
//                    string number = NumberConversion.ConvertNepaliDigits(match.Groups[1].Value);
//                    // Clean up the number
//                    number = Regex.Replace(number, @"[^\d-]", "");
//                    fields["citizenshipNumber"] = number;
//                    break;
//                }
//            }
//        }

//        private void ExtractGender(string ocrText, Dictionary<string, string> fields)
//        {
//            var genderPatterns = new[]
//            {
//                @"(?:लिङ्ग|Gender|Sex)\s*[:\-]?\s*([\u0900-\u097F]{3,10})",
//                @"(?:पुरुष|महिला|अन्य|Male|Female|Other)"
//            };

//            foreach (var pattern in genderPatterns)
//            {
//                var match = Regex.Match(ocrText, pattern, RegexOptions.IgnoreCase);
//                if (match.Success)
//                {
//                    string gender = match.Groups[1].Success ? match.Groups[1].Value : match.Value;
//                    gender = gender.Trim();

//                    if (genderMap.ContainsKey(gender))
//                        fields["gender"] = genderMap[gender];
//                    else if (gender.Equals("Male", StringComparison.OrdinalIgnoreCase))
//                        fields["gender"] = "Male";
//                    else if (gender.Equals("Female", StringComparison.OrdinalIgnoreCase))
//                        fields["gender"] = "Female";
//                    else
//                        fields["gender"] = gender;

//                    break;
//                }
//            }
//        }

//        private void ExtractDateOfBirth(string ocrText, Dictionary<string, string> fields)
//        {
//            // Pattern for BS date format
//            var dobPatterns = new[]
//            {
//                @"(?:जन्म|Birth).{0,20}(?:मिति|Date)\s*[:\-]?\s*(\d{2,4})\s*[-\s/]?\s*(\d{1,2})\s*[-\s/]?\s*(\d{1,2})",
//                @"(?:साल|Year)\s*[:\-]?\s*(\d+)\s*(?:महिना|Month)\s*[:\-]?\s*(\d+)\s*(?:गते|Day)\s*[:\-]?\s*(\d+)",
//                @"(\d{4})\s*[-/]\s*(\d{1,2})\s*[-/]\s*(\d{1,2})",
//                @"(\d{2,4})[-\s](\d{1,2})[-\s](\d{1,2})"
//            };

//            foreach (var pattern in dobPatterns)
//            {
//                var match = Regex.Match(ocrText, pattern, RegexOptions.IgnoreCase);
//                if (match.Success && match.Groups.Count >= 4)
//                {
//                    string year = NumberConversion.ConvertNepaliDigits(match.Groups[1].Value);
//                    string month = NumberConversion.ConvertNepaliDigits(match.Groups[2].Value).PadLeft(2, '0');
//                    string day = NumberConversion.ConvertNepaliDigits(match.Groups[3].Value).PadLeft(2, '0');

//                    // Validate and format year
//                    if (year.Length == 2) year = "19" + year; // Assume 1900s for 2-digit years
//                    else if (year.Length == 3) year = "1" + year;

//                    // Ensure year is reasonable (between 1900-2100)
//                    if (int.TryParse(year, out int yearNum) && yearNum > 1900 && yearNum < 2100)
//                    {
//                        fields["dateOfBirth"] = $"{year}-{month}-{day}";
//                        fields["dateOfBirthBS"] = $"{year}-{month}-{day}";
//                        break;
//                    }
//                }
//            }
//        }

//        private void ExtractIssuedDate(string ocrText, Dictionary<string, string> fields)
//        {
//            var issuedDatePatterns = new[]
//            {
//                @"(?:जारी|Issued).{0,20}(?:मिति|Date)\s*[:\-]?\s*(\d{2,4})\s*[-\s/]?\s*(\d{1,2})\s*[-\s/]?\s*(\d{1,2})",
//                @"(?:प्रमाणपत्र|Certificate).{0,20}(?:मिति|Date)\s*[:\-]?\s*(\d{2,4})\s*[-\s/]?\s*(\d{1,2})\s*[-\s/]?\s*(\d{1,2})",
//                @"Issued\s*Date\s*[:\-]?\s*(\d{4})[-/](\d{1,2})[-/](\d{1,2})"
//            };

//            foreach (var pattern in issuedDatePatterns)
//            {
//                var match = Regex.Match(ocrText, pattern, RegexOptions.IgnoreCase);
//                if (match.Success && match.Groups.Count >= 4)
//                {
//                    string year = NumberConversion.ConvertNepaliDigits(match.Groups[1].Value);
//                    string month = NumberConversion.ConvertNepaliDigits(match.Groups[2].Value).PadLeft(2, '0');
//                    string day = NumberConversion.ConvertNepaliDigits(match.Groups[3].Value).PadLeft(2, '0');

//                    fields["citizenshipIssuedDate"] = $"{year}-{month}-{day}";
//                    break;
//                }
//            }

//            // Try to extract from English month names
//            var englishDateMatch = Regex.Match(ocrText,
//                @"(?:Year|Yr)[:\s]*(\d{4}).{0,10}(?:Month|Mon)[:\s]*([A-Za-z]{3,9}).{0,10}(?:Day)[:\s]*(\d{1,2})",
//                RegexOptions.IgnoreCase);

//            if (englishDateMatch.Success)
//            {
//                string year = englishDateMatch.Groups[1].Value;
//                string monthName = englishDateMatch.Groups[2].Value;
//                string day = englishDateMatch.Groups[3].Value.PadLeft(2, '0');

//                string month = ConvertEnglishMonthToNumber(monthName);
//                fields["citizenshipIssuedDate"] = $"{year}-{month}-{day}";
//            }
//        }

//        private void ExtractNames(string ocrText, Dictionary<string, string> fields)
//        {
//            // Extract Father's Name
//            var fatherPatterns = new[]
//            {
//                @"(?:पिता|Father).{0,20}(?:को नाम|'s Name|Name)\s*[:\-]?\s*([\u0900-\u097F\s]{3,50})",
//                @"(?:पिता|Father)\s*[:\-]?\s*([\u0900-\u097F\s]{3,50})"
//            };

//            foreach (var pattern in fatherPatterns)
//            {
//                var match = Regex.Match(ocrText, pattern, RegexOptions.IgnoreCase);
//                if (match.Success)
//                {
//                    fields["fatherName"] = CleanNepaliText(match.Groups[1].Value);
//                    break;
//                }
//            }

//            // Extract Mother's Name
//            var motherPatterns = new[]
//            {
//                @"(?:माता|Mother).{0,20}(?:को नाम|'s Name|Name)\s*[:\-]?\s*([\u0900-\u097F\s]{3,50})",
//                @"(?:माता|Mother)\s*[:\-]?\s*([\u0900-\u097F\s]{3,50})"
//            };

//            foreach (var pattern in motherPatterns)
//            {
//                var match = Regex.Match(ocrText, pattern, RegexOptions.IgnoreCase);
//                if (match.Success)
//                {
//                    fields["motherName"] = CleanNepaliText(match.Groups[1].Value);
//                    break;
//                }
//            }
//        }

//        private void ExtractDates(string ocrText, Dictionary<string, string> fields)
//        {
//            // Look for any date pattern that might be useful
//            var dateMatches = Regex.Matches(ocrText, @"(\d{2,4})[-/\s](\d{1,2})[-/\s](\d{1,2})");

//            int dateIndex = 0;
//            foreach (Match match in dateMatches)
//            {
//                if (match.Success && match.Groups.Count >= 4)
//                {
//                    string year = NumberConversion.ConvertNepaliDigits(match.Groups[1].Value);
//                    string month = NumberConversion.ConvertNepaliDigits(match.Groups[2].Value).PadLeft(2, '0');
//                    string day = NumberConversion.ConvertNepaliDigits(match.Groups[3].Value).PadLeft(2, '0');

//                    fields[$"date_{dateIndex++}"] = $"{year}-{month}-{day}";
//                }
//            }
//        }

//        private void ExtractPassportFields(string ocrText, Dictionary<string, string> fields)
//        {
//            // Extract Passport Number
//            var passportMatch = Regex.Match(ocrText, @"(?:Passport No|PASSPORT NO|राहदानी नं)[:\s]*([A-Z0-9]{6,12})", RegexOptions.IgnoreCase);
//            if (passportMatch.Success)
//                fields["passportNumber"] = passportMatch.Groups[1].Value;

//            // Extract Name
//            var nameMatch = Regex.Match(ocrText, @"(?:Name|Surname|Given Names)[:\s]*([A-Z\s]+)", RegexOptions.IgnoreCase);
//            if (nameMatch.Success)
//                fields["fullName"] = nameMatch.Groups[1].Value.Trim();

//            // Extract Nationality
//            var nationalityMatch = Regex.Match(ocrText, @"(?:Nationality|राष्ट्रियता)[:\s]*([A-Za-z]+)", RegexOptions.IgnoreCase);
//            if (nationalityMatch.Success)
//                fields["nationality"] = nationalityMatch.Groups[1].Value;
//        }

//        private void ExtractDrivingLicenseFields(string ocrText, Dictionary<string, string> fields)
//        {
//            // Extract License Number
//            var licenseMatch = Regex.Match(ocrText, @"(?:License No|अनुमतिपत्र नं)[:\s]*([A-Z0-9-]{6,15})", RegexOptions.IgnoreCase);
//            if (licenseMatch.Success)
//                fields["licenseNumber"] = licenseMatch.Groups[1].Value;

//            // Extract Vehicle Categories
//            var categoriesMatch = Regex.Match(ocrText, @"(?:Categories|श्रेणी)[:\s]*([A-Z, ]+)", RegexOptions.IgnoreCase);
//            if (categoriesMatch.Success)
//                fields["vehicleCategories"] = categoriesMatch.Groups[1].Value;
//        }

//        private string ConvertEnglishMonthToNumber(string monthName)
//        {
//            if (string.IsNullOrEmpty(monthName)) return "01";

//            // Take first 3 characters and uppercase
//            string key = monthName.Length >= 3 ? monthName.Substring(0, 3).ToUpper() : monthName.ToUpper();

//            return englishMonthMap.ContainsKey(key) ? englishMonthMap[key] : "01";
//        }

//        private string CleanNepaliText(string text)
//        {
//            if (string.IsNullOrEmpty(text)) return "";

//            // Remove non-Nepali characters and extra spaces
//            text = Regex.Replace(text, @"[^\u0900-\u097F\s]", " ");
//            text = Regex.Replace(text, @"\s+", " ");

//            // Remove common OCR artifacts
//            text = text.Replace("|", "").Replace("•", "").Replace("·", "");

//            return text.Trim();
//        }
//    }
//}