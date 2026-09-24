using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Pages.ReservationPages;

public class EditModel : PageModel
{
    private readonly CRUD_ChildrenCareContext _context;

    public EditModel(CRUD_ChildrenCareContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Reservation Reservation { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var reservation = await _context.Reservation.FirstOrDefaultAsync(m => m.Id == id);
        if (reservation is null)
        {
            return NotFound();
        }
        Reservation = reservation;
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

        _context.Attach(Reservation).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ReservationExists(Reservation.Id))
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

    private bool ReservationExists(int id)
    {
        return _context.Reservation.Any(e => e.Id == id);
    }
}
