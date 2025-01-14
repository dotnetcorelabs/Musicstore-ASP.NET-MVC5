﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicStore.WebHost.Data;
using MusicStore.WebHost.Models;
using MusicStore.WebHost.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MusicStore.Controllers
{
    public class StoreController : Controller
    {
        //
        // GET: /Store/
        public async Task<IActionResult> Index([FromServices]IGenreRepository genreRepository, CancellationToken cancellationToken = default)
        {
            var genres = await genreRepository.ToList(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return View(genres);
        }
        //
        // GET: /Store/Browse
        [HttpGet("[controller]/[action]/{genreName:required}")]
        public async Task<IActionResult> Browse([FromRoute] string genreName, [FromServices] IGenreRepository genreRepository, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IEnumerable<Album> albums = await genreRepository.AlbumsFromGenre(genreName, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            ViewData["GenreName"] = genreName;

            return View(albums);
        }
        //
        // GET: /Store/Details
        public async Task<IActionResult> Details([FromRoute] int? id, [FromServices] IAlbumRepository albumRepository, CancellationToken cancellationToken = default)
        {
            if (id == null)
                return BadRequest();

            Album album = await albumRepository.FindAlbum(id.Value, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            return View(album);
        }
    }
}