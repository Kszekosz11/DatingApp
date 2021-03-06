using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController] // jeśli używamy tego c.d

    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;

        public AuthController(IAuthRepository repo, IConfiguration config, IMapper mapper)
        {
            _repo = repo;
            _config = config;
            _mapper = mapper;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
        {
            // validate request

            // if(!ModelState.IsValid)
            //     return BadRequest(ModelState); 

            // nie musimy używać tego, używamy to gdy nie określimy ApiController'a
            // i ustawimy [FromBody] gdzie przychodzą nasze dane z widoku

            userForRegisterDto.Username = userForRegisterDto.Username.ToLower();

            if (await _repo.UserExists(userForRegisterDto.Username))
                return BadRequest("Username already exists");

            var userToCreate = _mapper.Map<User>(userForRegisterDto);

            var createdUser = await _repo.Register(userToCreate, userForRegisterDto.Password);

            var userToReturn = _mapper.Map<UserForDetailedDto>(createdUser);

            return CreatedAtRoute("GetUser", new {controller = "Users", id = createdUser.Id}, userToReturn);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            var userFromRepo = await _repo.Login(userForLoginDto.Username.ToLower(), userForLoginDto.Password);

            if (userFromRepo == null)
                return Unauthorized();

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userFromRepo.Id.ToString()),
                new Claim(ClaimTypes.Name, userFromRepo.UserName)
            }; // rozczenia potrzebne do tworzenia tokena 

            var key = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(_config.GetSection("AppSettings:Token").Value)); // tworzenie klucza do autoryzacji tokenu

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature); // poświadczenia autoryzacji i algorytm kodujący                  

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler(); // handler do tokena

            var token = tokenHandler.CreateToken(tokenDescriptor);  // nasz token, który będziemy mogli przekazać do klienta

            var user = _mapper.Map<UserForListDto>(userFromRepo); // dodanie zmiennej z mapowaniem na klasę bez haseł,
            // żeby ją zwrócić i mieć informacje o zalogowanym użytkowniku oraz żeby wyciągnąć zdjęcie do pasku nawigacyjnego

            return Ok(new
            {
                token = tokenHandler.WriteToken(token),
                user
            }); // zwracanie tokena + zalogowanego użytkownika

        }
    }
}