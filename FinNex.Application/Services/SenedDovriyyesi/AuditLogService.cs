using AutoMapper;
using FinNex.Application.Common.Results;
using FinNex.Application.DTOs.SenedDovriyyesi;
using FinNex.Application.DTOs.SenedDovriyyesi.Sened;
using FinNex.Application.Interfaces.SenedDovriyyesi;
using FinNex.Domain.Entities.SenedDovriyyesi;
using FinNex.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FinNex.Application.Services.SenedDovriyyesi
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public AuditLogService(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        public async Task WriteAsync(int userId, string action, int? senedId, string? ip, object? details = null)
        {
            string? json = null;

            if (details != null)
            {
                json = JsonSerializer.Serialize(details);
            }

            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                SenedId = senedId,
                Ip = ip,
                DetailsJson = json,
                YaradanIcraciId = userId
            };

            await _uow.Repository<AuditLog>().YaratAsync(log);
            await _uow.YaddaSaxlaAsync();
        }

        public async Task<List<AuditLogDto>> GetBySenedIdAsync(int senedId)
        {
            var logs = await _uow.Repository<AuditLog>().Query()
                .Where(x => x.SenedId == senedId && !x.Silinib)
                .OrderByDescending(x => x.YaradilmaTarixi)
                .ToListAsync();

            return _mapper.Map<List<AuditLogDto>>(logs);
        }
       

    }
}
