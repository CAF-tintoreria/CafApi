using CafApi.Data;
using CafApi.Models.SampleState;
using CafApi.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CafApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SampleStateController : ControllerBase
    {
        private readonly ISampleStateRepository _repo;
        private readonly JwtTokenService _tokenSvc;
        public SampleStateController(ISampleStateRepository repo, JwtTokenService tokenSvc)
        {
            _repo = repo;
            _tokenSvc = tokenSvc;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            var data = await _repo.GetAllAsync();

            List<SampleStateDTO> response = data.Select(item => new SampleStateDTO
            {
                SampleStateID = item.SampleStateID,
                StateName = item.State.ToString()
            }).ToList();

            return Ok(response);
        }
    }
}
