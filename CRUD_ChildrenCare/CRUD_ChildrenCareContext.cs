using Microsoft.EntityFrameworkCore;

public class CRUD_ChildrenCareContext(DbContextOptions<CRUD_ChildrenCareContext> options) : DbContext(options)
{
    public DbSet<CRUD_ChildrenCare.Models.User> User { get; set; } = default!;
    public DbSet<CRUD_ChildrenCare.Models.Service> Service { get; set; } = default!;
    public DbSet<CRUD_ChildrenCare.Models.Post> Post { get; set; } = default!;
}
