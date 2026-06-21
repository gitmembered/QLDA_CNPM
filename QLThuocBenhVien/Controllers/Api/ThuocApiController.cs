using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThuocBenhVien.Data;
using QLThuocBenhVien.Models;

namespace QLThuocBenhVien.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThuocApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ThuocApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ThuocApi
        // Lấy danh sách toàn bộ thuốc
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Thuoc>>> GetThuocs()
        {
            var danhSachThuoc = await _context.Thuoc
                .Include(t => t.ThuocNhomBenhs)
                .ThenInclude(tnb => tnb.NhomBenh)
                .ToListAsync();

            return Ok(danhSachThuoc);
        }

        // GET: api/ThuocApi/5
        // Lấy thông tin 1 loại thuốc theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Thuoc>> GetThuoc(int id)
        {
            var thuoc = await _context.Thuoc
                .Include(t => t.ThuocNhomBenhs)
                .FirstOrDefaultAsync(t => t.MaThuoc == id);

            if (thuoc == null)
            {
                return NotFound(new { message = "Không tìm thấy thuốc!" });
            }

            return Ok(thuoc);
        }

        // POST: api/ThuocApi
        // Thêm thuốc mới
        [HttpPost]
        public async Task<ActionResult<Thuoc>> PostThuoc(Thuoc thuoc)
        {
            _context.Thuoc.Add(thuoc);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetThuoc", new { id = thuoc.MaThuoc }, thuoc);
        }

        // PUT: api/ThuocApi/5
        // Cập nhật thông tin thuốc
        [HttpPut("{id}")]
        public async Task<IActionResult> PutThuoc(int id, Thuoc thuoc)
        {
            if (id != thuoc.MaThuoc)
            {
                return BadRequest(new { message = "Mã thuốc không khớp!" });
            }

            _context.Entry(thuoc).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ThuocExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/ThuocApi/5
        // Xóa thuốc
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteThuoc(int id)
        {
            var thuoc = await _context.Thuoc.FindAsync(id);
            if (thuoc == null)
            {
                return NotFound();
            }

            _context.Thuoc.Remove(thuoc);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa thuốc thành công!" });
        }

        private bool ThuocExists(int id)
        {
            return _context.Thuoc.Any(e => e.MaThuoc == id);
        }
    }
}