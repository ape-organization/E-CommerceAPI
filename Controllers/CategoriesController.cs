using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;
using PharmacyAPI.Services;

namespace PharmacyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly PharmacyDbContext _context;
        private readonly ICategoryService categoryService;

        public CategoriesController(PharmacyDbContext context,ICategoryService category)
        {
            categoryService = category;
            _context = context;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            var cats= await categoryService.GetCategories();
            return cats;
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await categoryService.GetCategory(id);
            return category;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Category>> CreateCategory(Category dto)
        {
            var cat = await categoryService.CreateCategory(dto);
            return cat;
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCategory(int id, Category dto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            category.Name = dto.Name;

            await categoryService.UpdateCategory(id, category);
            return Ok(category);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            category.IsDeleted = true;
           
            await categoryService.DeleteCategory(id,category);
            return NoContent();
        }
    }
}
