using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/users/{userId}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinarySettings;
        private Cloudinary _cloudinary;

        public PhotosController(IDatingRepository repo, IMapper mapper, IOptions<CloudinarySettings> cloudinarySettings)
        {
            _cloudinarySettings = cloudinarySettings;
            _mapper = mapper;
            _repo = repo;

            Account account = new Account(
                _cloudinarySettings.Value.CloudName,
                _cloudinarySettings.Value.ApiKey,
                _cloudinarySettings.Value.ApiSecret 
            );  // utworzenie instancji obiektu nowego Konta dla serwisu Cloudinary z pobraniem ustawień z pliku .json

            _cloudinary = new Cloudinary(account); // przekazanie konta jako parametr
        }

        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photoFromRepo = await _repo.GetPhoto(id);

            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepo);

            return Ok(photo);
        }

        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, [FromForm]PhotoForCreationDto photoForCreationDto)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized(); // sprawdzamy czy użytkownik jest bieżącym użytkownikiem 

            var userFromRepo = await _repo.GetUser(userId);

            var file = photoForCreationDto.File;

            var uploadResult = new ImageUploadResult();           

            if (file.Length > 0)
            {
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream),
                        Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
                        // możemy z góry określić jakie zdjęcia będą przechowywane, szerokość wysokość wycięcie na twarz
                    };

                    uploadResult = _cloudinary.Upload(uploadParams);
                }               
            }

            photoForCreationDto.Url = uploadResult.Url.ToString();
            photoForCreationDto.PublicId = uploadResult.PublicId;   // przypisanie adresu url i id zdjęcia do właściwości klasy

            var photo = _mapper.Map<Photo>(photoForCreationDto);

            if (!userFromRepo.Photos.Any(u => u.IsMain))
                photo.IsMain = true;    // ustawienie zdjęcia na główne jeśli nie ma jeszcze zdjęć

            userFromRepo.Photos.Add(photo); // dodanie do zdjęć

            if (await _repo.SaveAll())
            {
                var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
                return CreatedAtRoute("GetPhoto", new { userId = userId, id = photo.Id}, photoToReturn); // pierwszy parametr określa zdjęcie, do którego napisaliśmy metodę HttpGet(id) wyżej
            }           

            return BadRequest("Could not add the photo");
        }

        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized(); // sprawdzamy czy użytkownik jest bieżącym użytkownikiem 

                var user = await _repo.GetUser(userId);

            if (!user.Photos.Any(p => p.Id == id))
                return Unauthorized(); // jeśli, żadne id zdjęcia nie pasuje do tego w tabeli zdjęcia zwracamy nieautoryzowany dostęp

            var photoFromRepo =  await _repo.GetPhoto(id);

            if (photoFromRepo.IsMain)
                return BadRequest("This is already the main photo"); // sprawdzamy czy zdjęcie jest już głównym

            var currentMainPhoto = await _repo.GetMainPhotoForUser(userId);
            currentMainPhoto.IsMain = false; // pobranie aktualnego zdjęcia i ustawienie, że nie jest główne
        
            photoFromRepo.IsMain = true; // ustawienie innego zdjęcia na główne

            if (await _repo.SaveAll())
                return NoContent(); // jeśli się powiodło nic nie zwracamy

            return BadRequest("Could not set photo to main");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id)
        {
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized(); // sprawdzamy czy użytkownik jest bieżącym użytkownikiem 

                var user = await _repo.GetUser(userId);

            if (!user.Photos.Any(p => p.Id == id))
                return Unauthorized(); // jeśli, żadne id zdjęcia nie pasuje do tego w tabeli zdjęcia zwracamy nieautoryzowany dostęp

            var photoFromRepo =  await _repo.GetPhoto(id);

            if (photoFromRepo.IsMain)
                return BadRequest("You cannot delete your main photo"); // sprawdzamy czy zdjęcie jest już głównym

            if (photoFromRepo.PublicId != null)
            {
                var deleteParams = new DeletionParams(photoFromRepo.PublicId); // parametry, czyli publicID

                var result = _cloudinary.Destroy(deleteParams); // rezultat z serwisu cloudinary

                if (result.Result == "ok")            
                    _repo.Delete(photoFromRepo); // jeśli się powiedzie to usuwamy    
            }

            if(photoFromRepo.PublicId == null) // dla zdjęć, które nie są w serwisie cloudinary
                _repo.Delete(photoFromRepo);

            if (await _repo.SaveAll()) // i zapisujemy zmiany
                return Ok();

            return BadRequest("Failed to delete the photo");
        }
    }
}