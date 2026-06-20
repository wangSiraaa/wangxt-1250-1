using Microsoft.EntityFrameworkCore;
using TowerCraneSafetyMonitor.Application.Interfaces;
using TowerCraneSafetyMonitor.Domain.Entities;
using TowerCraneSafetyMonitor.Domain.Enums;
using TowerCraneSafetyMonitor.Infrastructure.Data;

namespace TowerCraneSafetyMonitor.Application.Services;

public class TowerCraneService : ITowerCraneService
{
    private readonly AppDbContext _context;

    public TowerCraneService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TowerCrane>> GetAllAsync()
    {
        return await _context.TowerCranes
            .Include(tc => tc.Alarms)
            .Include(tc => tc.Rectifications)
            .Include(tc => tc.Tasks)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<TowerCrane?> GetByIdAsync(int id)
    {
        return await _context.TowerCranes
            .Include(tc => tc.Alarms)
            .Include(tc => tc.Rectifications)
            .Include(tc => tc.Tasks)
            .AsNoTracking()
            .FirstOrDefaultAsync(tc => tc.Id == id);
    }

    public async Task<TowerCrane> CreateAsync(TowerCrane towerCrane)
    {
        towerCrane.CreatedAt = DateTime.Now;
        _context.TowerCranes.Add(towerCrane);
        await _context.SaveChangesAsync();
        return towerCrane;
    }

    public async Task<TowerCrane> UpdateAsync(TowerCrane towerCrane)
    {
        towerCrane.UpdatedAt = DateTime.Now;
        _context.Entry(towerCrane).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await TowerCraneExistsAsync(towerCrane.Id))
                throw new KeyNotFoundException($"TowerCrane with id {towerCrane.Id} not found");
            throw;
        }
        return towerCrane;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var crane = await _context.TowerCranes.FindAsync(id);
        if (crane == null) return false;
        crane.IsActive = false;
        crane.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CanExecuteRiskLevelTaskAsync(int towerCraneId, TaskRiskLevel riskLevel)
    {
        var hasOpenRectification = await HasOpenRectificationAsync(towerCraneId);
        if (hasOpenRectification && riskLevel != TaskRiskLevel.Low)
        {
            return false;
        }
        return true;
    }

    public async Task<bool> HasPendingCriticalAlarmsAsync(int towerCraneId)
    {
        return await _context.Alarms.AnyAsync(a =>
            a.TowerCraneId == towerCraneId &&
            a.AlarmLevel == AlarmLevel.Critical &&
            a.Status != AlarmStatus.Resolved);
    }

    public async Task<bool> HasOpenRectificationAsync(int towerCraneId)
    {
        return await _context.Rectifications.AnyAsync(r =>
            r.TowerCraneId == towerCraneId &&
            r.Status != RectificationStatus.Closed);
    }

    public async Task UpdateStatusAsync(int id, TowerCraneStatus status)
    {
        var crane = await _context.TowerCranes.FindAsync(id);
        if (crane == null) throw new KeyNotFoundException($"TowerCrane with id {id} not found");
        
        crane.Status = status;
        crane.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    private async Task<bool> TowerCraneExistsAsync(int id)
    {
        return await _context.TowerCranes.AnyAsync(tc => tc.Id == id);
    }
}
