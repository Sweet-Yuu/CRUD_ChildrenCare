using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Pages.ServicePages;

public class EditModel : PageModel
{
    private readonly CRUD_ChildrenCareContext _context;

    public EditModel(CRUD_ChildrenCareContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Service Service { get; set; } = default!;

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
        Service = service;
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

        _context.Attach(Service).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ServiceExists(Service.Id))
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

    private bool ServiceExists(int id)
    {
        return _context.Service.Any(e => e.Id == id);
    }
}
