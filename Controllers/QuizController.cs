using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using LanguageLearningApp.API.Application.DTOs;
using LanguageLearningApp.API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LanguageLearningApp.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class QuizController : ControllerBase
    {
        private readonly IQuizService _quizService;
        private readonly ILogger<QuizController> _logger;

        public QuizController(
            IQuizService quizService,
            ILogger<QuizController> logger)
        {
            _quizService = quizService;
            _logger = logger;
        }

        [HttpGet("levels/{level}")]
        public async Task<ActionResult<IEnumerable<QuizDto>>> GetQuizzesByLevel(string level)
        {
            try
            {
                var quizzes = await _quizService.GetQuizzesByLevelAsync(level);
                return Ok(quizzes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quizzes by level");
                return StatusCode(500, "Failed to retrieve quizzes");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<QuizDto>> GetQuiz(int id)
        {
            try
            {
                var quiz = await _quizService.GetQuizByIdAsync(id);
                return Ok(quiz);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Quiz not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quiz");
                return StatusCode(500, "Failed to retrieve quiz");
            }
        }

        [HttpPost("generate")]
        public async Task<ActionResult<QuizDto>> GenerateVocabularyQuiz([FromBody] TakeQuizDto takeQuizDto)
        {
            try
            {
                var userId = GetUserId();
                var quiz = await _quizService.GenerateVocabularyQuizAsync(userId, takeQuizDto.Level);
                return Ok(quiz);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating vocabulary quiz");
                return StatusCode(500, "Failed to generate quiz");
            }
        }

        [HttpPost("submit")]
        public async Task<ActionResult<QuizResultDto>> SubmitQuizAnswers([FromBody] SubmitQuizAnswerDto answerDto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _quizService.SubmitQuizAnswersAsync(userId, answerDto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting quiz answers");
                return StatusCode(500, "Failed to submit quiz answers");
            }
        }

        [HttpGet("results")]
        public async Task<ActionResult<IEnumerable<QuizResultDto>>> GetUserQuizResults()
        {
            try
            {
                var userId = GetUserId();
                var results = await _quizService.GetUserQuizResultsAsync(userId);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user quiz results");
                return StatusCode(500, "Failed to retrieve quiz results");
            }
        }

        [HttpGet("results/{id}")]
        public async Task<ActionResult<QuizResultDto>> GetQuizResult(int id)
        {
            try
            {
                var userId = GetUserId();
                var result = await _quizService.GetQuizResultByIdAsync(id, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quiz result");
                return StatusCode(500, "Failed to retrieve quiz result");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] // Optional: restrict quiz creation to admins
        public async Task<ActionResult<QuizDto>> CreateQuiz([FromBody] QuizDto quizDto)
        {
            try
            {
                var quiz = await _quizService.CreateQuizAsync(quizDto);
                return CreatedAtAction(nameof(GetQuiz), new { id = quiz.Id }, quiz);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quiz");
                return StatusCode(500, "Failed to create quiz");
            }
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID in token");
            }
            return userId;
        }
    }
}