using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Pages.ServicePages;

public class DetailsModel : PageModel
{
    private readonly CRUD_ChildrenCareContext _context;
    public DetailsModel(CRUD_ChildrenCareContext context)
    {
        _context = context;
    }

    public CRUD_ChildrenCare.Models.Service Service { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var service = await _context.Service.FirstOrDefaultAsync(m => m.Id == id);
        if (service is null)
        {
            return NotFound();
        }
        else
        {
            Service = service;
        }

        return Page();
    }
}
