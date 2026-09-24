using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Pages.ServicePages;

public class IndexModel : PageModel
{
    private readonly CRUD_ChildrenCareContext _context;

    public IndexModel(CRUD_ChildrenCareContext context)
    {
        _context = context;
    }

    public IList<CRUD_ChildrenCare.Models.Service> Service { get; set; } = default!;

    public async Task OnGetAsync()
    {
        Service = await _context.Service.ToListAsync();
    }
}
