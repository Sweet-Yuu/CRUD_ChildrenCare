using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Pages.PostPages;

public class DetailsModel : PageModel
{
    private readonly CRUD_ChildrenCareContext _context;
    public DetailsModel(CRUD_ChildrenCareContext context)
    {
        _context = context;
    }

    public Post Post { get; set; } = default!;

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
}
