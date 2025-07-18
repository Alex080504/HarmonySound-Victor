﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HarmonySound.Models;
using HarmonySound.API.Data;

namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContentAlbumsController : ControllerBase
    {
        private readonly HarmonySoundDbContext _context;

        public ContentAlbumsController(HarmonySoundDbContext context)
        {
            _context = context;
        }

        // GET: api/ContentAlbums
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContentAlbum>>> GetContentAlbum()
        {
            return await _context.ContentsAlbums.ToListAsync();
        }

        // GET: api/ContentAlbums/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ContentAlbum>> GetContentAlbum(int id)
        {
            var contentAlbum = await _context.ContentsAlbums.FindAsync(id);

            if (contentAlbum == null)
            {
                return NotFound();
            }

            return contentAlbum;
        }

        // PUT: api/ContentAlbums/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutContentAlbum(int id, ContentAlbum contentAlbum)
        {
            if (id != contentAlbum.Id)
            {
                return BadRequest();
            }

            _context.Entry(contentAlbum).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ContentAlbumExists(id))
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

        // POST: api/ContentAlbums
        [HttpPost]
        public async Task<ActionResult<ContentAlbum>> PostContentAlbum(ContentAlbum data)
        {
            _context.ContentsAlbums.Add(data);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetContentAlbum", new { id = data.Id }, data);
        }

        // DELETE: api/ContentAlbums/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContentAlbum(int id)
        {
            var contentAlbum = await _context.ContentsAlbums.FindAsync(id);
            if (contentAlbum == null) return NotFound();

            _context.ContentsAlbums.Remove(contentAlbum);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool ContentAlbumExists(int id)
        {
            return _context.ContentsAlbums.Any(e => e.Id == id);
        }
    }
}
