using Microsoft.AspNetCore.Mvc;

namespace AspRpc.Controllers
{
    /// <summary>
    /// Home controller.
    /// </summary>
    [Route("/")]
    public class HomeController
    {
        /// <summary>
        /// Gets the home page.
        /// </summary>
        /// <returns>Home page HTML code.</returns>
        [HttpGet]
        public RedirectResult Get()
        {
            return new RedirectResult("/Site/Index.html");
        }

        /// <summary>
        /// Gets the fav-icon.
        /// </summary>
        /// <returns>Fav-icon.</returns>
        [HttpGet]
        [Route("/favicon.ico")]
        public object GetFavIcon()
        {
            return null;
        }
    }
}
