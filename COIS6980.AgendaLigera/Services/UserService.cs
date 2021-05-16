using COIS6980.AgendaLigera.Models.User;
using COIS6980.EFCoreDb.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace COIS6980.AgendaLigera.Services
{
    public interface IUserService
    {
        Task<bool> UserCompletedRegistration(string userId);
        Task<List<UserRole>> GetUserRoles();
        Task AddUserData(string roleId, string userId, string firstName, string lastName);
    }
    public class UserService : IUserService
    {
        private readonly AgendaLigeraContext _agendaLigeraCtx;
        public UserService(AgendaLigeraContext agendaLigeraCtx)
        {
            _agendaLigeraCtx = agendaLigeraCtx;
        }

        public async Task<bool> UserCompletedRegistration(string userId)
        {
            var userData = await _agendaLigeraCtx.AspNetUserRoles
                .Include(x => x.Role)
                .Where(x => x.UserId == userId)
                .FirstOrDefaultAsync();

            return !string.IsNullOrEmpty(userData?.UserId ?? string.Empty);
        }

        public async Task<List<UserRole>> GetUserRoles()
        {
            var rolesFound = await _agendaLigeraCtx.AspNetRoles
                .Where(x => !string.IsNullOrEmpty(x.Name))
                .ToListAsync();

            var userRoles = rolesFound
                .Select(x => new UserRole()
                {
                    RoleId = x.Id,
                    RoleName = x.Name
                })
                .OrderByDescending(x => x.RoleName)
                .ToList();

            return userRoles;
        }

        public async Task AddUserData(string roleId, string userId, string firstName, string lastName)
        {
            var userRole = new AspNetUserRole()
            {
                UserId = userId,
                RoleId = roleId
            };

            await _agendaLigeraCtx.AddAsync(userRole);

            var roleName = await GetRoleName(roleId);

            switch (roleName.ToLowerInvariant())
            {
                case "paciente":
                    await AddServiceRecipient(userId, firstName, lastName);
                    break;
                default:
                    await AddEmployee(userId, firstName, lastName);
                    break;
            }

            await _agendaLigeraCtx.SaveChangesAsync();
        }

        private async Task<string> GetRoleName(string roleId)
        {
            var roleName = await _agendaLigeraCtx.AspNetRoles
                .Where(x => x.Id == roleId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();

            return roleName;
        }

        private async Task AddServiceRecipient(string userId, string firstName, string lastName)
        {
            var serviceRecipient = new ServiceRecipient()
            {
                UserId = userId,
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                IsActive = true,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow.ToLocalTime()
            };

            await _agendaLigeraCtx.AddAsync(serviceRecipient);
        }

        private async Task AddEmployee(string userId, string firstName, string lastName)
        {
            var employee = new Employee()
            {
                UserId = userId,
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                IsActive = true,
                IsDeleted = false
            };

            await _agendaLigeraCtx.AddAsync(employee);
        }
    }
}
