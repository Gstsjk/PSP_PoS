﻿using PSP_PoS.Data;

namespace PSP_PoS.Components.CategoryComponent
{
    public class CategoryService : ICategoryService
    {
        private readonly DataContext _context;

        public CategoryService(DataContext context)
        {
            _context = context;
        }

        public List<Category> GetAllCategories()
        {
            return _context.Categories.ToList();
        }
        public Category? GetCategoryById(Guid categoryId)
        {
            return _context.Categories.FirstOrDefault(t => t.Id == categoryId)!;
        }
        public Category AddCategory(CategoryDto categoryDto)
        { 
            Category category = new Category(categoryDto);
            _context.Categories.Add(category);
            _context.SaveChanges();
            return category;
        }
        public bool UpdateCategory(CategoryDto categoryDto, Guid id)
        {
            Category? category = _context.Categories.Find(id);
            if (category == null)
            {
                return false;
            }

            category.Name = categoryDto.Name;

            _context.Categories.Update(category);
            _context.SaveChanges();
            return true;
        }

        public void DeleteCategory(Guid categoryId)
        {
            var category = _context.Categories.FirstOrDefault(t => t.Id == categoryId);

            if (category != null)
            {
                _context.Categories.Remove(category);
                _context.SaveChanges();
            }
        }
    }
}
