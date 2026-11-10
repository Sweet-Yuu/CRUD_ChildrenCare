const fs = require('fs');
const https = require('https');
const path = require('path');

const missing = [
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
    }
    class ServiceImage {
        +int Id
        +int ServiceId
        +string ImageUrl
        +int SortOrder
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
    Service "1" o-- "many" ServiceImage : contains
    Service "1" --> "1" Setting : category`
  },
  {
    name: 'fig_2_1_2_seq_public_list.png',
    code: `sequenceDiagram
    autonumber
    actor User as Guest / Customer
    participant View as ServicesIndexView
    participant Ctrl as PublicServicesController
    participant DB as ApplicationDbContext

    User->>View: Access /Services with filters
    View->>Ctrl: GET /Services?search=term&categoryId=id&page=p
    activate Ctrl
    Ctrl->>DB: Query Active Categories
    DB-->>Ctrl: Categories List
    Ctrl->>DB: Query Active Services (Filter, Sort, Paging)
    DB-->>Ctrl: Services List and TotalCount
    Ctrl->>Ctrl: Build ServiceListPublicViewModel
    Ctrl-->>View: View(viewModel) [200 OK]
    deactivate Ctrl
    View-->>User: Render service card items and pagination`
  },
  {
    name: 'fig_2_1_3_seq_public_detail.png',
    code: `sequenceDiagram
    autonumber
    actor User as Guest / Customer
    participant View as ServiceDetailsView
    participant Ctrl as PublicServicesController
    participant DB as ApplicationDbContext

    User->>View: Click service item
    View->>Ctrl: GET /Services/Details/{id}
    activate Ctrl
    Ctrl->>DB: Services.Include(Category).Include(Images).FirstOrDefault(Id==id)
    DB-->>Ctrl: Service entity with images
    alt Service Not Found or Inactive
        Ctrl-->>View: NotFound() [404]
        View-->>User: Show 404 Error
    else Service Active
        Ctrl->>Ctrl: Map to ServiceDetailPublicViewModel
        Ctrl-->>View: View(viewModel) [200 OK]
        View-->>User: Display details, gallery slider and Booking button
    end
    deactivate Ctrl`
  }
];

function download(d) {
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

async function main() {
  for (const d of missing) {
    try {
      console.log('Fetching', d.name);
      await download(d);
      await new Promise(r => setTimeout(r, 2000));
    } catch (e) {
      console.error(e.message);
    }
  }
}

main();

