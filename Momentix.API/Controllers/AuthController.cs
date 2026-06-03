using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Momentix.Data.DTOs;
using Momentix.Data.Models;
using Momentix.API.Services;
using System.Net.Mail;

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
        private readonly EmailService _emailService;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            TokenService tokenService,
            EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest("A user with this email already exists.");

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

        [HttpPost("send-registration-email")]
        public async Task<IActionResult> SendRegistrationEmail([FromBody] SendRegistrationEmailDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest("Email is required.");

            try
            {
                _ = new MailAddress(dto.Email.Trim());
                await _emailService.SendRegistrationEmailAsync(dto.Email.Trim(), dto.FullName);
                return Ok("Registration email sent.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (SmtpException ex)
            {
                return BadRequest($"Email could not be sent: {ex.Message}");
            }
            catch (FormatException)
            {
                return BadRequest("Email address is not valid.");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var login = dto.Email.Trim();
            var user = await _userManager.FindByEmailAsync(login)
                ?? await _userManager.FindByNameAsync(login);

            if (user == null)
                return Unauthorized("Invalid username or password.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded)
                return Unauthorized("Invalid username or password.");

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
