using AutoMailerBackend.Data;
using AutoMailerBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoMailerBackend.Services;

public class CustomersService
{
    private readonly AppDbContext _db;

    public CustomersService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Customer>> GetAllAsync()
    {
        return await _db.Customers.ToListAsync();
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _db.Customers.FindAsync(id);
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return customer;
    }

    public async Task<Customer?> UpdateAsync(int id, Action<Customer> applyUpdates)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null)
            return null;

        applyUpdates(customer);
        await _db.SaveChangesAsync();
        return customer;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null)
            return false;

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();
        return true;
    }
}
