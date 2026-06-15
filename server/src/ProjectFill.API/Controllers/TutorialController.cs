using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProjectFill.Application.Tutorial;
using ProjectFill.Contracts.Tutorial;

namespace ProjectFill.API.Controllers
{
    [ApiController]
    [Route("api/tutorial")]
    public sealed class TutorialController : ControllerBaseEx
    {
        private readonly TutorialService _tutorialService;

        public TutorialController(TutorialService tutorialService)
        {
            _tutorialService = tutorialService;
        }

        [HttpGet("progress")]
        public async Task<TutorialProgressResponse> GetProgress(CancellationToken ct)
        {
            var ids = await _tutorialService.GetCompletedTutorialIdsAsync(PlayerId, ct);
            return new TutorialProgressResponse { CompletedTutorialIds = ids };
        }

        [HttpPost("progress/{tutorialId:int}")]
        public async Task<TutorialProgressUpdateResponse> CompleteTutorial(int tutorialId, CancellationToken ct)
        {
            var ids = await _tutorialService.CompleteTutorialAsync(PlayerId, tutorialId, ct);
            return new TutorialProgressUpdateResponse { Success = true, CompletedTutorialIds = ids };
        }
    }
}
