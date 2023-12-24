﻿using PSP_PoS.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using PSP_PoS.Components.Tax;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PSP_PoS.Components.Category
{
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        [Required]
        public string Name { get; set; }

        public Category()
        {

        }
        public Category(CategoryDto categoryDto)
        {
            Name = categoryDto.Name;
        }
    }
}