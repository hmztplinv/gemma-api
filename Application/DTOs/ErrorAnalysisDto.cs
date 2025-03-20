using System.Collections.Generic;

namespace LanguageLearningApp.API.Application.DTOs
{
    public class ErrorAnalysisDto
    {
        public List<ErrorDto> Errors { get; set; } = new List<ErrorDto>();
    }
}