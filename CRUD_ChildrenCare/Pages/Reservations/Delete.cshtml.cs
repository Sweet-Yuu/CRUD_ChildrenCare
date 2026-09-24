using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Pages.ReservationPages;

public class DeleteModel : PageModel
{
    private readonly CRUD_ChildrenCareContext _context;

    public DeleteModel(CRUD_ChildrenCareContext context)
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
        else
        {
            Reservation = reservation;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var reservation = await _context.Reservation.FindAsync(id);
        if (reservation != null)
        {
            Reservation = reservation;
            _context.Reservation.Remove(Reservation);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
