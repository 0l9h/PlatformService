using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlatformService.Data;
using PlatformService.DTOs;
using PlatformService.Models;
using PlatformService.SyncDataServices.Http;

namespace PlatformService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlatformsController : ControllerBase
    {
        private readonly IPlatformRepo _repository;
        private readonly IMapper _mapper;
        private readonly ICommandDataClient _commandDataClient;

        public PlatformsController(IPlatformRepo repository, 
            IMapper mapper,
            ICommandDataClient commandDataClient)
        {
            _repository = repository;
            _mapper = mapper;
            _commandDataClient = commandDataClient;
        }

        [HttpGet]
        public ActionResult<IEnumerable<PlatformReadDTO>?> GetAllPlatforms()
        {
            Console.WriteLine("--> Getting platforms...");

            var platformItems = _repository.GetAllPlatforms();
            var platformItemsDTOs = _mapper.Map<IEnumerable<PlatformReadDTO>>(platformItems);

            return Ok(platformItemsDTOs);
        }

        [HttpGet("{id}", Name = "GetPlatformById")]
        public ActionResult<PlatformReadDTO> GetPlatformById(int id)
        {
            Console.WriteLine($"--> Getting platform with id {id}...");

            var platform = _repository.GetPlatformById(id);
            if(platform == null)
            {
                return NotFound();
            }

            var platformDTO = _mapper.Map<PlatformReadDTO>(platform);

            return Ok(platformDTO);
        }

        [HttpPost]
        public async Task<ActionResult<PlatformReadDTO>> CreatePlatform([FromBody] PlatformCreateDTO platformCreateDTO)
        {
            var platform = _mapper.Map<Platform>(platformCreateDTO);
            _repository.CreatePlatform(platform);
            _repository.SaveChanges();

            var platformReadDTO = _mapper.Map<PlatformReadDTO>(platform);

            try
            {
                await _commandDataClient.SendPlatformToCommand(platformReadDTO);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not send synchronously: {ex.Message}");
            }

            return CreatedAtRoute(nameof(GetPlatformById), new { Id = platformReadDTO.Id}, platformReadDTO);
        }
    }
}
