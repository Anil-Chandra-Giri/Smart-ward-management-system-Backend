using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;

namespace Smart_ward_management_system.Common
{
    public class DocumentService
    {
        private readonly ApplicationDbContext _context;

        public DocumentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateDocumentNumber(string wardNumber, string serviceCode)
        {
            // 1. Get Current Nepali Fiscal Year (e.g., 80/81)
            string fiscalYear = GetNepaliFiscalYear();

            // 2. Find the last sequence number for this ward and service in this fiscal year
            var lastDocument = await _context.Documents
                .Where(d => d.DocumentNumber.Contains($"/ {serviceCode} / {fiscalYear} /"))
                .OrderByDescending(d => d.CreatedOn)
                .FirstOrDefaultAsync();

            int newSequence = 1;

            if (lastDocument != null)
            {
                // Extract the last 4 digits and increment
                string lastPart = lastDocument.DocumentNumber.Split('/').Last();
                if (int.TryParse(lastPart, out int lastSeq))
                {
                    newSequence = lastSeq + 1;
                }
            }

            // 3. Format: WARD01/CIT/80-81/0001
            return $"WARD{wardNumber.PadLeft(2, '0')}/{serviceCode}/{fiscalYear}/{newSequence.ToString().PadLeft(4, '0')}";
        }

        private string GetNepaliFiscalYear()
        {
            // Simple logic: If current month > July (4 in Nepali Calendar context), it's the new FY
            // This is a simplified version; you can use a Nepali Calendar library for precision
            int year = DateTime.Now.Year - 1943; // Rough BS conversion
            int month = DateTime.Now.Month;

            if (month >= 7) // Shrawan starts around mid-July
                return $"{year}/{year + 1}";
            else
                return $"{year - 1}/{year}";
        }
    }
}
