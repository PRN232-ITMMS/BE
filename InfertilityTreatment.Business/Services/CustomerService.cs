﻿using AutoMapper;
using InfertilityTreatment.Business.Interfaces;
using InfertilityTreatment.Data.Repositories.Implementations;
using InfertilityTreatment.Data.Repositories.Interfaces;
using InfertilityTreatment.Entity.DTOs.Common;
using InfertilityTreatment.Entity.DTOs.Users;
using InfertilityTreatment.Entity.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfertilityTreatment.Business.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IMapper _mapper;
        public CustomerService(ICustomerRepository customerRepository,IMapper mapper)
        {
            _customerRepository = customerRepository;
            _mapper = mapper;
        }
        public async Task<CustomerDetailDto?> GetCustomerProfileAsync(int customerId)
        {
            var customer = await _customerRepository.GetWithUserAsync(customerId);

            if (customer == null)
            {
                throw new KeyNotFoundException($"Customer with ID {customerId} not found.");
            }
            var customerProfileDto =  _mapper.Map<CustomerDetailDto>(customer);
            return customerProfileDto;
        }

        public async Task<string> UpdateCustomerProfileAsync(int customerId, CustomerProfileDto customerProfileDto)
        {
            var updatedCustomer = await _customerRepository.UpdateCustomerProfileAsync(customerId, customerProfileDto);
            if (updatedCustomer == null)
            {
                throw new KeyNotFoundException($"Customer with ID {customerId} not found.");
            }
            return "Customer profile updated successfully.";
        }


        public async Task<string> UpdateMedicalHistoryAsync(int customerId, string medicalHistory)
        {
            var customer = await _customerRepository.UpdateMedicalHistoryAsync(customerId,medicalHistory);
            if (customer == null)
            {
                throw new KeyNotFoundException($"Customer with ID {customerId} not found.");
            }
            return "Customer profile updated successfully.";
        }
        public async Task<string> CheckCustomerWithMedicalHistoryAsync(int customerId)
        {
            var customer = await _customerRepository.GetWithMedicalHistoryAsync(customerId);
            if (customer == null)
            {
                throw new KeyNotFoundException($"Customer with ID {customerId} not found.");
            }
            return "Customer exists with medical history.";
        }

        public async Task<PaginatedResultDto<CustomerProfileDto>> GetCustomersAsync(CustomerFilterDto filter)
        {
            var pagedResult = await _customerRepository.GetCustomers(filter);

            if (pagedResult == null || !pagedResult.Items.Any())
            {
                throw new KeyNotFoundException("No users found.");
            }

            var profileDtos = _mapper.Map<List<CustomerProfileDto>>(pagedResult.Items);

            return new PaginatedResultDto<CustomerProfileDto>(
                profileDtos,
                pagedResult.TotalCount,
                pagedResult.PageNumber,
                pagedResult.PageSize
            );
        }
    }
}
