using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyAPI.Models;
using PharmacyAPI.Models.RequestsModels;
using PharmacyAPI.Services;

namespace PharmacyAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BrandsController : ControllerBase
    {
        private readonly IBrandService _brandService;


        public BrandsController(
            IBrandService brandService)
        {
            _brandService = brandService;
        }


        // =====================================================
        // GET ALL
        // =====================================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<Brand>>>
            GetBrands(
                CancellationToken cancellationToken)
        {
            var brands = await _brandService
                .GetBrands(cancellationToken);

            return Ok(brands);
        }


        // =====================================================
        // GET BY ID
        // =====================================================

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Brand>>
            GetBrand(
                int id,
                CancellationToken cancellationToken)
        {
            var brand = await _brandService
                .GetBrand(
                    id,
                    cancellationToken);


            if (brand is null)
            {
                return NotFound();
            }


            return Ok(brand);
        }


        // =====================================================
        // CREATE
        // =====================================================

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Brand>>
            CreateBrand(
                [FromForm] BrandRequest request,
                CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest(
                    "Brand data is required.");
            }


            if (string.IsNullOrWhiteSpace(request.NameEn) &&
                string.IsNullOrWhiteSpace(request.NameAr))
            {
                return BadRequest(
                    "Brand name is required.");
            }


            try
            {
                var brand = await _brandService
                    .CreateBrand(
                        request,
                        cancellationToken);


                return CreatedAtAction(
                    nameof(GetBrand),
                    new
                    {
                        id = brand.Id
                    },
                    brand);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }


        // =====================================================
        // UPDATE
        // =====================================================

        [HttpPut("{id:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult>
            UpdateBrand(
                int id,
                [FromForm] BrandRequest request,
                CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest(
                    "Brand data is required.");
            }


            if (string.IsNullOrWhiteSpace(request.NameEn) &&
                string.IsNullOrWhiteSpace(request.NameAr))
            {
                return BadRequest(
                    "Brand name is required.");
            }


            try
            {
                var result = await _brandService
                    .UpdateBrand(
                        id,
                        request,
                        cancellationToken);


                if (!result)
                {
                    return NotFound();
                }


                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }


        // =====================================================
        // DELETE
        // =====================================================

        [HttpDelete("{id:int}")]
        public async Task<IActionResult>
            DeleteBrand(
                int id,
                CancellationToken cancellationToken)
        {
            var result = await _brandService
                .DeleteBrand(
                    id,
                    cancellationToken);


            if (!result)
            {
                return NotFound();
            }


            return NoContent();
        }
    }
}