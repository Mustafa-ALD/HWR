using Microsoft.AspNetCore.Mvc;

namespace HWR.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorkItemController : ControllerBase
    {
        [HttpGet(Name = "work-items")]
        public string GetWorkItems()
        {
            

        }
    }
}
