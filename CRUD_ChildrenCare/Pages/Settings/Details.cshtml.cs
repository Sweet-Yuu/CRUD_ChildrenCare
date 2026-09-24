using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Pages.SettingPages;

public class DetailsModel : PageModel
{
    private readonly CRUD_ChildrenCareContext _context;
    public DetailsModel(CRUD_ChildrenCareContext context)
    {
        _context = context;
    }

    public Setting Setting { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var setting = await _context.Setting.FirstOrDefaultAsync(m => m.Id == id);
        if (setting is null)
        {
            return NotFound();
        }
        else
        {
            Setting = setting;
        }

        return Page();
    }
}
