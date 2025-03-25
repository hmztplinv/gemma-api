// Controllers/VocabularyController.cs
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
    [Route("api/vocabulary")]
    public class VocabularyController : ControllerBase
    {
        private readonly IUserVocabularyService _vocabularyService;
        private readonly ILogger<VocabularyController> _logger;

        public VocabularyController(
            IUserVocabularyService vocabularyService,
            ILogger<VocabularyController> logger)
        {
            _vocabularyService = vocabularyService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VocabularyItemDto>>> GetVocabulary()
        {
            try
            {
                var userId = GetUserId();
                var vocabulary = await _vocabularyService.GetUserVocabularyAsync(userId);
                return Ok(vocabulary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vocabulary");
                return StatusCode(500, "Failed to retrieve vocabulary");
            }
        }

        [HttpGet("flashcards")]
        public async Task<ActionResult<IEnumerable<VocabularyItemDto>>> GetFlashcards([FromQuery] string level = null, [FromQuery] int count = 10)
        {
            try
            {
                var userId = GetUserId();
                var flashcards = await _vocabularyService.GetFlashcardsAsync(userId, level, count);
                return Ok(flashcards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving flashcards");
                return StatusCode(500, "Failed to retrieve flashcards");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<VocabularyItemDto>> UpdateVocabularyItem(int id, [FromBody] UpdateVocabularyItemDto updateDto)
        {
            try
            {
                var userId = GetUserId();
                var updatedItem = await _vocabularyService.UpdateVocabularyItemAsync(userId, id, updateDto);
                return Ok(updatedItem);
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound("Vocabulary item not found or access denied");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating vocabulary item");
                return StatusCode(500, "Failed to update vocabulary item");
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