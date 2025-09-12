using CX.Container.Server.Domain;
using CX.Container.Server.Domain.ContentTypes;
using CX.Container.Server.Domain.Messages.Dtos;
using CX.Container.Server.Domain.Messages.Models;
using CX.Container.Server.Domain.MessageTypes;
using CX.Container.Server.Domain.SourceDocuments.Dtos;
using CX.Container.Server.Domain.SourceDocuments.Features;
using CX.Container.Server.Domain.Threads.Features;
using CX.Container.Server.Domain.Threads.Mappings;
using CX.Container.Server.Domain.Threads.Models;
using CX.Container.Server.Domain.Threads.Services;
using CX.Container.Server.Exceptions;
using CX.Container.Server.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CX.Container.Server.Controllers.v1
{
    /// <summary>
    /// Testing and Cleaning Functions
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="chatService"></param>
    /// <param name="threadRepository"></param>
    /// <param name="currentUserService"></param>
    [Route("api/[controller]")]
    [ApiController]
    public class DBActionController(IMediator mediator,
        IAiService chatService, IThreadRepository threadRepository, ICurrentUserService currentUserService) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;
        private readonly IAiService _chatService = chatService;
        private readonly IThreadRepository _threadRepository = threadRepository;
        private readonly ICurrentUserService _currentUserService = currentUserService;

        /// <summary>
        /// Reset Source Documents when Fields gets added
        /// </summary>
        [Authorize]
        [HttpGet(Name = "FixSourceDocuments")]
        public async Task<IActionResult> FixSourceDocuments()
        {
            // Authorization is handled by the Authorize attribute
            // To implement more granular permissions, use policy-based authorization with the [Authorize(Policy = "...")] attribute
            var query = new GetSourceDocumentList.Query(new SourceDocumentParametersDto { DefaultPageSize = 1_000_000, PageSize = 1_000_000 });
            var queryResponse = await _mediator.Send(query);
            queryResponse.ForEach(async sourceDocument =>
            {
                var command = new UpdateSourceDocument.Command(sourceDocument.Id,
                    new SourceDocumentForUpdateDto { DisplayName = sourceDocument.Name });
                await _mediator.Send(command);
            });
            return Ok(queryResponse.Take(1));
        }

        /// <summary>
        /// Create Chats and Threads for Testing
        /// </summary>
        [Authorize]
        [HttpPost(Name = "ChatAskAndResponce")]
        public async Task<ActionResult<AelaResponseDto>> ChatAskAndResponce(string Question, string ThreadName)
        {
            var thread = _threadRepository.Query().FirstOrDefault(p => p.CreatedBy == _currentUserService.UserId && p.Name == ThreadName)?.ToThreadDto();

            if (thread == null || thread == default)
            {
                var command = new AddThread.Command(new Domain.Threads.Dtos.ThreadForCreationDto() { Name = ThreadName });
                var threadDto = await _mediator.Send(command);
                thread = threadDto;
            }
            try
            {
                var message = new MessageForCreation()
                {
                    Content = Question,
                    MessageType = MessageType.User().Value,
                    ContentType = ContentType.PlainText().Value,
                    ThreadId = thread.Id
                };
                var response = await _chatService.SendMessage(message);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
    }
}
