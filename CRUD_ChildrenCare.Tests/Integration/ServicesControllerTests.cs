using CRUD_ChildrenCare.Data;
using CRUD_ChildrenCare.Models;
using CRUD_ChildrenCare.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CRUD_ChildrenCare.Tests.Integration;

public sealed class ServicesControllerTests
{
    [Fact]
    public async Task ServiceModel_DefaultsAndPropertyAssignment_WorksCorrectly()
    {
        var service = new Service
        {
            Title = "General Pediatric Checkup",
            BriefInfo = "Comprehensive health check for kids",
            Description = "<p>Full physical examination and vaccination check.</p>",
            ListPrice = 500000,
            SalePrice = 450000,
            AvailableQuantity = 20,
            IsFeatured = true,
            Status = ServiceStatus.Active
        };

        Assert.Equal("General Pediatric Checkup", service.Title);
        Assert.Equal(1, service.NumberOfPerson);
        Assert.Equal(ServiceStatus.Active, service.Status);
        Assert.True(service.IsFeatured);
        Assert.Equal(450000, service.SalePrice);
    }

    [Fact]
    public async Task ServiceImageModel_PropertyAssignment_WorksCorrectly()
    {
        var image = new ServiceImage
        {
            ServiceId = 1,
            ImageUrl = "/uploads/service1_detail1.jpg",
            SortOrder = 1
        };

        Assert.Equal(1, image.ServiceId);
        Assert.Equal("/uploads/service1_detail1.jpg", image.ImageUrl);
        Assert.Equal(1, image.SortOrder);
    }
}
