using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Whisprr.Entities.Models;
using Whisprr.Infrastructure.Persistence;

namespace Whisprr.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SocialInfoController : ControllerBase
{
    private readonly AppDbContext _context;

    public SocialInfoController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/SocialInfo
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SocialInfo>>> GetSocialInfos()
    {
        return await _context.SocialInfos.ToListAsync();
    }

    // GET: api/SocialInfo/5
    [HttpGet("{id}")]
    public async Task<ActionResult<SocialInfo>> GetSocialInfo(Guid id)
    {
        var socialInfo = await _context.SocialInfos.FindAsync(id);

        if (socialInfo == null)
        {
            return NotFound();
        }

        return socialInfo;
    }

    // PUT: api/SocialInfo/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutSocialInfo(Guid id, SocialInfo socialInfo)
    {
        if (id != socialInfo.Id)
        {
            return BadRequest();
        }

        _context.Entry(socialInfo).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SocialInfoExists(id))
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

    // POST: api/SocialInfo
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<SocialInfo>> PostSocialInfo(SocialInfo socialInfo)
    {
        _context.SocialInfos.Add(socialInfo);
        await _context.SaveChangesAsync();

        // Use nameof to automatically update GetSocialInfo string, so if we change that method's name, it will automatically update here too
        return CreatedAtAction(nameof(GetSocialInfo), new { id = socialInfo.Id }, socialInfo);
    }

    // DELETE: api/SocialInfo/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSocialInfo(Guid id)
    {
        var socialInfo = await _context.SocialInfos.FindAsync(id);
        if (socialInfo == null)
        {
            return NotFound();
        }

        _context.SocialInfos.Remove(socialInfo);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool SocialInfoExists(Guid id)
    {
        return _context.SocialInfos.Any(e => e.Id == id);
    }
}
