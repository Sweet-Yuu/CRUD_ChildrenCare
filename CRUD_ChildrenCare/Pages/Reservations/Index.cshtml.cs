using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.Pages.ReservationPages;

public class IndexModel : PageModel
{
    private readonly CRUD_ChildrenCareContext _context;

    public IndexModel(CRUD_ChildrenCareContext context)
    {
        _context = context;
    }

    public IList<Reservation> Reservation { get; set; } = default!;

    public async Task OnGetAsync()
    {
        Reservation = await _context.Reservation.ToListAsync();
    }
}
