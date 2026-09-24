using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Pages.SettingPages;

public class EditModel : PageModel
{
    private readonly CRUD_ChildrenCareContext _context;

    public EditModel(CRUD_ChildrenCareContext context)
    {
        _context = context;
    }

    [BindProperty]
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
        Setting = setting;
        return Page();
    }

    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.Attach(Setting).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SettingExists(Setting.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return RedirectToPage("./Index");
    }

    private bool SettingExists(int id)
    {
        return _context.Setting.Any(e => e.Id == id);
    }
}
