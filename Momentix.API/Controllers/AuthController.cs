using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Momentix.API.DTOs;
using Momentix.API.Models;
using Momentix.API.Services;

namespace Momentix.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
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
                return BadRequest("Потребител с този имейл вече съществува.");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.Email
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
                Email = user.Email
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized("Невалиден имейл или парола.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded)
                return Unauthorized("Невалиден имейл или парола.");

            var token = _tokenService.GenerateToken(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email
            });
        }
    }
}