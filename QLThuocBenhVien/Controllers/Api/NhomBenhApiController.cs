using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThuocBenhVien.Data;
using QLThuocBenhVien.Models;

namespace QLThuocBenhVien.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class NhomBenhApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NhomBenhApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/NhomBenhApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NhomBenh>>> GetNhomBenhs()
        {
            return await _context.NhomBenh.ToListAsync();
        }

        // POST: api/NhomBenhApi
        [HttpPost]
        public async Task<ActionResult<NhomBenh>> PostNhomBenh(NhomBenh nhomBenh)
        {
            _context.NhomBenh.Add(nhomBenh);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetNhomBenh", new { id = nhomBenh.MaNhomBenh }, nhomBenh);
        }

        // DELETE: api/NhomBenhApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNhomBenh(int id)
        {
            var nhomBenh = await _context.NhomBenh.FindAsync(id);
            if (nhomBenh == null)
            {
                return NotFound();
            }

            _context.NhomBenh.Remove(nhomBenh);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}