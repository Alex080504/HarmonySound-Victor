﻿using HarmonySound.Models;
using HarmonySound.API.Consumer;
using HarmonySound.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace HarmonySound.MVC.Controllers
{
    [Authorize(Roles = "artist")]
    public class ArtistsController : Controller
    {
        private readonly HttpClient _httpClient;

        public ArtistsController(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromMinutes(10);
        }

        // Vista Home del artista
        public async Task<IActionResult> Home()
        {
            int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"https://localhost:7120/api/Users/profile/{userId}");
                if (!response.IsSuccessStatusCode)
                    return View("Error");

                var json = await response.Content.ReadAsStringAsync();
                var dto = System.Text.Json.JsonSerializer.Deserialize<ProfileEditViewModel>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                ViewBag.Success = TempData["Success"];
                return View(dto);
            }
        }

        // GET: Editar perfil
        public async Task<IActionResult> EditProfile()
        {
            int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"https://localhost:7120/api/Users/profile/{userId}");
                if (!response.IsSuccessStatusCode)
                    return View("Error");

                var json = await response.Content.ReadAsStringAsync();
                var dto = System.Text.Json.JsonSerializer.Deserialize<ProfileEditViewModel>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return View(dto);
            }
        }

        // POST: Editar perfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(ProfileEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Some error message");
                return View(model);
            }

            // Subida de imagen
            if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
            {
                using (var client = new HttpClient())
                {
                    var form = new MultipartFormDataContent();
                    form.Add(new StringContent(model.Id.ToString()), "UserId");
                    form.Add(new StreamContent(model.ProfileImageFile.OpenReadStream()), "file", model.ProfileImageFile.FileName);

                    var response = await client.PostAsync("https://localhost:7120/api/Users/upload-profile-image", form);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var result = System.Text.Json.JsonDocument.Parse(json);
                        if (result.RootElement.TryGetProperty("ProfileImageUrl", out var urlProp) ||
                            result.RootElement.TryGetProperty("profileImageUrl", out urlProp))
                        {
                            model.ProfileImageUrl = urlProp.GetString();
                        }
                        else
                        {
                            ModelState.AddModelError("", "No se recibió la URL de la imagen del servidor.");
                            return View(model);
                        }
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        TempData["Error"] = "Error uploading profile image: " + error;
                        return View(model);
                    }
                }
            }
            else
            {
                // Si no se sube imagen, recupera la actual
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"https://localhost:7120/api/Users/profile/{model.Id}");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var dto = System.Text.Json.JsonSerializer.Deserialize<ProfileEditViewModel>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        model.ProfileImageUrl = dto?.ProfileImageUrl;
                    }
                }
            }

            // Actualizar el resto del perfil
            var userUpdate = new
            {
                Id = model.Id,
                Name = model.Name,
                Biography = model.Biography,
                ProfileImageUrl = model.ProfileImageUrl,
                Email = model.Email
            };

            using (var client = new HttpClient())
            {
                var json = System.Text.Json.JsonSerializer.Serialize(userUpdate);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PutAsync($"https://localhost:7120/api/Users/profile/{model.Id}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = "Error updating profile: " + error;
                    return View(model);
                }
            }
            TempData["Success"] = "Perfil actualizado correctamente.";
            return RedirectToAction("Home");
        }

        // ✅ NUEVO: Vista Upload - Solo para mostrar el formulario de subida
        public IActionResult Upload()
        {
            return View();
        }

        // ✅ MODIFICADO: UploadAudio - Redirige a Upload después de subir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAudio(IFormCollection form)
        {
            try
            {
                var file = form.Files["File"];
                if (file == null || file.Length == 0)
                {
                    TempData["Error"] = "Archivo no válido.";
                    return RedirectToAction("Upload");
                }

                System.Diagnostics.Debug.WriteLine($"Nombre: {file.FileName}, Tamaño: {file.Length}");

                if (string.IsNullOrWhiteSpace(form["Title"]) || string.IsNullOrWhiteSpace(form["Type"]) || string.IsNullOrWhiteSpace(form["ArtistId"]))
                {
                    TempData["Error"] = "Todos los campos son obligatorios.";
                    return RedirectToAction("Upload");
                }

                using var content = new MultipartFormDataContent();
                content.Add(new StringContent(form["Title"]), "Title");
                content.Add(new StringContent(form["Type"]), "Type");
                content.Add(new StringContent(form["ArtistId"]), "ArtistId");

                if (file.Length > 50 * 1024 * 1024)
                {
                    TempData["Error"] = "El archivo es demasiado grande.";
                    return RedirectToAction("Upload");
                }

                content.Add(new StreamContent(file.OpenReadStream()), "File", file.FileName);

                var response = await _httpClient.PostAsync("https://localhost:7120/api/Contents/upload", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Archivo subido con éxito.";
                }
                else
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Error al subir el archivo: {errorMsg}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Excepción: " + ex.ToString();
                System.Diagnostics.Debug.WriteLine("Excepción: " + ex.ToString());
            }

            return RedirectToAction("Upload");
        }

        // ✅ MODIFICADO: Index - Para mostrar las canciones con reproductor
        public async Task<IActionResult> Index()
        {
            var nameIdentifierClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (nameIdentifierClaim == null || !int.TryParse(nameIdentifierClaim.Value, out int artistId))
            {
                ViewBag.Error = "Tu sesión no es válida. Por favor, cierra sesión y vuelve a iniciar sesión.";
                return View("Error401");
            }

            Crud<Content>.EndPoint = "https://localhost:7120/api/Contents";
            var allContents = Crud<Content>.GetAll();
            var myContents = allContents.Where(c => c.ArtistId == artistId).ToList();
            return View(myContents);
        }
    }
}
