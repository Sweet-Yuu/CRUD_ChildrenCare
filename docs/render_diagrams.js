const fs = require('fs');
const https = require('https');
const path = require('path');

const diagrams = [
  {
    name: 'fig_1_1_architecture.png',
    code: `graph TD
    Client["CLIENT LAYER (Browser / Device) - HTML5, Bootstrap 5, jQuery"]
    Middleware["ASP.NET CORE 8.0 MIDDLEWARE PIPELINE - Auth, Static Files, Routing"]
    Controllers["PRESENTATION LAYER - Public & Admin ServicesController, Razor Views"]
    BusinessServices["BUSINESS & APPLICATION SERVICES - AccountService, LocalAvatarStorage"]
    DataLayer["DATA ACCESS LAYER - Entity Framework Core, ApplicationDbContext"]
    Database[("DATABASE LAYER - SQL Server Database")]

    Client -->|HTTP / HTTPS Request| Middleware
    Middleware --> Controllers
    Controllers --> BusinessServices
    Controllers --> DataLayer
    BusinessServices --> DataLayer
    DataLayer -->|ADO.NET / SQL Driver| Database`
  },
  {
    name: 'fig_1_2_1_package_presentation.png',
    code: `graph TD
    subgraph Presentation_Layer ["Presentation Layer (CRUD_ChildrenCare)"]
        Controllers_Public["Controllers.Public (ServicesController, HomeController)"]
        Controllers_Admin["Controllers.Admin (ServicesController, UsersController)"]
        ViewModels_Public["ViewModels.Public (ServiceListPublicViewModel)"]
        ViewModels_Admin["ViewModels.Admin (ServiceFormViewModel)"]
        Views_Public["Views/Services (Index.cshtml, Details.cshtml)"]
        Views_Admin["Areas/Admin/Views/Services (Index, Create, Edit.cshtml)"]
    end

    Controllers_Public --> ViewModels_Public
    Controllers_Public --> Views_Public
    Controllers_Admin --> ViewModels_Admin
    Controllers_Admin --> Views_Admin`
  },
  {
    name: 'fig_1_2_2_package_core.png',
    code: `graph TD
    subgraph Core_And_Data_Layer ["Core and Data Access Layer"]
        Models["CRUD_ChildrenCare.Models (Service, ServiceImage, ServiceStatus)"]
        Data["CRUD_ChildrenCare.Data (ApplicationDbContext, DatabaseSeeder)"]
        Services_Files["CRUD_ChildrenCare.Services.Files (IAvatarStorage, LocalAvatarStorage)"]
        Services_Security["CRUD_ChildrenCare.Services.Security (ISecureTokenService, PasswordGenerator)"]
        Services_Menus["CRUD_ChildrenCare.Services.Menus (IAdminMenuService, AdminMenuService)"]
    end

    Data --> Models
    Services_Files --> Models`
  },
  {
    name: 'fig_2_1_1_class_diagram.png',
    code: `classDiagram
    class Service {
        +int Id
        +string Title
        +int CategoryId
        +string BriefInfo
        +string Description
        +int NumberOfPerson
        +decimal ListPrice
        +decimal SalePrice
        +int AvailableQuantity
        +bool IsFeatured
        +int Status
        +string ThumbnailUrl
        +DateTime CreatedAtUtc
        +DateTime UpdatedAtUtc
    }

    class ServiceImage {
        +int Id
        +int ServiceId
        +string ImageUrl
        +int SortOrder
        +DateTime CreatedAtUtc
    }

    class Setting {
        +int Id
        +string Name
        +string Value
        +string Type
        +bool IsActive
    }

    class PublicServicesController {
        +Index(string search, int categoryId, int page)
        +Details(int id)
    }

    class AdminServicesController {
        +Index(string search, int categoryId, int status, int page)
        +Create(ServiceFormViewModel model)
        +Edit(int id, ServiceFormViewModel model)
        +ToggleStatus(int id)
    }

    class ServiceListPublicViewModel {
        +List Items
        +List Categories
        +int CurrentPage
        +int TotalPages
    }

    class ServiceDetailPublicViewModel {
        +int Id
        +string Title
        +string CategoryName
        +decimal ListPrice
        +decimal SalePrice
        +List GalleryImages
    }

    class ServiceFormViewModel {
        +int Id
        +string Title
        +int CategoryId
        +decimal ListPrice
        +decimal SalePrice
        +IFormFile ThumbnailFile
        +List NewImageFiles
    }

    class IAvatarStorage {
        +SaveAsync(IFormFile file, CancellationToken ct)
        +DeleteAsync(string fileUrl, CancellationToken ct)
    }

    Service "1" o-- "many" ServiceImage : contains
    Service "1" --> "1" Setting : category
    PublicServicesController ..> ServiceListPublicViewModel : creates
    PublicServicesController ..> ServiceDetailPublicViewModel : creates
    AdminServicesController ..> ServiceFormViewModel : uses
    AdminServicesController --> IAvatarStorage : uses`
  },
  {
    name: 'fig_2_1_2_seq_public_list.png',
    code: `sequenceDiagram
    autonumber
    actor User as Guest / Customer
    participant View as ServicesIndexView
    participant Ctrl as PublicServicesController
    participant DB as ApplicationDbContext

    User ->> View: Truy cap trang /Services (search, categoryId, page)
    View ->> Ctrl: HTTP GET /Services?search={term}&categoryId={id}&page={p}
    activate Ctrl

    Ctrl ->> DB: Query danh muc Active (Type == ServiceCategory && IsActive)
    activate DB
    DB -->> Ctrl: List Categories
    deactivate DB

    Ctrl ->> DB: Query dich vu Active (filter search & category, sort UpdatedDate desc, Paging)
    activate DB
    DB -->> Ctrl: List Services & TotalCount
    deactivate DB

    Ctrl ->> Ctrl: Dong goi ServiceListPublicViewModel (Items, Categories, Pagination)

    Ctrl -->> View: View(viewModel) [HTTP 200 OK]
    deactivate Ctrl
    activate View
    View -->> User: Hien thi danh sach the dich vu, bo loc & phan trang
    deactivate View`
  },
  {
    name: 'fig_2_1_3_seq_public_detail.png',
    code: `sequenceDiagram
    autonumber
    actor User as Guest / Customer
    participant View as ServiceDetailsView
    participant Ctrl as PublicServicesController
    participant DB as ApplicationDbContext

    User ->> View: Nhap chon mot dich vu tu danh sach
    View ->> Ctrl: HTTP GET /Services/Details/{id}
    activate Ctrl

    Ctrl ->> DB: Services.Include(Category).Include(Images).FirstOrDefault(Id == id && Status == Active)
    activate DB
    DB -->> Ctrl: Service entity (Category & Images)
    deactivate DB

    alt Dich vu khong ton tai hoac Inactive
        Ctrl -->> View: NotFound() [HTTP 404]
        View -->> User: Hien thi thong bao loi 404
    else Dich vu hop le
        Ctrl ->> Ctrl: Anh xa ServiceDetailPublicViewModel (Title, Info, Price, GalleryImages)
        Ctrl -->> View: View(viewModel) [HTTP 200 OK]
        activate View
        View -->> User: Hien thi chi tiet dich vu, slider anh & nut [Dat Lich]
        deactivate View
    end
    deactivate Ctrl`
  },
  {
    name: 'fig_2_1_4_seq_manager_create.png',
    code: `sequenceDiagram
    autonumber
    actor Mgr as Manager / Admin
    participant View as ServiceCreateView
    participant Ctrl as AdminServicesController
    participant Storage as IAvatarStorage
    participant DB as ApplicationDbContext

    Mgr ->> View: Nhap thong tin & chon anh (Thumbnail + Extra Images)
    Mgr ->> View: Bam nut [Tao Moi / Save]
    View ->> Ctrl: HTTP POST /Admin/Services/Create (ServiceFormViewModel)
    activate Ctrl

    Ctrl ->> Ctrl: Kiem tra Authorize(Manager,Admin) & Validate(SalePrice <= ListPrice)

    alt Du lieu khong hop le
        Ctrl ->> DB: Query danh muc Active
        DB -->> Ctrl: List Categories
        Ctrl -->> View: View(model) kem loi ModelState
        View -->> Mgr: Hien thi lai form kem loi do
    else Du lieu hop le
        Ctrl ->> Storage: SaveAsync(ThumbnailFile)
        activate Storage
        Storage -->> Ctrl: thumbnailRelativeUrl
        deactivate Storage

        Ctrl ->> Ctrl: new Service(Title, ListPrice, SalePrice, ThumbnailUrl)
        Ctrl ->> DB: Services.Add(newService)
        Ctrl ->> DB: SaveChangesAsync()
        activate DB
        DB -->> Ctrl: Success (CSDL sinh newService.Id)
        deactivate DB

        opt Co tai len anh chi tiet
            loop Lap qua tung file anh chi tiet
                Ctrl ->> Storage: SaveAsync(file)
                activate Storage
                Storage -->> Ctrl: imageUrl
                deactivate Storage
                Ctrl ->> DB: ServiceImages.Add(new ServiceImage(newService.Id, imageUrl))
            end
            Ctrl ->> DB: SaveChangesAsync()
            activate DB
            DB -->> Ctrl: Success (Luu ServiceImages)
            deactivate DB
        end

        Ctrl -->> View: RedirectToAction(Index) [HTTP 302]
        deactivate Ctrl
        activate View
        View -->> Mgr: Dieu huong ve danh sach & hien thi thong bao thanh cong
        deactivate View
    end`
  },
  {
    name: 'fig_2_1_5_seq_manager_update.png',
    code: `sequenceDiagram
    autonumber
    actor Mgr as Manager / Admin
    participant View as ServiceEditOrListView
    participant Ctrl as AdminServicesController
    participant Storage as IAvatarStorage
    participant DB as ApplicationDbContext

    alt Luong 1: Cap nhat thong tin dich vu (Action: Edit)
        Mgr ->> View: Thay doi thong tin, chon thumbnail moi, chon xoa anh cu
        Mgr ->> View: Bam nut [Luu Thay Doi / Save]
        View ->> Ctrl: HTTP POST /Admin/Services/Edit/{id} (ServiceFormViewModel)
        activate Ctrl

        Ctrl ->> DB: Services.Include(Images).FirstOrDefault(Id == id)
        activate DB
        DB -->> Ctrl: existingService entity
        deactivate DB

        Ctrl ->> Ctrl: Validate (ModelState, SalePrice <= ListPrice)

        opt Co tai len Thumbnail moi
            Ctrl ->> Storage: SaveAsync(ThumbnailFile)
            activate Storage
            Storage -->> Ctrl: newThumbUrl
            deactivate Storage
            Ctrl ->> Ctrl: existingService.ThumbnailUrl = newThumbUrl
        end

        opt Co anh chi tiet can xoa (DeleteImageIds)
            Ctrl ->> DB: ServiceImages.RemoveRange(DeleteImages)
        end

        opt Co anh chi tiet moi tai len (NewImageFiles)
            loop Lap qua tung file
                Ctrl ->> Storage: SaveAsync(file)
                activate Storage
                Storage -->> Ctrl: newImgUrl
                deactivate Storage
                Ctrl ->> DB: ServiceImages.Add(new ServiceImage(id, newImgUrl))
            end
        end

        Ctrl ->> Ctrl: Cap nhat Title, Price, Description, UpdatedAtUtc
        Ctrl ->> DB: SaveChangesAsync()
        activate DB
        DB -->> Ctrl: Success
        deactivate DB

        Ctrl -->> View: RedirectToAction(Index) [HTTP 302]
        View -->> Mgr: Thong bao "Cap nhat dich vu thanh cong"

    else Luong 2: Bat/Tat trang thai hien thi nhanh (ToggleStatus - Xoa mem)
        Mgr ->> View: Nhap nut [Show / Hide] tai bang danh sach
        View ->> Ctrl: HTTP POST /Admin/Services/ToggleStatus/{id}
        
        Ctrl ->> DB: Services.FindAsync(id)
        activate DB
        DB -->> Ctrl: service entity
        deactivate DB

        Ctrl ->> Ctrl: service.Status = (Active ? Inactive : Active)
        Ctrl ->> DB: SaveChangesAsync()
        activate DB
        DB -->> Ctrl: Success
        deactivate DB

        Ctrl -->> View: RedirectToAction(Index) hoac Json(success)
        deactivate Ctrl
        View -->> Mgr: Cap nhat badge trang thai (Active / Inactive) ngay tren bang
    end`
  }
];

async function downloadOne(d) {
  return new Promise((resolve, reject) => {
    const obj = { code: d.code, mermaid: { theme: 'default' } };
    const b64 = Buffer.from(JSON.stringify(obj)).toString('base64');
    const url = 'https://mermaid.ink/img/' + b64;
    const dest = path.join(__dirname, 'images', d.name);

    https.get(url, (res) => {
      if (res.statusCode !== 200) {
        return reject(new Error('Failed ' + d.name + ' HTTP ' + res.statusCode));
      }
      const chunks = [];
      res.on('data', c => chunks.push(c));
      res.on('end', () => {
        const buf = Buffer.concat(chunks);
        fs.writeFileSync(dest, buf);
        console.log('Saved:', d.name, buf.length, 'bytes');
        resolve();
      });
    }).on('error', reject);
  });
}

async function run() {
  for (const d of diagrams) {
    try {
      await downloadOne(d);
    } catch (e) {
      console.error(e.message);
    }
  }
  console.log('All done!');
}

run();
