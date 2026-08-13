using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Models;

namespace PharmacyAPI.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> GetCategories();
        Task<Category> GetCategory(int id);
        Task<ActionResult<Category>> CreateCategory(Category dto);
        Task UpdateCategory(int id, Category category);
        Task DeleteCategory(int id, Category category);
    }
    public class CategoryService:ICategoryService
    {
        private readonly PharmacyDbContext _context;
        public CategoryService(PharmacyDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetCategories()
        {
            var cats = await _context.Categories.Where(cat => cat.IsDeleted == false).ToListAsync();
            return cats;
        }

        public async Task<Category> GetCategory(int id)
        {
            var category = _context.Categories.Where(cat => cat.Id == id && cat.IsDeleted == false).FirstOrDefault();
            if (category == null) return (null);
            return category;
        }

        public async Task<ActionResult<Category>> CreateCategory(Category dto)
        {
            if (dto == null) return null;
            _context.Categories.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }


        public async Task UpdateCategory(int id, Category category)
        {
            
            

             _context.Update(category);
            _context.SaveChangesAsync();
            
        }

        public async Task DeleteCategory(int id,Category category)
        {
         
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
         
        }


    }

}
