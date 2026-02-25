namespace Smart_ward_management_system.Model.Services.ProbableServices
{
    public class PropertyDocumentRequest : ServiceRequest
    {
        public string PlotNumber { get; set; }
        public string SheetNumber { get; set; }
        public double TotalArea { get; set; }
        public string PropertyType { get; set; } // Agricultural, Residential, Commercial
        public string CurrentOwnerName { get; set; }
        public string LandRevenueReceiptNumber { get; set; }
    }
}
