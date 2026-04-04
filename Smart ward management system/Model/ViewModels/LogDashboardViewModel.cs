// Models/ViewModels/LogDashboardViewModel.cs
using Smart_ward_management_system.Model;
using System;
using System.Collections.Generic;

namespace Smart_ward_management_system.Models
{
    public class LogDashboardViewModel
    {
        // Summary statistics
        public int TotalLogsToday { get; set; }
        public int ErrorCount24h { get; set; }
        public int WarningCount24h { get; set; }
        public int InfoCount24h { get; set; }

        // Citizen services
        public int CitizenServiceRequests { get; set; }
        public int GrievancesFiled { get; set; }
        public int GrievancesResolved { get; set; }
        public int GrievancesInProgress { get; set; }

        // Financial
        public decimal TotalTaxCollected { get; set; }
        public int TotalPaymentsToday { get; set; }

        // Service requests
        public int TotalServiceRequests { get; set; }
        public int PendingServiceRequests { get; set; }
        public int ApprovedServiceRequests { get; set; }
        public int RejectedServiceRequests { get; set; }

        // Charts data
        public Dictionary<LogCategory, int> LogsByCategory { get; set; } = new();
        public Dictionary<string, int> LogsByHour { get; set; } = new();
        public Dictionary<string, int> TopActiveDepartments { get; set; } = new();
        public Dictionary<string, int> ComplaintsByWard { get; set; } = new();
        public Dictionary<string, int> ServiceRequestsByType { get; set; } = new();

        // Recent activity
        public List<LogEntry> RecentErrors { get; set; } = new();
        public List<LogEntry> RecentWarnings { get; set; } = new();
        public List<LogEntry> RecentCitizenActions { get; set; } = new();
        public List<LogEntry> RecentComplaints { get; set; } = new();

        // Time-based statistics
        public int LogsLastHour { get; set; }
        public int LogsLast6Hours { get; set; }
        public int LogsLast24Hours { get; set; }
        public int LogsLast7Days { get; set; }

        // Performance metrics
        public double AverageResponseTime { get; set; }
        public int ActiveUsersToday { get; set; }

        // Alert indicators
        public bool HasHighErrorRate => ErrorCount24h > 50;
        public bool HasPendingGrievances => GrievancesFiled > GrievancesResolved;

        // Helper methods
        public double GetResolutionRate()
        {
            if (GrievancesFiled == 0) return 0;
            return Math.Round((double)GrievancesResolved / GrievancesFiled * 100, 2);
        }

        public double GetServiceRequestApprovalRate()
        {
            if (TotalServiceRequests == 0) return 0;
            return Math.Round((double)ApprovedServiceRequests / TotalServiceRequests * 100, 2);
        }

        public string GetHealthStatus()
        {
            if (ErrorCount24h > 100) return "Critical";
            if (ErrorCount24h > 50) return "Warning";
            if (ErrorCount24h > 10) return "Degraded";
            return "Healthy";
        }
    }
}