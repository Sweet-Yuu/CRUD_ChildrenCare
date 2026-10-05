using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CRUD_ChildrenCare.Areas.Admin.ViewModels.Services;

public sealed class ServiceFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    public string Thumbnail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Brief information is required.")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Brief info must be between 10 and 500 characters.")]
    public string BriefInfo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    public string Description { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Number of person must be at least 1.")]
    public int NumberOfPerson { get; set; } = 1;

    [Range(1, 1000000000, ErrorMessage = "List price must be greater than 0.")]
    public decimal ListPrice { get; set; }

    [Range(1, 1000000000, ErrorMessage = "Sale price must be greater than 0.")]
    public decimal SalePrice { get; set; }

    [Range(0, 10000, ErrorMessage = "Available quantity cannot be negative.")]
    public int AvailableQuantity { get; set; }

    public bool IsFeatured { get; set; }

    public ServiceStatus Status { get; set; } = ServiceStatus.Inactive;

    public IReadOnlyList<Setting> Categories { get; set; } = [];
    public IReadOnlyList<ServiceImage> ExistingImages { get; set; } = [];

    public IFormFile? ThumbnailFile { get; set; }
    public List<IFormFile>? NewImageFiles { get; set; }
}
