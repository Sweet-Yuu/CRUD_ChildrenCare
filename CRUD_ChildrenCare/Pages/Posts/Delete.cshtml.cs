using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Pages.PostPages;

public class DeleteModel : PageModel
{
    private readonly CRUD_ChildrenCareContext _context;

    public DeleteModel(CRUD_ChildrenCareContext context)
    {
        _context = context;
    }

    [BindProperty]
    public CRUD_ChildrenCare.Models.Post Post { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var post = await _context.Post.FirstOrDefaultAsync(m => m.Id == id);
        if (post is null)
        {
            return NotFound();
        }
        else
        {
            Post = post;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var post = await _context.Post.FindAsync(id);
        if (post != null)
        {
            Post = post;
            _context.Post.Remove(Post);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
