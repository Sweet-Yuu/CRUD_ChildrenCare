import os
import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_ALIGN_VERTICAL
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

def set_cell_background(cell, fill_hex):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement('w:shd')
    shd.set(qn('w:val'), 'clear')
    shd.set(qn('w:color'), 'auto')
    shd.set(qn('w:fill'), fill_hex)
    tcPr.append(shd)

def set_cell_margins(cell, top=100, bottom=100, left=150, right=150):
    tcPr = cell._tc.get_or_add_tcPr()
    tcMar = OxmlElement('w:tcMar')
    for m, val in [('top', top), ('bottom', bottom), ('left', left), ('right', right)]:
        node = OxmlElement(f'w:{m}')
        node.set(qn('w:w'), str(val))
        node.set(qn('w:type'), 'dxa')
        tcMar.append(node)
    tcPr.append(tcMar)

def style_table_header(row, fill_hex='D9E2F3'):
    for cell in row.cells:
        set_cell_background(cell, fill_hex)
        set_cell_margins(cell, 120, 120, 150, 150)
        for p in cell.paragraphs:
            p.alignment = WD_ALIGN_PARAGRAPH.LEFT
            for r in p.runs:
                r.bold = True
                r.font.name = 'Arial'
                r.font.size = Pt(9.5)
                r.font.color.rgb = RGBColor(0, 32, 96)

def style_table_body(row, alternate=False):
    fill = 'F2F4F8' if alternate else 'FFFFFF'
    for cell in row.cells:
        if alternate:
            set_cell_background(cell, fill)
        set_cell_margins(cell, 90, 90, 140, 140)
        for p in cell.paragraphs:
            for r in p.runs:
                r.font.name = 'Arial'
                r.font.size = Pt(9)

def main():
    doc = docx.Document()

    # Page Margins (1 inch)
    sections = doc.sections
    for section in sections:
        section.top_margin = Inches(1)
        section.bottom_margin = Inches(1)
        section.left_margin = Inches(1)
        section.right_margin = Inches(1)

    # Base Styles
    normal_style = doc.styles['Normal']
    normal_style.font.name = 'Arial'
    normal_style.font.size = Pt(10)
    normal_style.font.color.rgb = RGBColor(51, 51, 51)

    # ==========================
    # COVER TITLE
    # ==========================
    p_title = doc.add_paragraph()
    p_title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r_main = p_title.add_run("CHILDREN CARE SYSTEM\n")
    r_main.bold = True
    r_main.font.size = Pt(22)
    r_main.font.color.rgb = RGBColor(26, 54, 93)

    r_sub = p_title.add_run("Report 4 – Software Design Document\n")
    r_sub.bold = True
    r_sub.font.size = Pt(15)
    r_sub.font.color.rgb = RGBColor(43, 108, 176)

    r_date = p_title.add_run("– Hanoi, 2026 –\n\n")
    r_date.font.size = Pt(11)
    r_date.font.italic = True
    r_date.font.color.rgb = RGBColor(100, 116, 139)

    # Table of Contents
    p_toc = doc.add_paragraph()
    r_toc = p_toc.add_run("Table of Contents")
    r_toc.bold = True
    r_toc.font.size = Pt(13)
    r_toc.font.color.rgb = RGBColor(26, 54, 93)

    toc_items = [
        "I. Record of Changes",
        "II. Software Design Document",
        "    1. System Design",
        "        1.1 System Architecture",
        "            1.1.1 Front-end Architecture",
        "            1.1.2 Back-end Architecture",
        "        1.2 Package Diagram",
        "            1.2.1 Presentation & Web Layer Package Diagram",
        "            1.2.2 Core & Data Access Package Diagram",
        "            Package Descriptions",
        "    2. Detailed Design – Part 3: Services Module (Quản lý Dịch vụ)",
        "        2.1 Services Management Overview",
        "            2.1.1 Class Diagram",
        "            2.1.2 View Public Services List (Màn hình #4)",
        "            2.1.3 View Public Service Details (Màn hình #5)",
        "            2.1.4 Manager Create New Service (Màn hình #23)",
        "            2.1.5 Manager Update Service & Toggle Status (Màn hình #22 & #23)",
        "    3. Database Design",
        "        3.1 Services Table Schema",
        "        3.2 ServiceImages Table Schema",
        "        3.3 Service Categories (Settings) Table Schema"
    ]
    for item in toc_items:
        p_item = doc.add_paragraph(item)
        p_item.paragraph_format.space_after = Pt(2)
        p_item.paragraph_format.line_spacing = 1.15

    doc.add_page_break()

    # ==========================
    # I. RECORD OF CHANGES
    # ==========================
    h1 = doc.add_heading("I. Record of Changes", level=1)
    h1.runs[0].font.color.rgb = RGBColor(26, 54, 93)

    table_changes = doc.add_table(rows=1, cols=4)
    table_changes.alignment = WD_TABLE_ALIGNMENT.CENTER
    table_changes.autofit = False

    col_widths_c = [Inches(1.1), Inches(1.0), Inches(1.3), Inches(3.1)]
    hdr_cells = table_changes.rows[0].cells
    hdr_cells[0].text = "Date"
    hdr_cells[1].text = "A*, M, D"
    hdr_cells[2].text = "In charge"
    hdr_cells[3].text = "Change Description"
    style_table_header(table_changes.rows[0])

    changes_data = [
        ("10/10/2026", "A", "nguynduong", "Khởi tạo cấu trúc tài liệu Software Design Document (Report 4) cho hệ thống Children Care"),
        ("12/10/2026", "A, M", "nguynduong", "Thiết kế System Architecture (Front-end, Back-end) & Package Diagram theo chuẩn ASP.NET Core MVC"),
        ("15/10/2026", "A", "nguynduong", "Bổ sung thiết kế Class Diagram chi tiết cho Part 3: Services Module"),
        ("18/10/2026", "A", "nguynduong", "Thêm Sequence Diagram 1 & 2 (Public Services List & Details) kèm SRS Requirements"),
        ("20/10/2026", "A", "nguynduong", "Thêm Sequence Diagram 3 & 4 (Manager Create Service & Update/Toggle Status) kèm SRS Requirements"),
        ("25/10/2026", "A, M", "nguynduong", "Hoàn thiện Database Design Schema cho các bảng Services, ServiceImages, Settings")
    ]

    for idx, row in enumerate(changes_data):
        row_cells = table_changes.add_row().cells
        for c_idx, val in enumerate(row):
            row_cells[c_idx].text = val
        style_table_body(table_changes.rows[idx + 1], alternate=(idx % 2 == 1))

    doc.add_paragraph("*A - Added, M - Modified, D - Deleted\n")

    # ==========================
    # II. SOFTWARE DESIGN DOCUMENT
    # ==========================
    h_doc = doc.add_heading("II. Software Design Document", level=1)
    h_doc.runs[0].font.color.rgb = RGBColor(26, 54, 93)

    # 1. System Design
    h_sys = doc.add_heading("1. System Design", level=2)
    h_sys.runs[0].font.color.rgb = RGBColor(37, 99, 235)

    doc.add_heading("1.1 System Architecture", level=3)
    p_arch = doc.add_paragraph(
        "Hệ thống Children Care được xây dựng dựa trên kiến trúc phân tầng chuẩn doanh nghiệp sử dụng nền tảng ASP.NET Core 8.0 MVC, kết hợp Entity Framework Core và cơ sở dữ liệu quan hệ Microsoft SQL Server."
    )

    doc.add_heading("1.1.1 Front-end Architecture", level=4)
    doc.add_paragraph(
        "Tầng giao diện sử dụng Razor View Engine (ASP.NET Core MVC) kết hợp Bootstrap 5, jQuery và Responsive CSS, tương thích hoàn toàn trên Desktop, Tablet và Mobile. Cấu trúc Layout gồm _Layout.cshtml (Khu vực Public) và _AdminLayout.cshtml (Khu vực Quản trị) với Sidebar phân quyền linh hoạt."
    )

    doc.add_heading("1.1.2 Back-end Architecture", level=4)
    doc.add_paragraph(
        "Tầng xử lý máy chủ tuân theo kiến trúc Model-View-Controller (MVC) với nguyên lý Inversion of Control & Dependency Injection tích hợp sẵn. Hệ thống bao gồm Middleware Pipeline (Authentication, Cookie Authorization, Static Files), Controller Layer điều phối logic, Tầng Service chuyên biệt (IAvatarStorage, IEmailSender), và Data Access Layer sử dụng Entity Framework Core LINQ."
    )

    img_arch = "docs/images/fig_1_1_architecture.png"
    if os.path.exists(img_arch):
        p_img = doc.add_paragraph()
        p_img.alignment = WD_ALIGN_PARAGRAPH.CENTER
        doc.add_picture(img_arch, width=Inches(6.2))
        p_cap = doc.add_paragraph("Figure 1.1. System Architecture")
        p_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_cap.runs[0].font.italic = True
        p_cap.runs[0].font.size = Pt(9)

    doc.add_heading("1.2 Package Diagram", level=3)
    doc.add_heading("1.2.1 Presentation & Web Layer Package Diagram", level=4)

    img_pkg1 = "docs/images/fig_1_2_1_package_presentation.png"
    if os.path.exists(img_pkg1):
        doc.add_picture(img_pkg1, width=Inches(6.0))
        p_cap = doc.add_paragraph("Figure 1.2.1. Presentation & Web Layer Package Diagram")
        p_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_cap.runs[0].font.italic = True
        p_cap.runs[0].font.size = Pt(9)

    doc.add_heading("1.2.2 Core & Data Access Package Diagram", level=4)
    img_pkg2 = "docs/images/fig_1_2_2_package_core.png"
    if os.path.exists(img_pkg2):
        doc.add_picture(img_pkg2, width=Inches(6.0))
        p_cap = doc.add_paragraph("Figure 1.2.2. Core & Data Access Package Diagram")
        p_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_cap.runs[0].font.italic = True
        p_cap.runs[0].font.size = Pt(9)

    doc.add_heading("Package Descriptions", level=4)
    tbl_pkg = doc.add_table(rows=1, cols=3)
    tbl_pkg.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_pkg.rows[0].cells[0].text = "No"
    tbl_pkg.rows[0].cells[1].text = "Package"
    tbl_pkg.rows[0].cells[2].text = "Description"
    style_table_header(tbl_pkg.rows[0])

    pkgs = [
        ("01", "CRUD_ChildrenCare.Controllers.Public", "Chứa các Controller phục vụ khách vãng lai (Guest) và khách hàng (Customer), bao gồm danh sách và chi tiết dịch vụ."),
        ("02", "CRUD_ChildrenCare.Controllers.Admin", "Chứa các Controller quản trị nằm trong Area Admin, xử lý phân quyền và thao tác CRUD cho Manager/Admin."),
        ("03", "CRUD_ChildrenCare.Models", "Chứa các Domain Entities của hệ thống: Service, ServiceImage, ServiceStatus, Setting, User..."),
        ("04", "CRUD_ChildrenCare.ViewModels", "Chứa các ViewModel đại diện cho dữ liệu trao đổi giữa Controller và Razor Views (Data Transfer Objects)."),
        ("05", "CRUD_ChildrenCare.Data", "Quản lý ApplicationDbContext, ánh xạ quan hệ bảng CSDL, cấu hình khóa ngoại và bộ khởi tạo dữ liệu mẫu (Seeder)."),
        ("06", "CRUD_ChildrenCare.Services.Files", "Dịch vụ xử lý tải lên, xác thực định dạng/dung lượng và lưu trữ an toàn các tệp tin hình ảnh (IAvatarStorage)."),
        ("07", "CRUD_ChildrenCare.Services.Security", "Cung cấp thuật toán mã hóa mật khẩu, sinh mã token xác thực an toàn cho tài khoản."),
        ("08", "CRUD_ChildrenCare.Services.Menus", "Dịch vụ phân giải menu điều hướng động trong trang quản trị dựa trên Role của tài khoản đang đăng nhập."),
        ("09", "Views & Areas/Admin/Views", "Giao diện Razor (.cshtml) chứa mã hiển thị, layout và form tương tác của người dùng.")
    ]
    for idx, row in enumerate(pkgs):
        row_cells = tbl_pkg.add_row().cells
        for c_idx, val in enumerate(row):
            row_cells[c_idx].text = val
        style_table_body(tbl_pkg.rows[idx + 1], alternate=(idx % 2 == 1))

    doc.add_page_break()

    # ==========================
    # 2. DETAILED DESIGN – PART 3
    # ==========================
    doc.add_heading("2. Detailed Design – Part 3: Services Module (Quản lý Dịch vụ)", level=2)
    doc.add_heading("2.1 Services Management Overview", level=3)
    doc.add_paragraph(
        "Module Dịch vụ (Part 3) chịu trách nhiệm toàn bộ vòng đời của các gói dịch vụ y tế, khám chữa bệnh và chăm sóc trẻ em trong hệ thống, bao gồm 4 màn hình chính: Màn hình #4 (Services List Public), Màn hình #5 (Service Details Public), Màn hình #22 (Services List Admin/Manager) và Màn hình #23 (Service Details / Create / Edit Manager)."
    )

    doc.add_heading("2.1.1 Class Diagram", level=4)
    img_class = "docs/images/fig_2_1_1_class_diagram.png"
    if os.path.exists(img_class):
        doc.add_picture(img_class, width=Inches(6.2))
        p_cap = doc.add_paragraph("Figure 2.1.1. Services Module Class Diagram")
        p_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_cap.runs[0].font.italic = True
        p_cap.runs[0].font.size = Pt(9)

    # 2.1.2 View Public Services List
    doc.add_heading("2.1.2 View Public Services List (Màn hình #4)", level=4)
    p_req1 = doc.add_paragraph()
    r = p_req1.add_run("Requirement Description (Yêu cầu nghiệp vụ liên quan):\n")
    r.bold = True
    p_req1.add_run(
        "• Mã màn hình / Nguồn SRS: Màn hình #4 – Services List (Public Feature) trong ChildrenCare_Requirements.md (mục 2.b) và Đặc tả CRUD (mục 3.3).\n"
        "• Tác nhân (Actor): Guest, Customer.\n"
        "• Mục tiêu: Xem danh sách các dịch vụ đang hoạt động, lọc theo Category, tìm kiếm từ khóa và phân trang.\n"
        "• Quy tắc Validate & Hiển thị:\n"
        "  - Chỉ hiển thị các dịch vụ có Status == Active. Tuyệt đối không hiển thị dịch vụ Inactive với khách công khai.\n"
        "  - Sắp xếp mặc định theo ngày cập nhật mới nhất (UpdatedAtUtc giảm dần).\n"
        "  - Phân trang từ 6-9 dịch vụ/trang; hỗ trợ tìm kiếm không dấu/có dấu trên Title và BriefInfo.\n"
        "  - Hiển thị nhãn HOT / Featured cho dịch vụ nổi bật, giá gốc (ListPrice) và giá ưu đãi (SalePrice)."
    )

    img_seq1 = "docs/images/fig_2_1_2_seq_public_list.png"
    if os.path.exists(img_seq1):
        doc.add_picture(img_seq1, width=Inches(6.2))
        p_cap = doc.add_paragraph("Figure 2.1.2. View Public Services List Sequence Diagram")
        p_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_cap.runs[0].font.italic = True
        p_cap.runs[0].font.size = Pt(9)

    # 2.1.3 View Public Service Details
    doc.add_heading("2.1.3 View Public Service Details (Màn hình #5)", level=4)
    p_req2 = doc.add_paragraph()
    r = p_req2.add_run("Requirement Description (Yêu cầu nghiệp vụ liên quan):\n")
    r.bold = True
    p_req2.add_run(
        "• Mã màn hình / Nguồn SRS: Màn hình #5 – Service Details (Public Feature) trong ChildrenCare_Requirements.md (mục 2.b) và Đặc tả CRUD (mục 3.3).\n"
        "• Tác nhân (Actor): Guest, Customer.\n"
        "• Mục tiêu: Hiển thị đầy đủ thông tin chi tiết gói dịch vụ, giá tiền, mô tả và slider bộ sưu tập ảnh.\n"
        "• Quy tắc Validate & Hiển thị:\n"
        "  - Nếu ID không tồn tại hoặc dịch vụ có Status == Inactive -> trả về lỗi 404 Not Found.\n"
        "  - Nạp kèm danh sách ảnh phụ từ bảng ServiceImages sắp xếp theo SortOrder tăng dần để dựng slider gallery.\n"
        "  - Nội dung chi tiết (Description HTML) được làm sạch để chống XSS.\n"
        "  - Nút 'Đặt dịch vụ này' kết nối trực tiếp với giỏ hàng / đặt lịch của Phần 4 (Reservations)."
    )

    img_seq2 = "docs/images/fig_2_1_3_seq_public_detail.png"
    if os.path.exists(img_seq2):
        doc.add_picture(img_seq2, width=Inches(6.2))
        p_cap = doc.add_paragraph("Figure 2.1.3. View Public Service Details Sequence Diagram")
        p_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_cap.runs[0].font.italic = True
        p_cap.runs[0].font.size = Pt(9)

    doc.add_page_break()

    # 2.1.4 Manager Create New Service
    doc.add_heading("2.1.4 Manager Create New Service (Màn hình #23)", level=4)
    p_req3 = doc.add_paragraph()
    r = p_req3.add_run("Requirement Description (Yêu cầu nghiệp vụ liên quan):\n")
    r.bold = True
    p_req3.add_run(
        "• Mã màn hình / Nguồn SRS: Màn hình #23 – Service Details (Add New) (Manager Feature) trong ChildrenCare_Requirements.md (mục 2.f) và Đặc tả CRUD (mục 3.3).\n"
        "• Tác nhân (Actor): Manager, Admin (Quyền Authorize(Roles = 'Manager,Admin')).\n"
        "• Mục tiêu: Tạo mới một gói dịch vụ y tế, tải lên ảnh đại diện và các ảnh chi tiết phụ đính kèm.\n"
        "• Quy tắc Validate:\n"
        "  - Title: Bắt buộc, từ 3-200 ký tự.\n"
        "  - CategoryId: Bắt buộc, phải thuộc danh mục dịch vụ đang có hiệu lực (IsActive == true).\n"
        "  - SalePrice <= ListPrice: Giá ưu đãi không được lớn hơn giá gốc. Cả 2 giá phải là số nguyên dương (VND).\n"
        "  - ThumbnailFile: Bắt buộc khi tạo mới. Định dạng ảnh hợp lệ (.jpg, .jpeg, .png, .webp), dung lượng <= 2 MB.\n"
        "  - Lưu ảnh vật lý qua IAvatarStorage và lưu các bản ghi liên kết ServiceImage vào CSDL trong cùng transaction."
    )

    img_seq3 = "docs/images/fig_2_1_4_seq_manager_create.png"
    if os.path.exists(img_seq3):
        doc.add_picture(img_seq3, width=Inches(6.2))
        p_cap = doc.add_paragraph("Figure 2.1.4. Manager Create New Service Sequence Diagram")
        p_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_cap.runs[0].font.italic = True
        p_cap.runs[0].font.size = Pt(9)

    # 2.1.5 Manager Update Service & Toggle Status
    doc.add_heading("2.1.5 Manager Update Service & Toggle Status (Màn hình #22 & #23)", level=4)
    p_req4 = doc.add_paragraph()
    r = p_req4.add_run("Requirement Description (Yêu cầu nghiệp vụ liên quan):\n")
    r.bold = True
    p_req4.add_run(
        "• Mã màn hình / Nguồn SRS: Màn hình #22 (Services List / Show & Hide) và #23 (Service Edit) trong ChildrenCare_Requirements.md (mục 2.f).\n"
        "• Tác nhân (Actor): Manager, Admin.\n"
        "• Mục tiêu: Cập nhật thông tin dịch vụ, quản lý bộ sưu tập ảnh và bật/tắt trạng thái ẩn/hiện dịch vụ (xóa mềm).\n"
        "• Quy tắc Validate:\n"
        "  - Nếu có ảnh đại diện mới thì cập nhật; nếu không có thì giữ nguyên ảnh cũ.\n"
        "  - Xóa các ảnh chi tiết được đánh dấu trong DeleteImageIds và thêm các ảnh mới từ NewImageFiles.\n"
        "  - Show/Hide (Xóa mềm - Soft Delete): Chuyển đổi trạng thái giữa Active (1) và Inactive (0) mà không xóa dữ liệu vật lý khỏi CSDL."
    )

    img_seq4 = "docs/images/fig_2_1_5_seq_manager_update.png"
    if os.path.exists(img_seq4):
        doc.add_picture(img_seq4, width=Inches(6.2))
        p_cap = doc.add_paragraph("Figure 2.1.5. Manager Update Service & Toggle Status Sequence Diagram")
        p_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_cap.runs[0].font.italic = True
        p_cap.runs[0].font.size = Pt(9)

    doc.add_page_break()

    # ==========================
    # 3. DATABASE DESIGN
    # ==========================
    doc.add_heading("3. Database Design", level=2)

    doc.add_heading("3.1 Services Table Schema", level=3)
    tbl_srv = doc.add_table(rows=1, cols=5)
    tbl_srv.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_srv.rows[0].cells[0].text = "Column Name"
    tbl_srv.rows[0].cells[1].text = "Data Type"
    tbl_srv.rows[0].cells[2].text = "Nullable"
    tbl_srv.rows[0].cells[3].text = "Constraint"
    tbl_srv.rows[0].cells[4].text = "Description"
    style_table_header(tbl_srv.rows[0])

    srv_cols = [
        ("Id", "INT", "NO", "PK, IDENTITY(1,1)", "Khóa chính tự tăng định danh gói dịch vụ."),
        ("Title", "NVARCHAR(200)", "NO", "INDEX", "Tiêu đề tên dịch vụ (3-200 ký tự)."),
        ("CategoryId", "INT", "NO", "FK (Settings.Id)", "Khóa ngoại tham chiếu danh mục dịch vụ."),
        ("BriefInfo", "NVARCHAR(500)", "NO", "-", "Mô tả ngắn gọn dịch vụ hiển thị trên thẻ card."),
        ("Description", "NVARCHAR(MAX)", "NO", "-", "Nội dung mô tả chi tiết gói khám (HTML)."),
        ("NumberOfPerson", "INT", "NO", "DEFAULT 1", "Số lượng người/trẻ em được phục vụ."),
        ("ListPrice", "DECIMAL(18,2)", "NO", ">= 0", "Giá niêm yết gốc (VND)."),
        ("SalePrice", "DECIMAL(18,2)", "NO", "<= ListPrice", "Giá bán ưu đãi khuyến mãi (VND)."),
        ("AvailableQuantity", "INT", "NO", "DEFAULT 0", "Số lượng khả dụng của dịch vụ."),
        ("IsFeatured", "BIT", "NO", "DEFAULT 0", "Đánh dấu dịch vụ tiêu biểu nổi bật (HOT)."),
        ("Status", "INT", "NO", "DEFAULT 1", "Trạng thái: 0 (Inactive/Ẩn), 1 (Active/Hiện)."),
        ("ThumbnailUrl", "NVARCHAR(500)", "NO", "-", "Đường dẫn ảnh đại diện trên máy chủ."),
        ("CreatedAtUtc", "DATETIME2", "NO", "GETUTCDATE()", "Thời điểm tạo bản ghi (UTC)."),
        ("UpdatedAtUtc", "DATETIME2", "YES", "-", "Thời điểm cập nhật gần nhất (UTC).")
    ]
    for idx, row in enumerate(srv_cols):
        row_cells = tbl_srv.add_row().cells
        for c_idx, val in enumerate(row):
            row_cells[c_idx].text = val
        style_table_body(tbl_srv.rows[idx + 1], alternate=(idx % 2 == 1))

    doc.add_heading("3.2 ServiceImages Table Schema", level=3)
    tbl_img = doc.add_table(rows=1, cols=5)
    tbl_img.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_img.rows[0].cells[0].text = "Column Name"
    tbl_img.rows[0].cells[1].text = "Data Type"
    tbl_img.rows[0].cells[2].text = "Nullable"
    tbl_img.rows[0].cells[3].text = "Constraint"
    tbl_img.rows[0].cells[4].text = "Description"
    style_table_header(tbl_img.rows[0])

    img_cols = [
        ("Id", "INT", "NO", "PK, IDENTITY(1,1)", "Khóa chính tự tăng của ảnh chi tiết."),
        ("ServiceId", "INT", "NO", "FK (Services.Id, CASCADE)", "Khóa ngoại liên kết bảng Services."),
        ("ImageUrl", "NVARCHAR(500)", "NO", "-", "Đường dẫn URL ảnh chi tiết trên máy chủ."),
        ("SortOrder", "INT", "NO", "DEFAULT 0", "Thứ tự hiển thị trong slider/gallery."),
        ("CreatedAtUtc", "DATETIME2", "NO", "GETUTCDATE()", "Thời điểm tải ảnh lên hệ thống (UTC).")
    ]
    for idx, row in enumerate(img_cols):
        row_cells = tbl_img.add_row().cells
        for c_idx, val in enumerate(row):
            row_cells[c_idx].text = val
        style_table_body(tbl_img.rows[idx + 1], alternate=(idx % 2 == 1))

    doc.add_heading("3.3 Service Categories (Settings) Table Schema", level=3)
    tbl_cat = doc.add_table(rows=1, cols=5)
    tbl_cat.alignment = WD_TABLE_ALIGNMENT.CENTER
    tbl_cat.rows[0].cells[0].text = "Column Name"
    tbl_cat.rows[0].cells[1].text = "Data Type"
    tbl_cat.rows[0].cells[2].text = "Nullable"
    tbl_cat.rows[0].cells[3].text = "Constraint"
    tbl_cat.rows[0].cells[4].text = "Description"
    style_table_header(tbl_cat.rows[0])

    cat_cols = [
        ("Id", "INT", "NO", "PK, IDENTITY(1,1)", "Khóa chính định danh thiết lập."),
        ("Name", "NVARCHAR(100)", "NO", "-", "Tên hiển thị danh mục (VD: Khám Nhi, Tiêm Chủng)."),
        ("Value", "NVARCHAR(100)", "NO", "-", "Mã định danh slug/value."),
        ("Type", "NVARCHAR(50)", "NO", "INDEX", "Loại cấu hình (Lọc Type = 'ServiceCategory')."),
        ("Order", "INT", "NO", "DEFAULT 0", "Thứ tự sắp xếp trong dropdown."),
        ("IsActive", "BIT", "NO", "DEFAULT 1", "Trạng thái hoạt động (1: Active, 0: Inactive).")
    ]
    for idx, row in enumerate(cat_cols):
        row_cells = tbl_cat.add_row().cells
        for c_idx, val in enumerate(row):
            row_cells[c_idx].text = val
        style_table_body(tbl_cat.rows[idx + 1], alternate=(idx % 2 == 1))

    # Save output
    output_path = "docs/ChildrenCare_Report4_Software_Design_Document.docx"
    doc.save(output_path)
    print(f"Successfully generated: {output_path}")

if __name__ == '__main__':
    main()

