namespace Smart_ward_management_system.Model.WasteManagement_And_Scheduling
{
    public enum WasteType
    {
        General = 1,
        Recyclable = 2,
        Hazardous = 3,
        Biomedical = 4,
        Organic = 5
    }

    public enum RouteStatus
    {
        Planned = 1,
        InProgress = 2,
        Completed = 3,
        Delayed = 4,
        Cancelled = 5
    }

    public enum VehicleStatus
    {
        Available = 1,
        InUse = 2,
        Maintenance = 3,
        OutOfService = 4
    }
}
