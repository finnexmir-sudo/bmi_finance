using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.HR.Dashboard;


namespace FinNex.Application.Interfaces
{
    /// <summary>
    /// İşçi dashboard-u üçün bütün məlumatları toplayıb vahid DTO qaytaran servis.
    /// Generic ServiceAsync-dən ayrıdır — çünki bir neçə entity-dən data toplayır.
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Username əsasında işçinin dashboard məlumatlarını qaytarır.
        /// Controller-də User.Identity.Name ilə çağırılır.
        /// </summary>
        Task<Result<UserDashboardDto>> GetDashboardAsync(string username);
    }
}