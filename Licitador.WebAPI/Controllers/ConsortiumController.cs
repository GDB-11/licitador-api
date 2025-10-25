using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Licitador.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConsortiumController
{
    
}