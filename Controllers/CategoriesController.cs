using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;
using PharmacyAPI.Services;

namespace PharmacyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService categoryService;


        public CategoriesController(
            ICategoryService category
        )
        {
            categoryService = category;
        }


        // =====================================================
        // GET MENU
        // =====================================================

        [HttpGet("menu")]
        [AllowAnonymous]
        public async Task<ActionResult<List<CategoryMenu>>>
            GetCategoriesForMenu()
        {
            var categories =
                await categoryService
                    .GetCategoriesForMenu();

            return Ok(categories);
        }


        // =====================================================
        // GET ALL
        // =====================================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Category>>>
            GetCategories()
        {
            var cats =
                await categoryService
                    .GetCategories();

            return Ok(cats);
        }


        // =====================================================
        // GET BY ID
        // =====================================================

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Category>>
            GetCategory(int id)
        {
            var category =
                await categoryService
                    .GetCategory(id);


            if (category == null)
                return NotFound();


            return Ok(category);
        }


        // =====================================================
        // CREATE
        // =====================================================

        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Category>>
            CreateCategory(
                [FromForm] CreateCategoryRequest dto
            )
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(
                    "Category name is required."
                );
            }


            var category =
                await categoryService
                    .CreateCategory(dto);


            return CreatedAtAction(
                nameof(GetCategory),
                new
                {
                    id = category.Id
                },
                category
            );
        }


        // =====================================================
        // UPDATE
        // =====================================================

        [HttpPut("{id}")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult>
            UpdateCategory(
                int id,
                [FromForm] CreateCategoryRequest dto
            )
        {
            try
            {
                await categoryService
                    .UpdateCategory(
                        id,
                        dto
                    );


                var category =
                    await categoryService
                        .GetCategory(id);


                return Ok(category);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }


        // =====================================================
        // DELETE
        // =====================================================

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult>
            DeleteCategory(int id)
        {
            var category =
                await categoryService
                    .GetCategory(id);


            if (category == null)
                return NotFound();


            await categoryService
                .DeleteCategory(
                    id,
                    category
                );


            return NoContent();
        }
    }
}