using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Whisprr.Entities.Models;
using Whisprr.Infrastructure.Persistence;

namespace Whisprr.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SourcePlatformController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SourcePlatformController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/SourcePlatform
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SourcePlatform>>> GetSourcePlatforms()
        {
            return await _context.SourcePlatforms.ToListAsync();
        }

        // GET: api/SourcePlatform/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SourcePlatform>> GetSourcePlatform(Guid id)
        {
            var sourcePlatform = await _context.SourcePlatforms.FindAsync(id);

            if (sourcePlatform == null)
            {
                return NotFound();
            }

            return sourcePlatform;
        }

        // PUT: api/SourcePlatform/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSourcePlatform(Guid id, SourcePlatform sourcePlatform)
        {
            if (id != sourcePlatform.Id)
            {
                return BadRequest();
            }

            _context.Entry(sourcePlatform).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SourcePlatformExists(id))
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

        // POST: api/SourcePlatform
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<SourcePlatform>> PostSourcePlatform(SourcePlatform sourcePlatform)
        {
            _context.SourcePlatforms.Add(sourcePlatform);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSourcePlatform), new { id = sourcePlatform.Id }, sourcePlatform);
        }

        // DELETE: api/SourcePlatform/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSourcePlatform(Guid id)
        {
            var sourcePlatform = await _context.SourcePlatforms.FindAsync(id);
            if (sourcePlatform == null)
            {
                return NotFound();
            }

            _context.SourcePlatforms.Remove(sourcePlatform);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SourcePlatformExists(Guid id)
        {
            return _context.SourcePlatforms.Any(e => e.Id == id);
        }
    }
}
