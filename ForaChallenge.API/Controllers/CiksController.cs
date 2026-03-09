using ForaChallenge.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ForaChallenge.API.Controllers;

/// <summary>
/// Endpoints for CIK import queue: add CIKs, process pending.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CiksController(
    IEdgarImportService importService,
    IAddCiksToQueueService addCiksService) : ControllerBase
{
    private readonly IEdgarImportService _importService = importService;
    private readonly IAddCiksToQueueService _addCiksService = addCiksService;

    /// <summary>
    /// Adds one or more CIKs to the queue (Status = Pending). Duplicates are skipped.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AddCiksResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AddCiksResponse>> Add([FromBody] AddCiksRequest request, CancellationToken cancellationToken)
    {
        var added = await _addCiksService.AddAsync(request.Ciks ?? [], cancellationToken);
        return Ok(new AddCiksResponse(added));
    }

    /// <summary>
    /// Runs the EDGAR import for all CIKs with status Pending. And returns counts of processed and failed.
    /// </summary>
    [HttpPost("process")]
    [ProducesResponseType(typeof(EdgarImportResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<EdgarImportResult>> ProcessPending(CancellationToken cancellationToken)
    {
        var result = await _importService.ProcessPendingAsync(cancellationToken);
        return Ok(result);
    }

}

/// <summary>Request body for adding CIKs to the queue.</summary>
public sealed record AddCiksRequest(int[]? Ciks);

/// <summary>Response: number of CIKs added.</summary>
public sealed record AddCiksResponse(int AddedCount);
