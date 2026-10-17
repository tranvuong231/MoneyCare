// danh mục  thu/chi
using MoneyCare.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; //kiểm tra dữ liệu

namespace MoneyCare.Models;

public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
    [StringLength(100, ErrorMessage = "Tên danh mục tối đa 100 ký tự")]
    [Display(Name = "Tên danh mục")]
    public string TenDanhMuc { get; set; } = string.Empty;

    [Display(Name = "Loại")]
    public LoaiGiaoDich Loai { get; set; }

    [Display(Name = "Màu sắc")]
    [StringLength(20)]
    public string MauSac { get; set; } = "#0d6efd";

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public ICollection<Transaction>? Transactions { get; set; }
}
