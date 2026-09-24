using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Pages.ReservationDetailPages;

public class EditModel : PageModel
{
    private readonly CRUD_ChildrenCareContext _context;

    public EditModel(CRUD_ChildrenCareContext context)
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
        ReservationDetail = reservationdetail;
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

        _context.Attach(ReservationDetail).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ReservationDetailExists(ReservationDetail.Id))
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

    private bool ReservationDetailExists(int id)
    {
        return _context.ReservationDetail.Any(e => e.Id == id);
    }
}
