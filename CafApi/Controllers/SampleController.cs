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
                return Ok(data);

            }catch(Exception ex)
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

                return Ok(data);

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

                return Ok(data);

            }
            catch (Exception ex)
            {
                throw new Exception("Error en controlador", ex);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] UpdateSampleDTO sample)
        {
            try
            {
                var existingSample = _sampleRepository.GetByIdAsync(id);

                if (existingSample is null) return NotFound("Muestra no encontrada con el Id dado: "  + id);
                
                var data = await _sampleRepository.UpdateAsync(sample);

                if (data < 1) return BadRequest("No se pudo actualizar el registro");

                return Ok(data);

            }
            catch (Exception ex)
            {
                throw new Exception("Error en controlador", ex);
            }
        }
    }
}
