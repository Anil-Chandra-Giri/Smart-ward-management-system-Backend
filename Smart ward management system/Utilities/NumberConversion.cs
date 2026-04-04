using System.Text.RegularExpressions;

namespace OCR.Utilities
{
    public static class NumberConversion
    {
        private static readonly Dictionary<char, char> NepaliToEnglishDigits = new()
        {
            { '०', '0' }, { '१', '1' }, { '२', '2' }, { '३', '3' },
            { '४', '4' }, { '५', '5' }, { '६', '6' }, { '७', '7' },
            { '८', '8' }, { '९', '9' }
        };

        public static string ConvertNepaliDigits(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            char[] chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (NepaliToEnglishDigits.ContainsKey(chars[i]))
                    chars[i] = NepaliToEnglishDigits[chars[i]];
            }
            return new string(chars);
        }

        public static string ConvertToNepaliDigits(string input)
        {
            var englishToNepali = NepaliToEnglishDigits.ToDictionary(x => x.Value, x => x.Key);

            if (string.IsNullOrEmpty(input)) return input;

            char[] chars = input.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (englishToNepali.ContainsKey(chars[i]))
                    chars[i] = englishToNepali[chars[i]];
            }
            return new string(chars);
        }

        public static string ExtractNumber(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            // First convert any Nepali digits
            text = ConvertNepaliDigits(text);

            // Then extract only digits and hyphens
            var match = Regex.Match(text, @"[\d-]+");
            return match.Success ? match.Value : string.Empty;
        }
    }
}
