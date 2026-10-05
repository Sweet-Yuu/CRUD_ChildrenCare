using CRUD_ChildrenCare.Models;

namespace CRUD_ChildrenCare.ViewModels.Home;

public class HomeViewModel
{
    public IEnumerable<Slider> Sliders { get; set; } = new List<Slider>();
    public IEnumerable<Post> FeaturedPosts { get; set; } = new List<Post>();
}
