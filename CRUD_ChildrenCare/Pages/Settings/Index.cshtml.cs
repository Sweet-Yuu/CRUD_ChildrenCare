using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Pages.SettingPages;

public class IndexModel : PageModel
{
    private readonly CRUD_ChildrenCareContext _context;

    public IndexModel(CRUD_ChildrenCareContext context)
    {
        _context = context;
    }

    public IList<Setting> Setting { get; set; } = default!;

    public async Task OnGetAsync()
    {
        Setting = await _context.Setting.ToListAsync();
    }
}
