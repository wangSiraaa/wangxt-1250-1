using Microsoft.EntityFrameworkCore;
using TowerCraneSafetyMonitor.Application.Interfaces;
using TowerCraneSafetyMonitor.Domain.Entities;
using TowerCraneSafetyMonitor.Domain.Enums;
using TowerCraneSafetyMonitor.Infrastructure.Data;

namespace TowerCraneSafetyMonitor.Application.Services;

public class PersonService : IPersonService
{
    private readonly AppDbContext _context;

    public PersonService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Person>> GetAllAsync()
    {
        return await _context.Persons
            .Include(p => p.Certificates)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Person?> GetByIdAsync(int id)
    {
        return await _context.Persons
            .Include(p => p.Certificates)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Person> CreateAsync(Person person)
    {
        person.CreatedAt = DateTime.Now;
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();
        return person;
    }

    public async Task<Person> UpdateAsync(Person person)
    {
        person.UpdatedAt = DateTime.Now;
        _context.Entry(person).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await PersonExistsAsync(person.Id))
                throw new KeyNotFoundException($"Person with id {person.Id} not found");
            throw;
        }
        return person;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var person = await _context.Persons.FindAsync(id);
        if (person == null) return false;
        person.IsActive = false;
        person.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CheckDriverQualificationAsync(int driverId)
    {
        return !(await GetDriverQualificationIssuesAsync(driverId)).Any();
    }

    public async Task<List<string>> GetDriverQualificationIssuesAsync(int driverId)
    {
        var issues = new List<string>();
        var driver = await _context.Persons
            .Include(p => p.Certificates)
            .FirstOrDefaultAsync(p => p.Id == driverId);

        if (driver == null)
        {
            issues.Add("司机信息不存在");
            return issues;
        }

        if (driver.Role != UserRole.Driver)
        {
            issues.Add("该人员不是司机角色");
        }

        if (driver.DriverStatus == DriverStatus.Suspended)
        {
            issues.Add("司机处于停职状态");
        }

        var certs = driver.Certificates
            .Where(c => c.CertificateType == CertificateType.TowerCraneOperator && c.IsActive)
            .ToList();

        if (!certs.Any())
        {
            issues.Add("缺少塔吊操作证");
        }
        else
        {
            var expiredCerts = certs.Where(c => c.IsExpired).ToList();
            if (expiredCerts.Any())
            {
                issues.Add($"塔吊操作证已过期（过期日期：{expiredCerts.First().ExpiryDate:yyyy-MM-dd}）");
            }

            var expiringSoon = certs.Where(c => !c.IsExpired && c.DaysUntilExpiry <= 30).ToList();
            if (expiringSoon.Any())
            {
                issues.Add($"塔吊操作证将在{expiringSoon.First().DaysUntilExpiry}天内过期");
            }
        }

        var specialCerts = driver.Certificates
            .Where(c => c.CertificateType == CertificateType.SpecialEquipment && c.IsActive)
            .ToList();

        if (specialCerts.Any())
        {
            var expired = specialCerts.Where(c => c.IsExpired).ToList();
            if (expired.Any())
            {
                issues.Add("特种设备作业人员证已过期");
            }
        }

        return issues;
    }

    public async Task<IEnumerable<Person>> GetDriversAsync()
    {
        return await _context.Persons
            .Include(p => p.Certificates)
            .Where(p => p.Role == UserRole.Driver && p.IsActive)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Person>> GetSafetyOfficersAsync()
    {
        return await _context.Persons
            .Include(p => p.Certificates)
            .Where(p => p.Role == UserRole.SafetyOfficer && p.IsActive)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Person>> GetSupervisorsAsync()
    {
        return await _context.Persons
            .Include(p => p.Certificates)
            .Where(p => p.Role == UserRole.Supervisor && p.IsActive)
            .AsNoTracking()
            .ToListAsync();
    }

    private async Task<bool> PersonExistsAsync(int id)
    {
        return await _context.Persons.AnyAsync(p => p.Id == id);
    }
}
