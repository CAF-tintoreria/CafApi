using CafApi.Models.Sample;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CafApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SampleController : ControllerBase
    {
        private readonly ISampleRepository _sampleRepository;

        public SampleController(ISampleRepository sampleRepository)
        {
            _sampleRepository = sampleRepository;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            try
            {
                var data = await _sampleRepository.GetAllAsync();
                return Ok(new { ok = true, data });
            }
            catch (Exception ex)
            {
                throw new Exception("Error en controlador", ex);
            }
        }
        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            if (id < 1) return BadRequest("El id ingresado es inválido:" + id);
            try
            {
                var data = await _sampleRepository.GetByIdAsync(id);

                if (data == null) return NotFound("No se encontró registros con el Id ingresado:" + id);

                return Ok(new { ok = true, data });

            }
            catch (Exception ex)
            {
                throw new Exception("Error en controlador", ex);
            }
        }
        [HttpPost("create")]
        public async Task<ActionResult> Create([FromBody] CreateSampleDTO sample)
        {
            try
            {
                var data = await _sampleRepository.CreateAsync(sample);
                if (data < 1) return BadRequest("No se pudo crear el registro");
                return Ok(new { ok = true, message = "Muestra creada correctamente." });
            }
            catch (InvalidOperationException vex)
            {
                return BadRequest(new { ok = false, message = vex.Message });
            }
            catch (Exception ex)
            {
                throw new Exception("Error en controlador", ex);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] CreateSampleDTO sample)
        {
            try
            {
                if (id <= 0) return BadRequest("Id inválido.");

                var exists = await _sampleRepository.GetByIdAsync(id);
                if (exists is null) return NotFound($"Muestra no encontrada: {id}");

                var updatedId = await _sampleRepository.UpdateAsync(id, sample);
                if (updatedId < 1) return BadRequest("No se pudo actualizar el registro");

                return Ok(new { ok = true, message = "Muestra actualizada correctamente." });
            }
            catch (InvalidOperationException vex)
            {
                return BadRequest(new { ok = false, message = vex.Message });
            }
            catch (Exception ex)
            {
                throw new Exception("Error en controlador", ex);
            }
        }
    }
}
