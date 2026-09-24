using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Pages.ReservationPages;

public class DetailsModel : PageModel
{
    private readonly CRUD_ChildrenCareContext _context;
    public DetailsModel(CRUD_ChildrenCareContext context)
    {
        _context = context;
    }

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
}
