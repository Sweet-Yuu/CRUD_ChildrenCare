using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Pages.UserPages;

public class IndexModel : PageModel
{
    private readonly CRUD_ChildrenCareContext _context;

    public IndexModel(CRUD_ChildrenCareContext context)
    {
        _context = context;
    }

    public IList<User> User { get; set; } = default!;

    public async Task OnGetAsync()
    {
        User = await _context.User.ToListAsync();
    }
}
