using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThuocBenhVien.Data;
using QLThuocBenhVien.Models;

namespace QLThuocBenhVien.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class NhaCungCapApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NhaCungCapApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/NhaCungCapApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NhaCungCap>>> GetNhaCungCaps()
        {
            return await _context.NhaCungCap.ToListAsync();
        }

        // GET: api/NhaCungCapApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<NhaCungCap>> GetNhaCungCap(int id)
        {
            var nhaCungCap = await _context.NhaCungCap.FindAsync(id);

            if (nhaCungCap == null)
            {
                return NotFound();
            }

            return nhaCungCap;
        }
    }
}