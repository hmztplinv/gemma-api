using System;
using System.Threading.Tasks;
using LanguageLearningApp.API.Application.DTOs;
using LanguageLearningApp.API.Application.Interfaces;
using LanguageLearningApp.API.Domain.Entities;
using LanguageLearningApp.API.Domain.Interfaces;
using LanguageLearningApp.API.Infrastructure.Services;

namespace LanguageLearningApp.API.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserProgressRepository _userProgressRepository;
        private readonly TokenService _tokenService;

        public AuthService(IUserRepository userRepository, 
                          IUserProgressRepository userProgressRepository, 
                          TokenService tokenService)
        {
            _userRepository = userRepository;
            _userProgressRepository = userProgressRepository;
            _tokenService = tokenService;
        }

        public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
        {
            // Check if username is taken
            if (await _userRepository.GetUserByUsernameAsync(registerDto.Username) != null)
            {
                throw new InvalidOperationException("Username is already taken");
            }

            // Check if email is taken
            if (await _userRepository.GetUserByEmailAsync(registerDto.Email) != null)
            {
                throw new InvalidOperationException("Email is already registered");
            }

            // Create password hash
            _tokenService.CreatePasswordHash(registerDto.Password, out string passwordHash, out string passwordSalt);

            // Create new user
            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                CreatedAt = DateTime.UtcNow,
                LastActive = DateTime.UtcNow,
                LanguageLevel = "Beginner",
                CurrentStreak = 0,
                LongestStreak = 0,
                TotalPoints = 0
            };

            // Add user to database
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            // Create initial user progress record
            var progress = new UserProgress
            {
                UserId = user.Id,
                TotalWordsLearned = 0,
                TotalQuizzesTaken = 0,
                AverageQuizScore = 0,
                DailyGoalCompletedCount = 0,
                WeeklyGoalCompletedCount = 0,
                TotalConversations = 0,
                TotalMessages = 0,
                LastUpdatedAt = DateTime.UtcNow
            };

            await _userProgressRepository.AddAsync(progress);
            await _userProgressRepository.SaveChangesAsync();

            // Return user DTO with token
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Token = _tokenService.CreateToken(user),
                LanguageLevel = user.LanguageLevel,
                CurrentStreak = user.CurrentStreak,
                TotalPoints = user.TotalPoints
            };
        }

        public async Task<UserDto> LoginAsync(LoginDto loginDto)
        {
            // Find user by username
            var user = await _userRepository.GetUserByUsernameAsync(loginDto.Username);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid username");
            }

            // Verify password
            if (!_tokenService.VerifyPasswordHash(loginDto.Password, user.PasswordHash, user.PasswordSalt))
            {
                throw new UnauthorizedAccessException("Invalid password");
            }

            // Update last active timestamp
            user.LastActive = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            // Return user DTO with token
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Token = _tokenService.CreateToken(user),
                LanguageLevel = user.LanguageLevel,
                CurrentStreak = user.CurrentStreak,
                TotalPoints = user.TotalPoints
            };
        }
    }
}