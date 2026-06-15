using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProjectFill.Application.Iap;
using ProjectFill.Contracts.Iap;

namespace ProjectFill.API.Controllers
{
    [ApiController]
    [Route("api/iap")]
    public sealed class IapController : ControllerBaseEx
    {
        private readonly IapService _iapService;

        public IapController(IapService iapService)
        {
            _iapService = iapService;
        }

        [HttpPost("verify")]
        public Task<VerifyIapResponse> Verify([FromBody] VerifyIapRequest request, CancellationToken ct)
        {
            return _iapService.VerifyIapAsync(PlayerId, request, CorrelationId, ct);
        }

        [HttpGet("products")]
        public Task<GetIapProductsResponse> GetProducts(CancellationToken ct)
        {
            return _iapService.GetProductStatusesAsync(PlayerId, ct);
        }
    }
}
