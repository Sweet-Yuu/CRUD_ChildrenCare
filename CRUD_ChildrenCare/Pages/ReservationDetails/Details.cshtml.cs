using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Pages.ReservationDetailPages;

public class DetailsModel : PageModel
{
    private readonly CRUD_ChildrenCareContext _context;
    public DetailsModel(CRUD_ChildrenCareContext context)
    {
        _context = context;
    }

    public ReservationDetail ReservationDetail { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var reservationdetail = await _context.ReservationDetail.FirstOrDefaultAsync(m => m.Id == id);
        if (reservationdetail is null)
        {
            return NotFound();
        }
        else
        {
            ReservationDetail = reservationdetail;
        }

        return Page();
    }
}
