# Thuyết Minh Sơ Đồ Lớp (Class Diagram) & Sơ Đồ Gói (Package Diagram) – Phần 3: Dịch Vụ (Services)

Tài liệu này thuyết minh kiến trúc phần mềm chuẩn UML, cấu trúc lớp và phân chia gói (package structure) dành riêng cho **Phần 3: Dịch Vụ (Service Module)** của hệ thống **Children Care**, chuẩn hóa theo phong cách mẫu Class Diagram quy chuẩn đồ án.

---

## 1. Sơ Đồ Lớp (Class Diagram)

### 1.1. Sơ đồ bằng Mermaid

```mermaid
classDiagram
    namespace Auth_Identity {
        class User {
            <<entity>>
            +id: Integer [PK]
            +email: String [unique]
            +fullName: String
            +mobile: String [0..1]
            +avatarUrl: String [0..1]
        }
        class Manager {
            <<entity>>
            +roleName: String = "Manager"
        }
        class Doctor {
            <<entity>>
            +roleName: String = "Doctor"
        }
        class Nurse {
            <<entity>>
            +roleName: String = "Nurse"
        }
    }

    namespace System_Settings {
        class ServiceCategory {
            <<entity>>
            +id: Integer [PK]
            +name: String
            +value: String [unique]
            +status: SettingStatus
        }
        class SettingStatus {
            <<enumeration>>
            INACTIVE
            ACTIVE
        }
    }

    namespace Service_Management {
        class Service {
            <<entity>>
            +id: Integer [PK]
            +title: String
            +thumbnail: String
            +categoryId: Integer [FK]
            +briefInfo: String
            +description: Text
            +numberOfPerson: Integer
            +listPrice: Decimal
            +salePrice: Decimal
            +availableQuantity: Integer
            +isFeatured: Boolean
            +status: ServiceStatus
            +createdDate: DateTime
            +updatedDate: DateTime
        }
        class ServiceImage {
            <<entity>>
            +id: Integer [PK]
            +serviceId: Integer [FK]
            +imageUrl: String
            +sortOrder: Integer [0..1]
        }
        class ServiceStatus {
            <<enumeration>>
            INACTIVE
            ACTIVE
        }
    }

    User <|-- Manager
    User <|-- Doctor
    User <|-- Nurse

    Manager "1" -- "0..*" Service : manages >
    Doctor "1" -- "0..*" Service : views >
    Nurse "1" -- "0..*" Service : views >

    Service "1" -- "1" ServiceCategory : belongs to >
    Service "1" *-- "0..*" ServiceImage : serviceImages >
    Service ..> ServiceStatus : status
    ServiceCategory ..> SettingStatus : status
```

### 1.2. Mã PlantUML Chuẩn Hóa Theo Mẫu Đồ Án

```plantuml
@startuml ClassDiagram_Part3_Services
left to right direction
skinparam classAttributeIconSize 0
skinparam style strictuml

package "Auth & Identity" {
    class "User" as User <<entity>> {
        + id: Integer [PK]
        + email: String [unique]
        + fullName: String
        + mobile: String [0..1]
        + avatarUrl: String [0..1]
    }

    class "Manager" as Manager <<entity>> {
        + roleName: String = "Manager"
    }

    class "Doctor" as Doctor <<entity>> {
        + roleName: String = "Doctor"
    }

    class "Nurse" as Nurse <<entity>> {
        + roleName: String = "Nurse"
    }

    User <|-- Manager
    User <|-- Doctor
    User <|-- Nurse
}

package "System Settings" {
    enum "SettingStatus" as SettingStatus <<enumeration>> {
        INACTIVE
        ACTIVE
    }

    class "ServiceCategory" as ServiceCategory <<entity>> {
        + id: Integer [PK]
        + name: String
        + value: String [unique]
        + status: SettingStatus
    }

    ServiceCategory ..> SettingStatus
}

package "Service Management" {
    enum "ServiceStatus" as ServiceStatus <<enumeration>> {
        INACTIVE
        ACTIVE
    }

    class "Service" as Service <<entity>> {
        + id: Integer [PK]
        + title: String
        + thumbnail: String
        + categoryId: Integer [FK]
        + briefInfo: String
        + description: Text
        + numberOfPerson: Integer
        + listPrice: Decimal
        + salePrice: Decimal
        + availableQuantity: Integer
        + isFeatured: Boolean
        + status: ServiceStatus
        + createdDate: DateTime
        + updatedDate: DateTime
    }

    class "ServiceImage" as ServiceImage <<entity>> {
        + id: Integer [PK]
        + serviceId: Integer [FK]
        + imageUrl: String
        + sortOrder: Integer [0..1]
    }

    Service "1" *-- "0..*" ServiceImage : serviceImages >
    Service ..> ServiceStatus
}

Manager "1" -- "0..*" Service : manages >
Doctor "1" -- "0..*" Service : views >
Nurse "1" -- "0..*" Service : views >
Service "*" -- "1" ServiceCategory : belongs to >

@endluml
```

---

## 2. Sơ Đồ Gói (Package Diagram)

### 2.1. Sơ đồ bằng Mermaid

```mermaid
graph TB
    subgraph PresentationLayer ["Tầng Giao Diện & Điều Khiển (Presentation Layer)"]
        PublicCtrl["CRUD_ChildrenCare.Controllers"]
        AdminCtrl["CRUD_ChildrenCare.Areas.Admin.Controllers"]
        Views["Views & Razor Templates"]
    end

    subgraph ViewModelsLayer ["Tầng Mẫu Hiển Thị (ViewModels Layer)"]
        PublicVM["CRUD_ChildrenCare.ViewModels.Services"]
        AdminVM["CRUD_ChildrenCare.Areas.Admin.ViewModels.Services"]
    end

    subgraph DomainDataLayer ["Tầng Dữ Liệu & Thực Thể (Data & Domain Layer)"]
        ModelsPkg["CRUD_ChildrenCare.Models"]
        DataPkg["CRUD_ChildrenCare.Data"]
        FilesPkg["CRUD_ChildrenCare.Services.Files"]
    end

    PublicCtrl --> PublicVM
    AdminCtrl --> AdminVM
    Views --> PublicVM
    Views --> AdminVM

    PublicVM --> ModelsPkg
    AdminVM --> ModelsPkg

    PublicCtrl --> DataPkg
    AdminCtrl --> DataPkg
    AdminCtrl --> FilesPkg

    DataPkg --> ModelsPkg
```

### 2.2. Mã PlantUML cho Package Diagram

```plantuml
@startuml PackageDiagram_Part3_Services
skinparam packageStyle rect

package "Presentation Layer" {
    package "CRUD_ChildrenCare.Controllers" as PublicCtrl
    package "CRUD_ChildrenCare.Areas.Admin.Controllers" as AdminCtrl
    package "Views & Razor Templates" as Views
}

package "ViewModels Layer" {
    package "CRUD_ChildrenCare.ViewModels.Services" as PublicVM
    package "CRUD_ChildrenCare.Areas.Admin.ViewModels.Services" as AdminVM
}

package "Data & Core Domain Layer" {
    package "CRUD_ChildrenCare.Models" as ModelsPkg
    package "CRUD_ChildrenCare.Data" as DataPkg
    package "CRUD_ChildrenCare.Services.Files" as FilesPkg
}

PublicCtrl ..> PublicVM : <<use>>
AdminCtrl ..> AdminVM : <<use>>
Views ..> PublicVM : <<bind>>
Views ..> AdminVM : <<bind>>

PublicVM ..> ModelsPkg : <<import>>
AdminVM ..> ModelsPkg : <<import>>

PublicCtrl ..> DataPkg : <<use>>
AdminCtrl ..> DataPkg : <<use>>
AdminCtrl ..> FilesPkg : <<use>>

DataPkg ..> ModelsPkg : <<persist>>

@endluml
```
