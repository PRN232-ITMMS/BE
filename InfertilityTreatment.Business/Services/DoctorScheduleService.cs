using AutoMapper;
using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.DoctorSchedules;
using InfertilityTreatment.Entity.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Services
{
    public class DoctorScheduleService : IDoctorScheduleService
    {
        private readonly IDoctorScheduleRepository _repo;
        private readonly IAppointmentRepository _appointmentRepo;
        private readonly IMapper _mapper;

        public DoctorScheduleService(IDoctorScheduleRepository repo, IAppointmentRepository appointmentRepo, IMapper mapper)
        {
            _repo = repo;
            _appointmentRepo = appointmentRepo;
            _mapper = mapper;
        }

        public async Task<PaginatedResultDto<DoctorScheduleDto>> GetAllAsync(PaginationQueryDTO pagination)
        {
            var result = await _repo.GetAll(pagination);
            return new PaginatedResultDto<DoctorScheduleDto>(
                _mapper.Map<List<DoctorScheduleDto>>(result.Items),
                result.TotalCount,
                result.PageNumber,
                result.PageSize
            );
        }

        public async Task<DoctorScheduleDto?> GetByIdAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<DoctorScheduleDto>(entity);
        }

        public async Task<PaginatedResultDto<DoctorScheduleDto>> GetByDoctorIdAsync(int doctorId, PaginationQueryDTO pagination)
        {
            var result = await _repo.GetByDoctorIdAsync(doctorId, pagination);
            return new PaginatedResultDto<DoctorScheduleDto>(
                _mapper.Map<List<DoctorScheduleDto>>(result.Items),
                result.TotalCount,
                result.PageNumber,
                result.PageSize
            );
        }

        public async Task<DoctorScheduleDto> CreateAsync(CreateDoctorScheduleDto dto)
        {
            var entity = _mapper.Map<DoctorSchedule>(dto);
            var created = await _repo.CreateAsync(entity);
            return _mapper.Map<DoctorScheduleDto>(created);
        }

        public async Task<DoctorScheduleDto> UpdateAsync(int id, UpdateDoctorScheduleDto dto)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) throw new KeyNotFoundException("DoctorSchedule not found");
            _mapper.Map(dto, entity);
            await _repo.UpdateAsync(entity);
            return _mapper.Map<DoctorScheduleDto>(entity);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repo.DeleteAsync(id);
        }

        public async Task<PaginatedResultDto<DoctorScheduleDto>> GetAvailableSlotsAsync(int doctorId, DateTime date, PaginationQueryDTO pagination)
        {
            var allSlots = await _repo.GetSchedulesByDoctorAndDateAsync(doctorId, date);
            var bookedAppointments = await _appointmentRepo.GetByDoctorAndDateAsync(doctorId, date);
            var bookedScheduleIds = bookedAppointments.Select(a => a.DoctorScheduleId).ToHashSet();
            var availableSlots = allSlots.Where(slot => !bookedScheduleIds.Contains(slot.Id)).ToList();
            var totalCount = availableSlots.Count;
            var pagedSlots = availableSlots.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
            return new PaginatedResultDto<DoctorScheduleDto>(
                _mapper.Map<List<DoctorScheduleDto>>(pagedSlots),
                totalCount,
                pagination.PageNumber,
                pagination.PageSize
            );
        }

        public async Task<PaginatedResultDto<DoctorScheduleDto>> GetDoctorAvailabilityAsync(int doctorId, DateTime date, PaginationQueryDTO pagination)
        {
            var entityResult = await _appointmentRepo.GetDoctorAvailabilityAsync(doctorId, date, pagination);
            return new PaginatedResultDto<DoctorScheduleDto>(
                _mapper.Map<List<DoctorScheduleDto>>(entityResult.Items),
                entityResult.TotalCount,
                entityResult.PageNumber,
                entityResult.PageSize
            );
        }
    }
}
