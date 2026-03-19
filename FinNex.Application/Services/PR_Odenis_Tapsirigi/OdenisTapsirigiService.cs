using AutoMapper;
using FinNex.Application.DTOs.PR_Odenis_Tapsirigi.OdenisTapsirigi;
using FinNex.Application.Interfaces.PR_Odenis_Tapsirigi;
using FinNex.DataAccess.Contexts;
using FinNex.Domain.Entities.PR_Odenis_Tapsirigi;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinNex.Application.Services.PR_Odenis_Tapsirigi
{
    public class OdenisTapsirigiService : ServiceAsync<OdenisTapsirigi, OdenisTapsirigiDetailDto, OdenisTapsirigiCreateDto, OdenisTapsirigiUpdateDto>, IOdenisTapsirigiService
    {
        private readonly AppDbContext _appDbContext;
        public OdenisTapsirigiService(IUnitOfWork unitOfWork, IMapper mapper, AppDbContext appDbContext) : base(unitOfWork, mapper)
        {
            _appDbContext = appDbContext;
        }

        // Nümunə: Əgər xüsusi bir məntiq lazımdırsa bura əlavə edilir
        // public async Task TesdiqleAsync(int id) { ... }

        
        public async Task<DateTime> DbTarixiniGetirAsync()
        {
            return await _appDbContext.Database
                .SqlQuery<DateTime>($"SELECT CAST(GETDATE() AS DATE) AS Value")
                .FirstAsync();
        }
    }
}
