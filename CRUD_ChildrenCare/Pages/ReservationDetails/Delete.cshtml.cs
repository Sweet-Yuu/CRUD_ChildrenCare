using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Pages.ReservationDetailPages;

public class DeleteModel : PageModel
{
    private readonly CRUD_ChildrenCareContext _context;

    public DeleteModel(CRUD_ChildrenCareContext context)
    {
        _context = context;
    }

    [BindProperty]
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

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var reservationdetail = await _context.ReservationDetail.FindAsync(id);
        if (reservationdetail != null)
        {
            ReservationDetail = reservationdetail;
            _context.ReservationDetail.Remove(ReservationDetail);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
