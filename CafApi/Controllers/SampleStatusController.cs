using CafApi.Data;
using CafApi.Models.SampleState;
using CafApi.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CafApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SampleStatusController : ControllerBase
    {
        private readonly ISampleStatusRepository _repo;
        private readonly JwtTokenService _tokenSvc;
        public SampleStatusController(ISampleStatusRepository repo, JwtTokenService tokenSvc)
        {
            _repo = repo;
            _tokenSvc = tokenSvc;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            var data = await _repo.GetAllAsync();

            List<SampleStatusDTO> response = data.Select(item => new SampleStatusDTO
            {
                SampleStatusID = item.SampleStatusID,
                StatusName = item.Status.ToString()
            }).ToList();

            return Ok(response);
        }
    }
}
