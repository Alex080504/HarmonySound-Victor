﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using HarmonySound.Models;
using HarmonySound.API.Data;
namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        public UsersController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        // GET: api/Users
        [HttpGet]
        public ActionResult<IEnumerable<object>> GetUsers()
        {
            // Devuelve solo información pública
            var users = _userManager.Users.Select(u => new {
                u.Id,
                u.UserName,
                u.Email,
                u.Name,
                u.State,
                u.RegisterDate
            }).ToList();

            return Ok(users);
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound();

            return Ok(new {
                user.Id,
                user.UserName,
                user.Email,
                user.Name,
                user.State,
                user.RegisterDate
            });
        }

        // GET: api/Users/Artists
        [HttpGet("Artists")]
        public async Task<ActionResult<IEnumerable<object>>> GetArtists()
        {
            var artists = await _userManager.GetUsersInRoleAsync("Artist");
            var result = artists.Select(u => new {
                u.Id,
                u.UserName,
                u.Email,
                u.Name,
                u.State,
                u.RegisterDate
            }).ToList();

            return Ok(result);
        }

        // GET: api/Users/Clients
        [HttpGet("Clients")]
        public async Task<ActionResult<IEnumerable<object>>> GetClients()
        {
            var clients = await _userManager.GetUsersInRoleAsync("Client");
            var result = clients.Select(u => new {
                u.Id,
                u.UserName,
                u.Email,
                u.Name,
                u.State,
                u.RegisterDate
            }).ToList();

            return Ok(result);
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return NoContent();
        }
    }
}
