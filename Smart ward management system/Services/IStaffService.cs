using Smart_ward_management_system.DTOs.Staff;

namespace Smart_ward_management_system.Services.Staff
{
    public interface IStaffService
    {
        Task<List<StaffListDto>> GetAllAsync(string? role, string? wardNumber, string? search);
        Task<StaffListDto?> GetByIdAsync(Guid userId);
        Task<(StaffListDto staff, StaffCredentialsDto credentials)> CreateAsync(CreateStaffDto dto, Guid adminUserId);
        Task<bool> UpdateAsync(Guid userId, UpdateStaffDto dto);
        Task<bool> DeleteAsync(Guid userId);
        Task<StaffCredentialsDto?> ResetPasswordAsync(Guid userId);
        Task<bool> SetAccountStatusAsync(Guid userId, string accountStatus);
    }
}