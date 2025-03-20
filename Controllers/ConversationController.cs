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
    public class ConversationController : ControllerBase
    {
        private readonly IConversationService _conversationService;
        private readonly ILogger<ConversationController> _logger;

        public ConversationController(
            IConversationService conversationService,
            ILogger<ConversationController> logger)
        {
            _conversationService = conversationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConversationDto>>> GetConversations()
        {
            try
            {
                var userId = GetUserId();
                var conversations = await _conversationService.GetUserConversationsAsync(userId);
                return Ok(conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations");
                return StatusCode(500, "Failed to retrieve conversations");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ConversationDto>> GetConversation(int id)
        {
            try
            {
                var userId = GetUserId();
                var conversation = await _conversationService.GetConversationAsync(id, userId);
                return Ok(conversation);
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound("Conversation not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation");
                return StatusCode(500, "Failed to retrieve conversation");
            }
        }

        [HttpPost]
        public async Task<ActionResult<ConversationDto>> CreateConversation(CreateConversationDto dto)
        {
            try
            {
                var userId = GetUserId();
                var conversation = await _conversationService.CreateConversationAsync(userId, dto);
                return CreatedAtAction(nameof(GetConversation), new { id = conversation.Id }, conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating conversation");
                return StatusCode(500, "Failed to create conversation");
            }
        }

        [HttpPost("{id}/messages")]
        public async Task<ActionResult<MessageDto>> SendMessage(int id, SendMessageDto dto)
        {
            try
            {
                var userId = GetUserId();
                var message = await _conversationService.SendMessageAsync(id, userId, dto);
                return Ok(message);
            }
            catch (UnauthorizedAccessException)
            {
                return NotFound("Conversation not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return StatusCode(500, "Failed to send message");
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