using HarmonySound.API.Consumer;
using HarmonySound.Models;
using HarmonySound.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
namespace HarmonySound.MVC.Controllers
{
    [Authorize]
    public class ArtistsController : Controller
    {
        private readonly HttpClient _httpClient;

        public ArtistsController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAudio(ContentUploadDto model)
        {
            try
            {
                if (model.File == null || model.File.Length == 0)
                {
                    ModelState.AddModelError("", "Por favor selecciona un archivo.");
                    return View();
                }

                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(model.Title), "Title");
                formData.Add(new StringContent(model.Type), "Type");
                formData.Add(new StringContent(model.ArtistId.ToString()), "ArtistId");
                formData.Add(new StreamContent(model.File.OpenReadStream()), "File", model.File.FileName);

                var response = await _httpClient.PostAsync("https://localhost:7120/api/Contents/upload", formData);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return RedirectToAction("Index"); // O lo que desees hacer con la respuesta
                }
                else
                {
                    ModelState.AddModelError("", "Error al subir el archivo.");
                    return View();
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al intentar procesar el archivo.");
                return View();
            }
        }
        public async Task<IActionResult> Index()
        {
            var nameIdentifierClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (nameIdentifierClaim == null || !int.TryParse(nameIdentifierClaim.Value, out int artistId))
            {
                ViewBag.Error = "Tu sesión no es válida. Por favor, cierra sesión y vuelve a iniciar sesión.";
                return View("Error401");
            }

            Crud<Content>.EndPoint = "https://localhost:7120/api/Contents";
            var allContents = Crud<Content>.GetAll(); // Cambia a async si es posible
            var myContents = allContents.Where(c => c.ArtistId == artistId).ToList();
            return View(myContents);
        }

        public async Task<IActionResult> Details(int id)
        {
            // Obtener detalles del artista con su ID desde la API
            Crud<User>.EndPoint = "https://localhost:7160/api/Users"; // Ajusta el endpoint a tu API

            // Obtener los datos del artista desde la API de manera asincrónica
            var artist = await Task.Run(() => Crud<User>.GetById(id));

            if (artist == null)
                return NotFound(); // Si no se encuentra al artista, retornar un error 404

            // Pasar los datos del artista a la vista
            return View(artist);
        }
    }
}
