// Controllers/SampleController.cs
using CafApi.Data;
using CafApi.Models.Sample;
using Microsoft.AspNetCore.Mvc;

namespace CafApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SampleController : ControllerBase
    {
        private readonly ISampleRepository _sampleRepository;
        private readonly AuditService _audit;

        public SampleController(ISampleRepository sampleRepository, AuditService audit)
        {
            _sampleRepository = sampleRepository;
            _audit = audit;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            var data = await _sampleRepository.GetAllAsync();
            return Ok(new { ok = true, data });
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetById(int id)
        {
            if (id < 1) return BadRequest("El id ingresado es inválido:" + id);

            var data = await _sampleRepository.GetByIdAsync(id);
            if (data is null) return NotFound("No se encontró registros con el Id ingresado:" + id);

            return Ok(new { ok = true, data });
        }

        [HttpPost("create")]
        public async Task<ActionResult> Create([FromBody] CreateSampleDTO sample)
        {
            try
            {
                var newId = await _sampleRepository.CreateAsync(sample);
                if (newId < 1) return BadRequest("No se pudo crear el registro");

                // AFTER para auditoría
                var after = await _sampleRepository.GetByIdAsync(newId);
                await _audit.LogAsync(
                    entidad: "Muestra", entidadId: newId, operacion: "CREATE",
                    beforeObj: null, afterObj: after,
                    extra: $"Creación de muestra #{newId}"
                );

                return Ok(new { ok = true, id = newId, message = "Muestra creada correctamente." });
            }
            catch (InvalidOperationException vex)
            {
                return BadRequest(new { ok = false, message = vex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] CreateSampleDTO sample)
        {
            try
            {
                if (id <= 0) return BadRequest("Id inválido.");

                var exists = await _sampleRepository.GetByIdAsync(id);
                if (exists is null) return NotFound($"Muestra no encontrada: {id}");

                // BEFORE
                var before = exists;

                var updatedId = await _sampleRepository.UpdateAsync(id, sample);
                if (updatedId < 1) return BadRequest("No se pudo actualizar el registro");

                // AFTER
                var after = await _sampleRepository.GetByIdAsync(id);

                await _audit.LogAsync(
                    entidad: "Muestra", entidadId: id, operacion: "UPDATE",
                    beforeObj: before, afterObj: after,
                    extra: $"Actualización completa de muestra #{id}"
                );

                return Ok(new { ok = true, message = "Muestra actualizada correctamente." });
            }
            catch (InvalidOperationException vex)
            {
                return BadRequest(new { ok = false, message = vex.Message });
            }
        }

        // ====== NAVEGACIÓN extra ======

        [HttpPatch("{id:int}/estado/{estadoId:int}")]
        public async Task<ActionResult> UpdateEstado(int id, int estadoId)
        {
            // BEFORE (solo lo necesario)
            var before = await _sampleRepository.GetByIdAsync(id);
            var beforeObj = before is null ? null : new
            {
                MuestraID = before.MuestraID,
                EstadoID = before.Status?.SampleStatusID,
                NombreEstado = before.Status?.Status.ToString()
            };

            var code = await _sampleRepository.UpdateEstadoAsync(id, estadoId);

            switch (code)
            {
                case 1:
                    // AFTER
                    var after = await _sampleRepository.GetByIdAsync(id);
                    var afterObj = after is null ? null : new
                    {
                        MuestraID = after.MuestraID,
                        EstadoID = after.Status?.SampleStatusID,
                        NombreEstado = after.Status?.Status.ToString()
                    };

                    await _audit.LogAsync(
                        entidad: "Muestra", entidadId: id, operacion: "UPDATE-ESTADO",
                        beforeObj: beforeObj, afterObj: afterObj,
                        extra: $"Cambio de estado: {beforeObj?.NombreEstado} -> {afterObj?.NombreEstado}"
                    );

                    return Ok(new { ok = true, message = "Estado actualizado." });

                case 0:
                    return Ok(new { ok = true, message = "Sin cambios (ya tenía ese estado)." });

                case -1:
                    return NotFound(new { ok = false, message = $"Muestra no encontrada: {id}" });

                default:
                    return StatusCode(500, new { ok = false, message = "Error desconocido al actualizar estado." });
            }
        }

        [HttpGet("{id:int}/procesos")]
        public async Task<ActionResult> GetProcesos(int id)
        {
            var data = await _sampleRepository.GetProcesosBySampleIdAsync(id);
            return Ok(new { ok = true, data });
        }

        public class AddProcesoDto { public int ProcesoID { get; set; } }

        [HttpPost("{id:int}/procesos")]
        public async Task<ActionResult> AddProceso(int id, [FromBody] AddProcesoDto body)
        {
            var newId = await _sampleRepository.AddProcesoToSampleAsync(id, body.ProcesoID);

            if (newId == 0)
                return Ok(new { ok = true, message = "El proceso ya estaba asociado." });

            // Solo audito si realmente se agregó
            await _audit.LogAsync(
                entidad: "Muestra", entidadId: id, operacion: "ADD-PROCESO",
                beforeObj: null,
                afterObj: new { MuestraID = id, ProcesoID = body.ProcesoID, MuestraProcesoID = newId },
                extra: $"Asociación de proceso {body.ProcesoID} a muestra #{id}"
            );

            return Ok(new { ok = true, id = newId, message = "Proceso agregado a la muestra." });
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                // BEFORE
                var before = await _sampleRepository.GetByIdAsync(id);

                var rows = await _sampleRepository.DeleteAsync(id);
                if (rows == 0) return NotFound(new { ok = false, message = $"Muestra no encontrada: {id}" });

                await _audit.LogAsync(
                    entidad: "Muestra", entidadId: id, operacion: "DELETE",
                    beforeObj: before, afterObj: null,
                    extra: $"Eliminación de muestra #{id}"
                );

                return Ok(new { ok = true, message = "Muestra eliminada." });
            }
            catch (InvalidOperationException vex)
            {
                // cae acá cuando hay prendas no terminadas vinculadas
                return BadRequest(new { ok = false, message = vex.Message });
            }
            catch (Exception ex)
            {
                // mantengo el comportamiento, pero sin perder la auditoría previa si falló el delete
                throw new Exception("Error en controlador", ex);
            }
        }
    }
}
