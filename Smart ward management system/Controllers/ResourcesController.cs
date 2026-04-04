using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs.Volunteer;
using Smart_ward_management_system.Model.Volunteer;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResourcesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ResourcesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Resources
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResourceDto>>> GetResources()
        {
            var resources = await _context.Resources
                .Select(r => new ResourceDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Type = r.Type,
                    Category = r.Category,
                    Description = r.Description,
                    Quantity = r.Quantity,
                    MinimumThreshold = r.MinimumThreshold,
                    Unit = r.Unit,
                    ExpiryDate = r.ExpiryDate,
                    StorageLocation = r.StorageLocation,
                    Supplier = r.Supplier,
                    UnitPrice = r.UnitPrice,
                    Status = r.Status,
                    LastUpdated = r.LastUpdated
                })
                .ToListAsync();

            return Ok(resources);
        }

        // GET: api/Resources/type/{type}
        [HttpGet("type/{type}")]
        public async Task<ActionResult<IEnumerable<ResourceDto>>> GetResourcesByType(string type)
        {
            var resources = await _context.Resources
                .Where(r => r.Type.ToLower() == type.ToLower())
                .Select(r => new ResourceDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Type = r.Type,
                    Category = r.Category,
                    Description = r.Description,
                    Quantity = r.Quantity,
                    MinimumThreshold = r.MinimumThreshold,
                    Unit = r.Unit,
                    ExpiryDate = r.ExpiryDate,
                    StorageLocation = r.StorageLocation,
                    Supplier = r.Supplier,
                    UnitPrice = r.UnitPrice,
                    Status = r.Status,
                    LastUpdated = r.LastUpdated
                })
                .ToListAsync();

            return Ok(resources);
        }

        // GET: api/Resources/lowstock
        [HttpGet("lowstock")]
        public async Task<ActionResult<IEnumerable<ResourceDto>>> GetLowStockResources()
        {
            var resources = await _context.Resources
                .Where(r => r.Quantity <= r.MinimumThreshold)
                .Select(r => new ResourceDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Type = r.Type,
                    Category = r.Category,
                    Description = r.Description,
                    Quantity = r.Quantity,
                    MinimumThreshold = r.MinimumThreshold,
                    Unit = r.Unit,
                    ExpiryDate = r.ExpiryDate,
                    StorageLocation = r.StorageLocation,
                    Supplier = r.Supplier,
                    UnitPrice = r.UnitPrice,
                    Status = r.Status,
                    LastUpdated = r.LastUpdated
                })
                .ToListAsync();

            return Ok(resources);
        }

        // GET: api/Resources/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ResourceDto>> GetResource(Guid id)
        {
            var resource = await _context.Resources
                .Where(r => r.Id == id)
                .Select(r => new ResourceDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Type = r.Type,
                    Category = r.Category,
                    Description = r.Description,
                    Quantity = r.Quantity,
                    MinimumThreshold = r.MinimumThreshold,
                    Unit = r.Unit,
                    ExpiryDate = r.ExpiryDate,
                    StorageLocation = r.StorageLocation,
                    Supplier = r.Supplier,
                    UnitPrice = r.UnitPrice,
                    Status = r.Status,
                    LastUpdated = r.LastUpdated
                })
                .FirstOrDefaultAsync();

            if (resource == null)
            {
                return NotFound();
            }

            return Ok(resource);
        }

        // POST: api/Resources
        [HttpPost]
        public async Task<ActionResult<Resource>> CreateResource(CreateResourceDto createDto)
        {
            var resource = new Resource
            {
                Name = createDto.Name,
                Type = createDto.Type,
                Category = createDto.Category,
                Description = createDto.Description,
                Quantity = createDto.Quantity,
                MinimumThreshold = createDto.MinimumThreshold,
                Unit = createDto.Unit,
                ExpiryDate = createDto.ExpiryDate,
                StorageLocation = createDto.StorageLocation,
                Supplier = createDto.Supplier,
                UnitPrice = createDto.UnitPrice,
                Status = createDto.Quantity > 0 ? "Available" : "Out of Stock",
                LastUpdated = DateTime.UtcNow
            };

            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetResource), new { id = resource.Id }, resource);
        }

        // PUT: api/Resources/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateResource(Guid id, UpdateResourceDto updateDto)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource == null)
            {
                return NotFound();
            }

            resource.Name = updateDto.Name;
            resource.Type = updateDto.Type;
            resource.Category = updateDto.Category;
            resource.Description = updateDto.Description;
            resource.Quantity = updateDto.Quantity;
            resource.MinimumThreshold = updateDto.MinimumThreshold;
            resource.Unit = updateDto.Unit;
            resource.ExpiryDate = updateDto.ExpiryDate;
            resource.StorageLocation = updateDto.StorageLocation;
            resource.Supplier = updateDto.Supplier;
            resource.UnitPrice = updateDto.UnitPrice;
            resource.Status = updateDto.Quantity > 0
                ? (updateDto.Quantity <= updateDto.MinimumThreshold ? "Low Stock" : "Available")
                : "Out of Stock";
            resource.LastUpdated = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ResourceExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // PATCH: api/Resources/{id}/quantity
        [HttpPatch("{id}/quantity")]
        public async Task<IActionResult> UpdateQuantity(Guid id, [FromBody] int newQuantity)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource == null)
            {
                return NotFound();
            }

            resource.Quantity = newQuantity;
            resource.Status = newQuantity > 0
                ? (newQuantity <= resource.MinimumThreshold ? "Low Stock" : "Available")
                : "Out of Stock";
            resource.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Resources/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteResource(Guid id)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource == null)
            {
                return NotFound();
            }

            _context.Resources.Remove(resource);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ResourceExists(Guid id)
        {
            return _context.Resources.Any(e => e.Id == id);
        }
    }
}
