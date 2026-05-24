using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Momentix.Data.DTOs;
using Momentix.Data.Models;
using Momentix.API.Services;

namespace Momentix.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private const string AdminRole = "Admin";

        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly TokenService _tokenService;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            TokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest("?????????? ? ???? ????? ???? ??????????.");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.Email,
                ThemeColor = dto.ThemeColor
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            var token = _tokenService.GenerateToken(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                ThemeColor = user.ThemeColor,
                IsAdmin = false
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var login = dto.Email.Trim();
            var user = await _userManager.FindByEmailAsync(login)
                ?? await _userManager.FindByNameAsync(login);

            if (user == null)
                return Unauthorized("????????? ?????????? ??? ??????.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded)
                return Unauthorized("????????? ?????????? ??? ??????.");

            var token = _tokenService.GenerateToken(user);
            var isAdmin = await _userManager.IsInRoleAsync(user, AdminRole);

            return Ok(new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                ThemeColor = user.ThemeColor,
                IsAdmin = isAdmin
            });
        }
    }
}
