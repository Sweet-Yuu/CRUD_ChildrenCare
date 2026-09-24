using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Pages.UserPages;

public class DeleteModel : PageModel
{
    private readonly CRUD_ChildrenCareContext _context;

    public DeleteModel(CRUD_ChildrenCareContext context)
    {
        _context = context;
    }

    [BindProperty]
    public User User { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var user = await _context.User.FirstOrDefaultAsync(m => m.ID == id);
        if (user is null)
        {
            return NotFound();
        }
        else
        {
            User = user;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var user = await _context.User.FindAsync(id);
        if (user != null)
        {
            User = user;
            _context.User.Remove(User);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
