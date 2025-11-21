using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CMCS_Prototype.Data;
using CMCS_Prototype.Models;

namespace CMCS_Prototype.Services
{
    public class ReportingService
    {
        private readonly CMCSDbContext _context;

        public ReportingService(CMCSDbContext context)
        {
            _context = context;
        }  

        public async Task<List<Claim>> GenerateInvoiceAsync(int month, int year)
        {
            return await _context.Claims
                .Where(c => c.DateSubmitted.Month == month && c.DateSubmitted.Year == year)
                .Include(c => c.Lecturer)
                .Where(c => c.Status == "Settled")
                .ToListAsync();
        }
    }
}