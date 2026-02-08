using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Auth.Presentation.Models.Shared;

namespace TaskManagement.Auth.Presentation.Controllers
{
    /// <summary>
    /// Handles error rendering for the authentication UI.
    /// </summary>
    public class ErrorController : Controller
    {
        /// <summary>
        /// Displays the error page.
        /// </summary>
        /// <returns>The error view.</returns>
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true), Route("~/error")]
        public IActionResult Error()
        {
            // If the error originated from the OpenIddict server, render the error details.
            var response = HttpContext.GetOpenIddictServerResponse();
            if (response is not null)
            {
                return View(new ErrorViewModel
                {
                    Error = response.Error ?? string.Empty,
                    ErrorDescription = response.ErrorDescription ?? string.Empty
                });
            }

            return View(new ErrorViewModel());
        }
    }
}
