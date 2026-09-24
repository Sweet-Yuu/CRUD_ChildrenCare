using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Pages.SettingPages;

public class CreateModel : PageModel
{
    private readonly CRUD_ChildrenCareContext _context;

    public CreateModel(CRUD_ChildrenCareContext context)
    {
        _context = context;
    }

    public IActionResult OnGet()
    {
        return Page();
    }

    [BindProperty]
    public Setting Setting { get; set; } = default!;

    // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.Setting.Add(Setting);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
