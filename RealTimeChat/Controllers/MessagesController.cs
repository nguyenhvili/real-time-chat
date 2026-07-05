using Microsoft.AspNetCore.Mvc;
using RealTimeChat.Models.Models;
using RealTimeChat.Server.Services;

namespace RealTimeChat.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class MessagesController(MessageProcessing messageProcessing, IServiceScopeFactory serviceScopeFactory) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ChatResponse>> GetMessages()
    {
        var response = await messageProcessing.InitializeHistoryAsync(serviceScopeFactory);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> SendMessage([FromBody] SendMessageRequest request)
    {
        var response = await messageProcessing.ProcessMessageAsync(request, serviceScopeFactory);

        if (response.Errors.Count > 0)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(nameof(GetMessages), response);
    }
}
