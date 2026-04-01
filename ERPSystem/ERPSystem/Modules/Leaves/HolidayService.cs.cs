using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Leaves.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace ERPSystem.Modules.Leaves
{
    public class HolidayService
    {
        private readonly ApplicationDbContext _context;

        public HolidayService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<DateTime>> GetHolidays(int year)
        {
            var holidays = await _context.PublicHolidays
                .Where(h => h.Date.Year == year)
                .Select(h => h.Date.Date)
                .ToListAsync();

            if (holidays.Any())
                return holidays;

            using var http = new HttpClient();

            var url = $"https://date.nager.at/api/v3/PublicHolidays/{year}/RO";

            var response = await http.GetFromJsonAsync<List<NagerHoliday>>(url);

            if (response == null)
                return new List<DateTime>();

            var entities = response.Select(x => new PublicHoliday
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Parse(x.date),
                Name = x.localName,
                Country = "RO"
            }).ToList();

            _context.PublicHolidays.AddRange(entities);
            await _context.SaveChangesAsync();

            return entities.Select(x => x.Date.Date).ToList();
        }
    }
}
