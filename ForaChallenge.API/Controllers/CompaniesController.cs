using ForaChallenge.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ForaChallenge.API.Controllers;

/// <summary>
/// Companies with fundable amounts (contract: id, name, standardFundableAmount, specialFundableAmount).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CompaniesController(ICompaniesWithFundingService companiesWithFundingService) : ControllerBase
{
    private readonly ICompaniesWithFundingService _companiesWithFundingService = companiesWithFundingService;

    /// <summary>
    /// Gets all companies with standardFundableAmount and specialFundableAmount. Optional filter by first letter of name.
    /// </summary>
    /// <param name="nameStartsWith">Optional: filter by first letter (e.g. A).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CompanyFundingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CompanyFundingDto>>> Get([FromQuery] string? nameStartsWith, CancellationToken cancellationToken)
    {
        char? filter = !string.IsNullOrWhiteSpace(nameStartsWith) ? nameStartsWith!.Trim()[0] : null;
        var list = await _companiesWithFundingService.GetAsync(filter, cancellationToken);
        return Ok(list);
    }
}
