namespace OCR.Utilities
{
    public class NumberConversion
    {
        public static string ConvertNepaliDigits(string input)
        {
            var map = new Dictionary<char, char>()
            {
                ['०'] = '0',
                ['१'] = '1',
                ['२'] = '2',
                ['३'] = '3',
                ['४'] = '4',
                ['५'] = '5',
                ['६'] = '6',
                ['७'] = '7',
                ['८'] = '8',
                ['९'] = '9'
            };

            foreach (var kv in map)
                input = input.Replace(kv.Key, kv.Value);

            return input;
        }
    }
}
