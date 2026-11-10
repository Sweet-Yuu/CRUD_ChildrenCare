# -*- coding: utf-8 -*-
"""
Full Generator for Children Care Draw.io files:
1. ContextDiagram.drawio
2. UseCase_Guest.drawio
3. UseCase_Customer.drawio
4. UseCase_Staff.drawio
5. UseCase_Manager.drawio
6. UseCase_Admin.drawio
7. UseCaseDiagrams.drawio (Multi-page)
8. ClassDiagram_Part4_Reservations.drawio
9. ClassDiagram_Part5_Medical_Feedback_Dashboard.drawio
10. Database_ERD_ChildrenCare.drawio
"""

import os
import xml.etree.ElementTree as ET

def create_cell(id_val, value, style, vertex=True, edge=False, parent="1", source=None, target=None, x=0, y=0, width=100, height=50):
    attribs = {"id": str(id_val), "parent": str(parent)}
    if value is not None:
        attribs["value"] = value
    if style:
        attribs["style"] = style
    if vertex:
        attribs["vertex"] = "1"
    if edge:
        attribs["edge"] = "1"
    if source:
        attribs["source"] = str(source)
    if target:
        attribs["target"] = str(target)
    
    cell = ET.Element("mxCell", attribs)
    geo = ET.SubElement(cell, "mxGeometry")
    if edge:
        geo.set("relative", "1")
        geo.set("as", "geometry")
    else:
        geo.set("x", str(x))
        geo.set("y", str(y))
        geo.set("width", str(width))
        geo.set("height", str(height))
        geo.set("as", "geometry")
    return cell

def build_drawio_xml(diagrams_info):
    """
    diagrams_info: list of (diagram_id, diagram_name, list_of_cells)
    """
    mxfile = ET.Element("mxfile", host="app.diagrams.net", type="device")
    for d_id, d_name, cells in diagrams_info:
        diagram = ET.SubElement(mxfile, "diagram", id=d_id, name=d_name)
        model = ET.SubElement(diagram, "mxGraphModel", dx="1400", dy="900", grid="1", gridSize="10",
                             guides="1", tooltips="1", connect="1", arrows="1", fold="1", page="1",
                             pageScale="1", pageWidth="1600", pageHeight="1100", math="0", shadow="0")
        root = ET.SubElement(model, "root")
        ET.SubElement(root, "mxCell", id="0")
        ET.SubElement(root, "mxCell", id="1", parent="0")
        for c in cells:
            root.append(c)
    return ET.tostring(mxfile, encoding="utf-8", xml_declaration=True).decode("utf-8")

def save_drawio(file_path, diagrams_info):
    xml_content = build_drawio_xml(diagrams_info)
    with open(file_path, "w", encoding="utf-8") as f:
        f.write(xml_content)
    print(f"Generated: {file_path}")

# ==========================================
# 1. CONTEXT DIAGRAM
# ==========================================
def generate_context_diagram():
    cells = []
    # Styles
    s_proc = "ellipse;whiteSpace=wrap;html=1;aspect=fixed;fillColor=#dae8fc;strokeColor=#6c8ebf;fontStyle=1;fontSize=14;fontColor=#003366;shadow=1;"
    s_actor = "shape=umlActor;verticalLabelPosition=bottom;verticalAlign=top;html=1;outlineConnect=0;fillColor=#d5e8d4;strokeColor=#82b366;fontStyle=1;fontSize=12;"
    s_ext = "rounded=1;whiteSpace=wrap;html=1;fillColor=#ffe6cc;strokeColor=#d79b00;fontStyle=1;fontSize=12;fontColor=#663300;shadow=1;"
    s_edge = "edgeStyle=orthogonalEdgeStyle;rounded=1;orthogonalLoop=1;jettySize=auto;html=1;fontSize=10;fontColor=#333333;strokeColor=#4d4d4d;strokeWidth=1.5;"

    # Center Process
    cells.append(create_cell("ccs_proc", "<b>CHILDREN CARE SYSTEM (CCS)</b><br/>(Hệ Thống Trung Tâm Y Tế Nhi Khoa)<br/><i>ASP.NET Core 8.0 MVC</i>", s_proc, x=580, y=360, width=240, height=240))

    # Actors
    cells.append(create_cell("act_guest", "Khách vãng lai<br/>(Guest)", s_actor, x=140, y=140, width=60, height=100))
    cells.append(create_cell("act_cust", "Khách hàng / Phụ huynh<br/>(Customer)", s_actor, x=140, y=420, width=60, height=100))
    cells.append(create_cell("act_manager", "Quản lý trung tâm<br/>(Manager)", s_actor, x=140, y=700, width=60, height=100))
    
    cells.append(create_cell("act_staff", "Nhân sự y tế<br/>(Staff: Bác sĩ & Y tá)", s_actor, x=1200, y=260, width=60, height=100))
    cells.append(create_cell("act_admin", "Quản trị viên<br/>(Admin)", s_actor, x=1200, y=620, width=60, height=100))

    # External systems
    cells.append(create_cell("ext_smtp", "Hệ Thống Email Ngoại Vi<br/><b>Gmail SMTP Server</b>", s_ext, x=610, y=80, width=180, height=70))
    cells.append(create_cell("ext_vnpay", "Cổng Thanh Toán Trực Tuyến<br/><b>VNPay Gateway</b>", s_ext, x=610, y=780, width=180, height=70))

    # Flows Guest
    cells.append(create_cell("e_g_in", "1. Tìm dịch vụ/bài viết, gửi liên hệ<br/>2. Đăng ký, đặt hẹn tạm thời", s_edge, vertex=False, edge=True, source="act_guest", target="ccs_proc"))
    cells.append(create_cell("e_g_out", "Trang chủ, chi tiết gói khám, cẩm nang y tế", s_edge, vertex=False, edge=True, source="ccs_proc", target="act_guest"))

    # Flows Customer
    cells.append(create_cell("e_c_in", "Đăng nhập, đặt hẹn khám, hủy lịch (Submitted),<br/>gửi phản hồi/feedback, thanh toán cọc", s_edge, vertex=False, edge=True, source="act_cust", target="ccs_proc"))
    cells.append(create_cell("e_c_out", "Danh sách lịch hẹn của tôi, lịch sử khám,<br/>kết luận chẩn đoán & đơn thuốc của trẻ", s_edge, vertex=False, edge=True, source="ccs_proc", target="act_cust"))

    # Flows Staff
    cells.append(create_cell("e_s_in", "Cập nhật trạng thái khám (Check-in/Khám xong),<br/>ghi chẩn đoán, kê đơn thuốc (Doctor)", s_edge, vertex=False, edge=True, source="act_staff", target="ccs_proc"))
    cells.append(create_cell("e_s_out", "Lịch hẹn phân công, thông tin bệnh nhi, bệnh sử", s_edge, vertex=False, edge=True, source="ccs_proc", target="act_staff"))

    # Flows Manager
    cells.append(create_cell("e_m_in", "Quản lý dịch vụ, bài viết, banner, phân công ca khám", s_edge, vertex=False, edge=True, source="act_manager", target="ccs_proc"))
    cells.append(create_cell("e_m_out", "Reservation Dashboard (doanh thu, lượt khám), phản hồi", s_edge, vertex=False, edge=True, source="ccs_proc", target="act_manager"))

    # Flows Admin
    cells.append(create_cell("e_a_in", "Quản lý người dùng, phân vai trò, cấu hình Settings", s_edge, vertex=False, edge=True, source="act_admin", target="ccs_proc"))
    cells.append(create_cell("e_a_out", "Admin Dashboard, danh sách tài khoản & cấu hình", s_edge, vertex=False, edge=True, source="ccs_proc", target="act_admin"))

    # Flows SMTP
    cells.append(create_cell("e_smtp_send", "Lệnh gửi mail kích hoạt, reset MK, nhắc lịch khám", s_edge, vertex=False, edge=True, source="ccs_proc", target="ext_smtp"))
    cells.append(create_cell("e_smtp_res", "Trạng thái gửi mail (Success/Fail)", s_edge, vertex=False, edge=True, source="ext_smtp", target="ccs_proc"))

    # Flows VNPay
    cells.append(create_cell("e_pay_req", "Yêu cầu tạo phiên thanh toán (OrderCode, Amount)", s_edge, vertex=False, edge=True, source="ccs_proc", target="ext_vnpay"))
    cells.append(create_cell("e_pay_res", "Kết quả giao dịch (IPN Callback: Paid/Failed)", s_edge, vertex=False, edge=True, source="ext_vnpay", target="ccs_proc"))

    return [("context-diagram", "System Context Diagram", cells)]

# ==========================================
# 2. USE CASE DIAGRAMS
# ==========================================
def generate_usecase_diagram(actor_name, actor_title, use_cases, includes=None, extends=None, inherits=None):
    cells = []
    s_boundary = "swimlane;whiteSpace=wrap;html=1;fillColor=#f8f9fa;strokeColor=#495057;fontStyle=1;fontSize=14;fontColor=#212529;startSize=40;"
    s_actor = "shape=umlActor;verticalLabelPosition=bottom;verticalAlign=top;html=1;outlineConnect=0;fillColor=#d5e8d4;strokeColor=#82b366;fontStyle=1;fontSize=12;"
    s_subactor = "shape=umlActor;verticalLabelPosition=bottom;verticalAlign=top;html=1;outlineConnect=0;fillColor=#e1d5e7;strokeColor=#9673a6;fontStyle=1;fontSize=11;"
    s_uc = "ellipse;whiteSpace=wrap;html=1;fillColor=#dae8fc;strokeColor=#6c8ebf;fontStyle=0;fontSize=11;fontColor=#000000;"
    s_edge = "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;endArrow=none;strokeColor=#4d4d4d;"
    s_inc = "edgeStyle=orthogonalEdgeStyle;rounded=0;html=1;dashed=1;endArrow=open;endSize=8;fontSize=10;fontColor=#003366;"
    s_inherit = "edgeStyle=orthogonalEdgeStyle;rounded=0;html=1;endArrow=block;endFill=0;endSize=10;strokeColor=#82b366;"

    # Boundary
    cells.append(create_cell("sys_bound", f"Children Care System - {actor_title} Use Cases", s_boundary, x=340, y=40, width=640, height=max(700, len(use_cases) * 58 + 80)))

    # Actor
    cells.append(create_cell("main_act", f"{actor_name}<br/>({actor_title})", s_actor, x=80, y=280, width=60, height=100))

    # Sub-actors if any
    if inherits:
        for sub_id, sub_name, sy in inherits:
            cells.append(create_cell(sub_id, sub_name, s_subactor, x=180, y=sy, width=50, height=80))
            cells.append(create_cell(f"inh_{sub_id}", "", s_inherit, vertex=False, edge=True, source=sub_id, target="main_act"))

    # Use Cases
    col_x = 420
    start_y = 90
    spacing = 52
    for i, (uc_id, uc_text, act_src) in enumerate(use_cases):
        y_pos = start_y + i * spacing
        cells.append(create_cell(uc_id, uc_text, s_uc, x=col_x, y=y_pos, width=280, height=44))
        # Edge from actor to use case
        src = act_src if act_src else "main_act"
        cells.append(create_cell(f"e_{uc_id}", "", s_edge, vertex=False, edge=True, source=src, target=uc_id))

    # Includes
    if includes:
        for inc_id, src_uc, tgt_uc in includes:
            cells.append(create_cell(inc_id, "&lt;&lt;include&gt;&gt;", s_inc, vertex=False, edge=True, source=src_uc, target=tgt_uc))

    # Extends
    if extends:
        for ext_id, src_uc, tgt_uc in extends:
            cells.append(create_cell(ext_id, "&lt;&lt;extend&gt;&gt;", s_inc, vertex=False, edge=True, source=src_uc, target=tgt_uc))

    return cells

# ==========================================
# 3. CLASS DIAGRAM PART 4 (RESERVATIONS)
# ==========================================
def generate_class_diagram_part4():
    cells = []
    s_entity = "swimlane;fontStyle=1;align=center;verticalAlign=top;childLayout=stackLayout;horizontal=1;startSize=26;horizontalStack=0;resizeParent=1;resizeParentMax=0;resizeLast=0;collapsible=1;marginBottom=0;fillColor=#d5e8d4;strokeColor=#82b366;fontSize=12;"
    s_ctrl = "swimlane;fontStyle=1;align=center;verticalAlign=top;childLayout=stackLayout;horizontal=1;startSize=26;horizontalStack=0;resizeParent=1;resizeParentMax=0;resizeLast=0;collapsible=1;marginBottom=0;fillColor=#dae8fc;strokeColor=#6c8ebf;fontSize=12;"
    s_vm = "swimlane;fontStyle=1;align=center;verticalAlign=top;childLayout=stackLayout;horizontal=1;startSize=26;horizontalStack=0;resizeParent=1;resizeParentMax=0;resizeLast=0;collapsible=1;marginBottom=0;fillColor=#fff2cc;strokeColor=#d6b656;fontSize=12;"
    s_enum = "swimlane;fontStyle=1;align=center;verticalAlign=top;childLayout=stackLayout;horizontal=1;startSize=26;horizontalStack=0;resizeParent=1;resizeParentMax=0;resizeLast=0;collapsible=1;marginBottom=0;fillColor=#e1d5e7;strokeColor=#9673a6;fontSize=12;"
    s_text = "text;strokeColor=none;fillColor=none;align=left;verticalAlign=top;spacingLeft=6;spacingRight=6;overflow=hidden;rotatable=0;points=[[0,0.5],[1,0.5]];portConstraint=eastwest;fontSize=11;"
    s_edge = "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;strokeColor=#4d4d4d;"
    s_assoc = "edgeStyle=orthogonalEdgeStyle;rounded=0;html=1;endArrow=open;endSize=8;strokeColor=#4d4d4d;"

    # 1. Entity: Reservation
    c_res = create_cell("c_res", "&lt;&lt;entity&gt;&gt;<br/><b>Reservation</b>", s_entity, x=420, y=40, width=240, height=260)
    cells.append(c_res)
    t_res = (
        "+ id: Integer [PK]<br/>"
        "+ customerId: Integer [FK]<br/>"
        "+ receiverName: String<br/>"
        "+ receiverGender: Gender<br/>"
        "+ receiverEmail: String<br/>"
        "+ receiverMobile: String<br/>"
        "+ receiverAddress: String<br/>"
        "+ status: ReservationStatus<br/>"
        "+ totalCost: Decimal<br/>"
        "+ notes: String<br/>"
        "+ assignedStaffId: Integer [FK]<br/>"
        "+ createdAt: DateTime<br/>"
        "+ updatedAt: DateTime"
    )
    cells.append(create_cell("t_res", t_res, s_text, parent="c_res", x=0, y=26, width=240, height=230))

    # 2. Entity: ReservationItem
    c_item = create_cell("c_item", "&lt;&lt;entity&gt;&gt;<br/><b>ReservationItem</b>", s_entity, x=780, y=40, width=220, height=180)
    cells.append(c_item)
    t_item = (
        "+ id: Integer [PK]<br/>"
        "+ reservationId: Integer [FK]<br/>"
        "+ serviceId: Integer [FK]<br/>"
        "+ quantity: Integer<br/>"
        "+ unitPrice: Decimal<br/>"
        "+ subTotal: Decimal<br/>"
        "+ examinationDate: DateTime"
    )
    cells.append(create_cell("t_item", t_item, s_text, parent="c_item", x=0, y=26, width=220, height=150))

    # 3. Entity: PaymentTransaction
    c_pay = create_cell("c_pay", "&lt;&lt;entity&gt;&gt;<br/><b>PaymentTransaction</b>", s_entity, x=780, y=260, width=220, height=180)
    cells.append(c_pay)
    t_pay = (
        "+ id: Integer [PK]<br/>"
        "+ reservationId: Integer [FK]<br/>"
        "+ orderCode: String [unique]<br/>"
        "+ providerTxnRef: String<br/>"
        "+ amount: Decimal<br/>"
        "+ paymentMethod: String<br/>"
        "+ status: String<br/>"
        "+ createdAt: DateTime"
    )
    cells.append(create_cell("t_pay", t_pay, s_text, parent="c_pay", x=0, y=26, width=220, height=150))

    # 4. Enum: ReservationStatus
    c_status = create_cell("c_status", "&lt;&lt;enumeration&gt;&gt;<br/><b>ReservationStatus</b>", s_enum, x=420, y=340, width=200, height=180)
    cells.append(c_status)
    t_status = (
        "SUBMITTED<br/>"
        "CONFIRMED<br/>"
        "ASSIGNED<br/>"
        "CHECKIN<br/>"
        "IN_PROGRESS<br/>"
        "COMPLETED<br/>"
        "CANCELLED"
    )
    cells.append(create_cell("t_status", t_status, s_text, parent="c_status", x=0, y=26, width=200, height=150))

    # 5. Controller: PublicReservationsController
    c_pub_ctrl = create_cell("c_pub_ctrl", "&lt;&lt;controller&gt;&gt;<br/><b>PublicReservationsController</b>", s_ctrl, x=60, y=40, width=280, height=220)
    cells.append(c_pub_ctrl)
    t_pub_ctrl = (
        "+ Cart(): IActionResult<br/>"
        "+ AddToCart(serviceId: int): IActionResult<br/>"
        "+ UpdateCart(serviceId, qty): IActionResult<br/>"
        "+ RemoveFromCart(serviceId): IActionResult<br/>"
        "+ Checkout(): IActionResult<br/>"
        "+ Submit(model): Task&lt;IActionResult&gt;<br/>"
        "+ MyReservations(page, status): IActionResult<br/>"
        "+ Details(id: int): IActionResult<br/>"
        "+ Cancel(id: int): Task&lt;IActionResult&gt;"
    )
    cells.append(create_cell("t_pub_ctrl", t_pub_ctrl, s_text, parent="c_pub_ctrl", x=0, y=26, width=280, height=190))

    # 6. Controller: AdminReservationsController
    c_adm_ctrl = create_cell("c_adm_ctrl", "&lt;&lt;controller&gt;&gt;<br/><b>AdminReservationsController</b>", s_ctrl, x=60, y=300, width=280, height=160)
    cells.append(c_adm_ctrl)
    t_adm_ctrl = (
        "+ Index(search, status, page): IActionResult<br/>"
        "+ Details(id: int): IActionResult<br/>"
        "+ AssignStaff(id, staffId): Task&lt;IActionResult&gt;<br/>"
        "+ UpdateStatus(id, newStatus): Task&lt;IActionResult&gt;<br/>"
        "+ CreateReservation(model): Task&lt;IActionResult&gt;"
    )
    cells.append(create_cell("t_adm_ctrl", t_adm_ctrl, s_text, parent="c_adm_ctrl", x=0, y=26, width=280, height=130))

    # 7. ViewModel: ReservationContactViewModel
    c_vm_contact = create_cell("c_vm_contact", "&lt;&lt;viewModel&gt;&gt;<br/><b>ReservationContactViewModel</b>", s_vm, x=60, y=500, width=280, height=180)
    cells.append(c_vm_contact)
    t_vm_contact = (
        "+ receiverName: String<br/>"
        "+ receiverGender: Integer<br/>"
        "+ receiverEmail: String<br/>"
        "+ receiverMobile: String<br/>"
        "+ receiverAddress: String<br/>"
        "+ notes: String<br/>"
        "+ cartItems: List&lt;CartItemViewModel&gt;<br/>"
        "+ totalAmount: Decimal"
    )
    cells.append(create_cell("t_vm_contact", t_vm_contact, s_text, parent="c_vm_contact", x=0, y=26, width=280, height=150))

    # Edges
    cells.append(create_cell("e_res_item", "1 .. *", s_assoc, vertex=False, edge=True, source="c_res", target="c_item"))
    cells.append(create_cell("e_res_pay", "0 .. 1", s_assoc, vertex=False, edge=True, source="c_res", target="c_pay"))
    cells.append(create_cell("e_res_status", "", s_edge, vertex=False, edge=True, source="c_res", target="c_status"))
    cells.append(create_cell("e_pub_res", "manages", s_assoc, vertex=False, edge=True, source="c_pub_ctrl", target="c_res"))
    cells.append(create_cell("e_adm_res", "manages", s_assoc, vertex=False, edge=True, source="c_adm_ctrl", target="c_res"))

    return [("class-diagram-part4", "Class Diagram Part 4 - Reservations & Payment", cells)]

# ==========================================
# 4. CLASS DIAGRAM PART 5 (MEDICAL & FEEDBACK)
# ==========================================
def generate_class_diagram_part5():
    cells = []
    s_entity = "swimlane;fontStyle=1;align=center;verticalAlign=top;childLayout=stackLayout;horizontal=1;startSize=26;horizontalStack=0;resizeParent=1;resizeParentMax=0;resizeLast=0;collapsible=1;marginBottom=0;fillColor=#d5e8d4;strokeColor=#82b366;fontSize=12;"
    s_ctrl = "swimlane;fontStyle=1;align=center;verticalAlign=top;childLayout=stackLayout;horizontal=1;startSize=26;horizontalStack=0;resizeParent=1;resizeParentMax=0;resizeLast=0;collapsible=1;marginBottom=0;fillColor=#dae8fc;strokeColor=#6c8ebf;fontSize=12;"
    s_vm = "swimlane;fontStyle=1;align=center;verticalAlign=top;childLayout=stackLayout;horizontal=1;startSize=26;horizontalStack=0;resizeParent=1;resizeParentMax=0;resizeLast=0;collapsible=1;marginBottom=0;fillColor=#fff2cc;strokeColor=#d6b656;fontSize=12;"
    s_text = "text;strokeColor=none;fillColor=none;align=left;verticalAlign=top;spacingLeft=6;spacingRight=6;overflow=hidden;rotatable=0;points=[[0,0.5],[1,0.5]];portConstraint=eastwest;fontSize=11;"
    s_assoc = "edgeStyle=orthogonalEdgeStyle;rounded=0;html=1;endArrow=open;endSize=8;strokeColor=#4d4d4d;"

    # 1. Entity: MedicalExamination
    c_exam = create_cell("c_exam", "&lt;&lt;entity&gt;&gt;<br/><b>MedicalExamination</b>", s_entity, x=440, y=40, width=240, height=200)
    cells.append(c_exam)
    t_exam = (
        "+ id: Integer [PK]<br/>"
        "+ reservationItemId: Integer [FK]<br/>"
        "+ doctorId: Integer [FK]<br/>"
        "+ symptoms: String<br/>"
        "+ diagnosis: String<br/>"
        "+ conclusion: String<br/>"
        "+ examinedAt: DateTime<br/>"
        "+ reExaminationDate: DateTime [0..1]"
    )
    cells.append(create_cell("t_exam", t_exam, s_text, parent="c_exam", x=0, y=26, width=240, height=170))

    # 2. Entity: Prescription
    c_pres = create_cell("c_pres", "&lt;&lt;entity&gt;&gt;<br/><b>Prescription</b>", s_entity, x=780, y=40, width=220, height=180)
    cells.append(c_pres)
    t_pres = (
        "+ id: Integer [PK]<br/>"
        "+ medicalExaminationId: Integer [FK]<br/>"
        "+ medicineName: String<br/>"
        "+ dosage: String<br/>"
        "+ usageInstructions: String<br/>"
        "+ quantity: Integer<br/>"
        "+ unit: String"
    )
    cells.append(create_cell("t_pres", t_pres, s_text, parent="c_pres", x=0, y=26, width=220, height=150))

    # 3. Entity: Feedback
    c_fb = create_cell("c_fb", "&lt;&lt;entity&gt;&gt;<br/><b>Feedback</b>", s_entity, x=440, y=280, width=240, height=180)
    cells.append(c_fb)
    t_fb = (
        "+ id: Integer [PK]<br/>"
        "+ customerId: Integer [FK]<br/>"
        "+ serviceId: Integer [FK, 0..1]<br/>"
        "+ ratedStar: Integer (1..5)<br/>"
        "+ comments: String<br/>"
        "+ imageUrl: String [0..1]<br/>"
        "+ status: Boolean<br/>"
        "+ createdAt: DateTime"
    )
    cells.append(create_cell("t_fb", t_fb, s_text, parent="c_fb", x=0, y=26, width=240, height=150))

    # 4. Entity: Contact
    c_contact = create_cell("c_contact", "&lt;&lt;entity&gt;&gt;<br/><b>Contact</b>", s_entity, x=780, y=280, width=220, height=170)
    cells.append(c_contact)
    t_contact = (
        "+ id: Integer [PK]<br/>"
        "+ fullName: String<br/>"
        "+ email: String<br/>"
        "+ phone: String<br/>"
        "+ message: String<br/>"
        "+ status: Integer (New/Processing/Resolved)<br/>"
        "+ createdAt: DateTime"
    )
    cells.append(create_cell("t_contact", t_contact, s_text, parent="c_contact", x=0, y=26, width=220, height=140))

    # 5. Controller: DoctorExaminationController
    c_doc_ctrl = create_cell("c_doc_ctrl", "&lt;&lt;controller&gt;&gt;<br/><b>DoctorExaminationController</b>", s_ctrl, x=60, y=40, width=280, height=160)
    cells.append(c_doc_ctrl)
    t_doc_ctrl = (
        "+ AssignedReservations(page): IActionResult<br/>"
        "+ CreateExamination(itemId): IActionResult<br/>"
        "+ SaveExamination(model): Task&lt;IActionResult&gt;<br/>"
        "+ AddPrescription(examId, model): Task&lt;IActionResult&gt;<br/>"
        "+ History(customerId): IActionResult"
    )
    cells.append(create_cell("t_doc_ctrl", t_doc_ctrl, s_text, parent="c_doc_ctrl", x=0, y=26, width=280, height=130))

    # 6. Controller: StaffFeedbacksController
    c_fb_ctrl = create_cell("c_fb_ctrl", "&lt;&lt;controller&gt;&gt;<br/><b>StaffFeedbacksController</b>", s_ctrl, x=60, y=240, width=280, height=130)
    cells.append(c_fb_ctrl)
    t_fb_ctrl = (
        "+ Index(serviceId, star, page): IActionResult<br/>"
        "+ Details(id: int): IActionResult<br/>"
        "+ ToggleStatus(id: int): Task&lt;IActionResult&gt;<br/>"
        "+ ReplyFeedback(id, reply): Task&lt;IActionResult&gt;"
    )
    cells.append(create_cell("t_fb_ctrl", t_fb_ctrl, s_text, parent="c_fb_ctrl", x=0, y=26, width=280, height=100))

    # 7. Controller: ReservationDashboardController
    c_dash_ctrl = create_cell("c_dash_ctrl", "&lt;&lt;controller&gt;&gt;<br/><b>ReservationDashboardController</b>", s_ctrl, x=60, y=400, width=280, height=110)
    cells.append(c_dash_ctrl)
    t_dash_ctrl = (
        "+ Index(fromDate, toDate): IActionResult<br/>"
        "+ GetTrendChartData(days: int): JsonResult"
    )
    cells.append(create_cell("t_dash_ctrl", t_dash_ctrl, s_text, parent="c_dash_ctrl", x=0, y=26, width=280, height=80))

    # 8. ViewModels
    c_vm_dash = create_cell("c_vm_dash", "&lt;&lt;viewModel&gt;&gt;<br/><b>ReservationDashboardViewModel</b>", s_vm, x=440, y=500, width=240, height=160)
    cells.append(c_vm_dash)
    t_vm_dash = (
        "+ totalReservations: Integer<br/>"
        "+ totalRevenue: Decimal<br/>"
        "+ successReservations: Integer<br/>"
        "+ cancelledReservations: Integer<br/>"
        "+ dailyRevenues: List&lt;DailyRevenuePoint&gt;"
    )
    cells.append(create_cell("t_vm_dash", t_vm_dash, s_text, parent="c_vm_dash", x=0, y=26, width=240, height=130))

    # Edges
    cells.append(create_cell("e_exam_pres", "1 .. *", s_assoc, vertex=False, edge=True, source="c_exam", target="c_pres"))
    cells.append(create_cell("e_doc_exam", "creates", s_assoc, vertex=False, edge=True, source="c_doc_ctrl", target="c_exam"))
    cells.append(create_cell("e_fb_ctrl_fb", "manages", s_assoc, vertex=False, edge=True, source="c_fb_ctrl", target="c_fb"))
    cells.append(create_cell("e_dash_vm", "binds", s_assoc, vertex=False, edge=True, source="c_dash_ctrl", target="c_vm_dash"))

    return [("class-diagram-part5", "Class Diagram Part 5 - Medical, Feedback & Dashboard", cells)]

# ==========================================
# 5. DATABASE PHYSICAL ERD
# ==========================================
def generate_database_erd():
    cells = []
    # Styles for table
    s_tbl = "shape=table;startSize=28;container=1;collapsible=0;childLayout=tableLayout;fixedRows=1;rowLines=0;fontStyle=1;align=center;resizeLast=1;fontSize=12;"
    s_row = "shape=tableRow;horizontal=0;startSize=0;swimlaneHead=0;swimlaneBody=0;fillColor=none;collapsible=0;dropTarget=0;points=[[0,0.5],[1,0.5]];portConstraint=eastwest;fontSize=11;top=0;left=0;right=0;bottom=1;"
    s_pk = "shape=partialRectangle;connectable=0;fillColor=none;top=0;left=0;bottom=0;right=1;fontStyle=1;overflow=hidden;fontSize=10;align=center;fontColor=#b85450;"
    s_fk = "shape=partialRectangle;connectable=0;fillColor=none;top=0;left=0;bottom=0;right=1;fontStyle=1;overflow=hidden;fontSize=10;align=center;fontColor=#23445d;"
    s_nn = "shape=partialRectangle;connectable=0;fillColor=none;top=0;left=0;bottom=0;right=1;overflow=hidden;fontSize=10;align=center;"
    s_col = "shape=partialRectangle;connectable=0;fillColor=none;top=0;left=0;bottom=0;right=0;overflow=hidden;fontSize=11;align=left;spacingLeft=5;"
    s_erd_rel = "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;endArrow=ERmany;endFill=0;startArrow=ERone;startFill=0;strokeColor=#4d4d4d;strokeWidth=1.2;"

    def make_table(tbl_id, tbl_name, fill_hex, stroke_hex, cols, x, y, width=220):
        h = 28 + len(cols) * 22
        t_style = f"{s_tbl}fillColor={fill_hex};strokeColor={stroke_hex};"
        t_cell = create_cell(tbl_id, tbl_name, t_style, x=x, y=y, width=width, height=h)
        cells.append(t_cell)
        for i, (ktype, cname, ctype) in enumerate(cols):
            row_id = f"{tbl_id}_r{i}"
            cells.append(create_cell(row_id, None, s_row, parent=tbl_id, x=0, y=28 + i * 22, width=width, height=22))
            k_style = s_pk if ktype == "PK" else (s_fk if ktype == "FK" else s_nn)
            cells.append(create_cell(f"{row_id}_k", ktype, k_style, parent=row_id, x=0, y=0, width=32, height=22))
            cells.append(create_cell(f"{row_id}_c", f"{cname}: {ctype}", s_col, parent=row_id, x=32, y=0, width=width-32, height=22))

    # 1. Users
    u_cols = [
        ("PK", "id", "INT IDENTITY"),
        ("NN", "full_name", "NVARCHAR(150)"),
        ("NN", "email", "NVARCHAR(255) UQ"),
        ("NN", "password_hash", "NVARCHAR(255)"),
        ("", "phone", "VARCHAR(20)"),
        ("", "address", "NVARCHAR(255)"),
        ("FK", "role_id", "INT -> settings"),
        ("NN", "status", "INT"),
        ("", "avatar_url", "NVARCHAR(500)"),
        ("", "email_verified_at", "DATETIME2"),
        ("NN", "created_at", "DATETIME2")
    ]
    make_table("t_users", "users (Tài khoản)", "#dae8fc", "#6c8ebf", u_cols, 40, 40, width=230)

    # 2. Settings
    set_cols = [
        ("PK", "id", "INT IDENTITY"),
        ("NN", "name", "NVARCHAR(100)"),
        ("NN", "value", "NVARCHAR(100)"),
        ("NN", "type", "VARCHAR(50)"),
        ("NN", "order", "INT"),
        ("NN", "is_active", "BIT")
    ]
    make_table("t_settings", "settings (Cấu hình)", "#fff2cc", "#d6b656", set_cols, 40, 350, width=210)

    # 3. RoleMenus
    rm_cols = [
        ("PK", "id", "INT IDENTITY"),
        ("FK", "role_id", "INT -> settings"),
        ("NN", "menu_name", "NVARCHAR(100)"),
        ("NN", "url", "NVARCHAR(255)"),
        ("NN", "sort_order", "INT")
    ]
    make_table("t_role_menus", "role_menus (Menu vai trò)", "#fff2cc", "#d6b656", rm_cols, 40, 540, width=210)

    # 4. Posts
    post_cols = [
        ("PK", "id", "INT IDENTITY"),
        ("NN", "title", "NVARCHAR(200)"),
        ("NN", "thumbnail", "NVARCHAR(255)"),
        ("FK", "category_id", "INT -> settings"),
        ("FK", "author_id", "INT -> users"),
        ("NN", "brief_info", "NVARCHAR(500)"),
        ("NN", "description", "NVARCHAR(MAX)"),
        ("NN", "status", "INT")
    ]
    make_table("t_posts", "posts (Cẩm nang y tế)", "#ffe6cc", "#d79b00", post_cols, 320, 40, width=220)

    # 5. Sliders
    sl_cols = [
        ("PK", "id", "INT IDENTITY"),
        ("NN", "title", "NVARCHAR(200)"),
        ("NN", "image_url", "NVARCHAR(255)"),
        ("NN", "back_link", "NVARCHAR(500)"),
        ("NN", "status", "INT")
    ]
    make_table("t_sliders", "sliders (Banner quảng bá)", "#ffe6cc", "#d79b00", sl_cols, 320, 270, width=210)

    # 6. Services
    srv_cols = [
        ("PK", "id", "INT IDENTITY"),
        ("NN", "title", "NVARCHAR(200)"),
        ("FK", "category_id", "INT -> settings"),
        ("NN", "brief_info", "NVARCHAR(500)"),
        ("NN", "description", "NVARCHAR(MAX)"),
        ("NN", "list_price", "DECIMAL(18,2)"),
        ("NN", "sale_price", "DECIMAL(18,2)"),
        ("NN", "status", "INT"),
        ("NN", "thumbnail_url", "NVARCHAR(500)")
    ]
    make_table("t_services", "services (Gói dịch vụ)", "#d5e8d4", "#82b366", srv_cols, 600, 40, width=230)

    # 7. ServiceImages
    si_cols = [
        ("PK", "id", "INT IDENTITY"),
        ("FK", "service_id", "INT -> services"),
        ("NN", "image_url", "NVARCHAR(500)"),
        ("NN", "sort_order", "INT")
    ]
    make_table("t_service_images", "service_images (Ảnh chi tiết)", "#d5e8d4", "#82b366", si_cols, 600, 310, width=210)

    # 8. Reservations
    res_cols = [
        ("PK", "id", "INT IDENTITY"),
        ("FK", "customer_id", "INT -> users"),
        ("NN", "receiver_name", "NVARCHAR(150)"),
        ("NN", "receiver_mobile", "VARCHAR(20)"),
        ("NN", "receiver_email", "NVARCHAR(255)"),
        ("NN", "status", "INT"),
        ("NN", "total_cost", "DECIMAL(18,2)"),
        ("FK", "assigned_staff_id", "INT -> users"),
        ("NN", "created_at", "DATETIME2")
    ]
    make_table("t_reservations", "reservations (Lịch hẹn khám)", "#e1d5e7", "#9673a6", res_cols, 880, 40, width=230)

    # 9. ReservationItems
    ri_cols = [
        ("PK", "id", "INT IDENTITY"),
        ("FK", "reservation_id", "INT -> reservations"),
        ("FK", "service_id", "INT -> services"),
        ("NN", "quantity", "INT"),
        ("NN", "unit_price", "DECIMAL(18,2)"),
        ("NN", "sub_total", "DECIMAL(18,2)"),
        ("", "examination_date", "DATETIME2")
    ]
    make_table("t_reservation_items", "reservation_items (Chi tiết)", "#e1d5e7", "#9673a6", ri_cols, 880, 310, width=230)

    # 10. PaymentTransactions
    pt_cols = [
        ("PK", "id", "INT IDENTITY"),
        ("FK", "reservation_id", "INT -> reservations"),
        ("NN", "order_code", "NVARCHAR(100) UQ"),
        ("NN", "provider_txn_ref", "NVARCHAR(100)"),
        ("NN", "amount", "DECIMAL(18,2)"),
        ("NN", "payment_method", "VARCHAR(50)"),
        ("NN", "status", "VARCHAR(30)"),
        ("NN", "created_at", "DATETIME2")
    ]
    make_table("t_payments", "payment_transactions (Thanh toán)", "#e1d5e7", "#9673a6", pt_cols, 880, 530, width=230)

    # 11. MedicalExaminations
    me_cols = [
        ("PK", "id", "INT IDENTITY"),
        ("FK", "reservation_item_id", "INT -> res_items"),
        ("FK", "doctor_id", "INT -> users"),
        ("NN", "symptoms", "NVARCHAR(MAX)"),
        ("NN", "diagnosis", "NVARCHAR(MAX)"),
        ("NN", "conclusion", "NVARCHAR(MAX)"),
        ("NN", "examined_at", "DATETIME2"),
        ("", "re_examination_date", "DATETIME2")
    ]
    make_table("t_examinations", "medical_examinations (Khám bệnh)", "#f8cecc", "#b85450", me_cols, 1160, 40, width=240)

    # 12. Prescriptions
    pr_cols = [
        ("PK", "id", "INT IDENTITY"),
        ("FK", "medical_exam_id", "INT -> med_exams"),
        ("NN", "medicine_name", "NVARCHAR(150)"),
        ("NN", "dosage", "NVARCHAR(150)"),
        ("NN", "usage_instructions", "NVARCHAR(500)"),
        ("NN", "quantity", "INT"),
        ("NN", "unit", "NVARCHAR(50)")
    ]
    make_table("t_prescriptions", "prescriptions (Đơn thuốc)", "#f8cecc", "#b85450", pr_cols, 1160, 290, width=230)

    # 13. Feedbacks
    fb_cols = [
        ("PK", "id", "INT IDENTITY"),
        ("FK", "customer_id", "INT -> users"),
        ("FK", "service_id", "INT -> services"),
        ("NN", "rated_star", "INT"),
        ("NN", "comments", "NVARCHAR(1000)"),
        ("NN", "status", "BIT"),
        ("NN", "created_at", "DATETIME2")
    ]
    make_table("t_feedbacks", "feedbacks (Đánh giá)", "#e1d5e7", "#9673a6", fb_cols, 600, 460, width=220)

    # 14. Contacts
    ct_cols = [
        ("PK", "id", "INT IDENTITY"),
        ("NN", "full_name", "NVARCHAR(150)"),
        ("NN", "email", "NVARCHAR(255)"),
        ("NN", "phone", "VARCHAR(20)"),
        ("NN", "message", "NVARCHAR(1000)"),
        ("NN", "status", "INT"),
        ("NN", "created_at", "DATETIME2")
    ]
    make_table("t_contacts", "contacts (Liên hệ)", "#fff2cc", "#d6b656", ct_cols, 320, 440, width=210)

    # Relationships
    cells.append(create_cell("rel_u_set", "", s_erd_rel, vertex=False, edge=True, source="t_settings", target="t_users"))
    cells.append(create_cell("rel_rm_set", "", s_erd_rel, vertex=False, edge=True, source="t_settings", target="t_role_menus"))
    cells.append(create_cell("rel_srv_set", "", s_erd_rel, vertex=False, edge=True, source="t_settings", target="t_services"))
    cells.append(create_cell("rel_post_set", "", s_erd_rel, vertex=False, edge=True, source="t_settings", target="t_posts"))
    cells.append(create_cell("rel_post_user", "", s_erd_rel, vertex=False, edge=True, source="t_users", target="t_posts"))
    cells.append(create_cell("rel_si_srv", "", s_erd_rel, vertex=False, edge=True, source="t_services", target="t_service_images"))
    cells.append(create_cell("rel_res_user", "", s_erd_rel, vertex=False, edge=True, source="t_users", target="t_reservations"))
    cells.append(create_cell("rel_ri_res", "", s_erd_rel, vertex=False, edge=True, source="t_reservations", target="t_reservation_items"))
    cells.append(create_cell("rel_ri_srv", "", s_erd_rel, vertex=False, edge=True, source="t_services", target="t_reservation_items"))
    cells.append(create_cell("rel_pay_res", "", s_erd_rel, vertex=False, edge=True, source="t_reservations", target="t_payments"))
    cells.append(create_cell("rel_me_ri", "", s_erd_rel, vertex=False, edge=True, source="t_reservation_items", target="t_examinations"))
    cells.append(create_cell("rel_pr_me", "", s_erd_rel, vertex=False, edge=True, source="t_examinations", target="t_prescriptions"))
    cells.append(create_cell("rel_fb_srv", "", s_erd_rel, vertex=False, edge=True, source="t_services", target="t_feedbacks"))
    cells.append(create_cell("rel_fb_u", "", s_erd_rel, vertex=False, edge=True, source="t_users", target="t_feedbacks"))

    return [("database-erd", "Database Physical ERD - Microsoft SQL Server", cells)]

# ==========================================
# MAIN EXECUTION
# ==========================================
def main():
    docs_dir = os.path.dirname(os.path.abspath(__file__))

    # 1. Context Diagram
    ctx_diag = generate_context_diagram()
    save_drawio(os.path.join(docs_dir, "ContextDiagram.drawio"), ctx_diag)

    # 2. Use Case Diagrams
    # Guest
    uc_guest_items = [
        ("uc_g1", "Xem Trang chủ & Banner Slider", None),
        ("uc_g2", "Tìm kiếm & Xem Danh sách dịch vụ y tế", None),
        ("uc_g3", "Xem Chi tiết gói dịch vụ y tế", None),
        ("uc_g4", "Tìm kiếm & Đọc Cẩm nang y khoa", None),
        ("uc_g5", "Xem Chi tiết bài viết cẩm nang", None),
        ("uc_g6", "Xem Giỏ dịch vụ & Đặt hẹn khám", None),
        ("uc_g7", "Nhập thông tin người nhận & Hoàn tất", None),
        ("uc_g8", "Gửi Phản hồi / Liên hệ trung tâm", None),
        ("uc_g9", "Đăng ký tài khoản người dùng", None),
        ("uc_g10", "Xác minh Email qua liên kết kích hoạt", None),
        ("uc_g11", "Đăng nhập hệ thống", None),
        ("uc_g12", "Quên mật khẩu / Đặt lại mật khẩu", None)
    ]
    guest_diag = [("uc-guest", "Guest Use Case Diagram", generate_usecase_diagram("Khách vãng lai", "Guest", uc_guest_items, includes=[("inc_g7_g6", "uc_g7", "uc_g6")]))]
    save_drawio(os.path.join(docs_dir, "UseCase_Guest.drawio"), guest_diag)

    # Customer
    uc_cust_items = [
        ("uc_c1", "Đăng nhập / Đăng xuất", None),
        ("uc_c2", "Xem & Cập nhật hồ sơ cá nhân", None),
        ("uc_c3", "Đổi mật khẩu tài khoản", None),
        ("uc_c4", "Xem Danh sách lịch hẹn của tôi (My Reservations)", None),
        ("uc_c5", "Xem Chi tiết thông tin lịch hẹn", None),
        ("uc_c6", "Chỉnh sửa lịch hẹn khám (khi Submitted)", None),
        ("uc_c7", "Hủy lịch hẹn khám (khi Submitted)", None),
        ("uc_c8", "Thanh toán đặt cọc qua cổng VNPay", None),
        ("uc_c9", "Xem Lịch sử khám bệnh & Chẩn đoán của trẻ", None),
        ("uc_c10", "Xem Chi tiết đơn thuốc điều trị của trẻ", None),
        ("uc_c11", "Gửi Phản hồi & Đánh giá dịch vụ đã khám", None)
    ]
    cust_diag = [("uc-customer", "Customer Use Case Diagram", generate_usecase_diagram("Khách hàng / Phụ huynh", "Customer", uc_cust_items, includes=[("inc_c5_c4", "uc_c5", "uc_c4"), ("inc_c10_c9", "uc_c10", "uc_c9")], extends=[("ext_c8_c5", "uc_c8", "uc_c5")]))]
    save_drawio(os.path.join(docs_dir, "UseCase_Customer.drawio"), cust_diag)

    # Staff
    uc_staff_items = [
        ("uc_s1", "Xem Danh sách lịch hẹn phân công (Assigned)", None),
        ("uc_s2", "Lọc & Tìm kiếm lịch hẹn theo ngày / trạng thái", None),
        ("uc_s3", "Xem Chi tiết lịch hẹn & Cập nhật trạng thái", None),
        ("uc_s4", "Tra cứu Danh sách khách hàng & Bệnh nhi", None),
        ("uc_s5", "Xem Chi tiết hồ sơ bệnh sử bệnh nhi", None),
        ("uc_s6", "Tra cứu Danh mục dịch vụ y tế trung tâm", None),
        ("uc_s7", "Xem Danh sách & Chi tiết phản hồi khách hàng", None),
        ("uc_s8", "Ghi nhận kết quả khám bệnh & Chẩn đoán", None),
        ("uc_doc1", "Kê mới đơn thuốc điều trị cho trẻ (Doctor)", "act_doc"),
        ("uc_doc2", "Chỉnh sửa đơn thuốc điều trị (Doctor)", "act_doc"),
        ("uc_doc3", "Kết luận phác đồ điều trị & Hẹn tái khám", "act_doc")
    ]
    staff_inherits = [
        ("act_doc", "Bác sĩ<br/>(Doctor)", 420),
        ("act_nurse", "Y tá / Điều dưỡng<br/>(Nurse)", 550)
    ]
    staff_diag = [("uc-staff", "Staff Use Case Diagram", generate_usecase_diagram("Nhân sự y tế", "Staff", uc_staff_items, inherits=staff_inherits, includes=[("inc_doc3_s8", "uc_doc3", "uc_s8")], extends=[("ext_doc1_s8", "uc_doc1", "uc_s8")]))]
    save_drawio(os.path.join(docs_dir, "UseCase_Staff.drawio"), staff_diag)

    # Manager
    uc_mgr_items = [
        ("uc_m1", "Xem Reservation Dashboard (Thống kê & Xu hướng)", None),
        ("uc_m2", "Quản lý Lịch hẹn (Xem, Lọc, Phân công ca khám)", None),
        ("uc_m3", "Xem & Chỉnh sửa chi tiết lịch hẹn khám", None),
        ("uc_m4", "Quản lý Gói dịch vụ y tế (Xem, Thêm, Sửa)", None),
        ("uc_m5", "Bật / Tắt trạng thái hiển thị dịch vụ (Ẩn/Hiện)", None),
        ("uc_m6", "Quản lý Bài viết cẩm nang y tế (Danh sách, Thêm, Sửa)", None),
        ("uc_m7", "Quản lý Banner Slider (Danh sách, Thêm, Sửa)", None),
        ("uc_m8", "Quản lý Khách hàng & Liên hệ (Contacts)", None),
        ("uc_m9", "Quản lý Phản hồi / Đánh giá (Duyệt feedback)", None)
    ]
    mgr_diag = [("uc-manager", "Manager Use Case Diagram", generate_usecase_diagram("Quản lý trung tâm", "Manager", uc_mgr_items, includes=[("inc_m3_m2", "uc_m3", "uc_m2")], extends=[("ext_m5_m4", "uc_m5", "uc_m4")]))]
    save_drawio(os.path.join(docs_dir, "UseCase_Manager.drawio"), mgr_diag)

    # Admin
    uc_adm_items = [
        ("uc_a1", "Xem Admin Dashboard (Thống kê tài khoản, Role)", None),
        ("uc_a2", "Quản lý Người dùng (Users List: Tìm kiếm, Lọc)", None),
        ("uc_a3", "Xem Chi tiết tài khoản người dùng", None),
        ("uc_a4", "Thêm mới tài khoản nhân sự / Bác sĩ / Quản lý", None),
        ("uc_a5", "Cập nhật thông tin & Phân quyền vai trò (Role)", None),
        ("uc_a6", "Kích hoạt / Khóa tài khoản người dùng (Status)", None),
        ("uc_a7", "Quản lý Cài đặt hệ thống (Settings List)", None),
        ("uc_a8", "Xem & Cập nhật chi tiết cấu hình (Setting Details)", None),
        ("uc_a9", "Thêm mới cấu hình danh mục (Categories, Roles)", None)
    ]
    adm_diag = [("uc-admin", "Admin Use Case Diagram", generate_usecase_diagram("Quản trị viên", "Admin", uc_adm_items, includes=[("inc_a3_a2", "uc_a3", "uc_a2"), ("inc_a8_a7", "uc_a8", "uc_a7")], extends=[("ext_a5_a3", "uc_a5", "uc_a3"), ("ext_a6_a3", "uc_a6", "uc_a3")]))]
    save_drawio(os.path.join(docs_dir, "UseCase_Admin.drawio"), adm_diag)

    # Multi-tab UseCaseDiagrams.drawio
    all_usecase_pages = [
        guest_diag[0],
        cust_diag[0],
        staff_diag[0],
        mgr_diag[0],
        adm_diag[0]
    ]
    save_drawio(os.path.join(docs_dir, "UseCaseDiagrams.drawio"), all_usecase_pages)

    # 3. Class Diagram Part 4
    p4_diag = generate_class_diagram_part4()
    save_drawio(os.path.join(docs_dir, "ClassDiagram_Part4_Reservations.drawio"), p4_diag)

    # 4. Class Diagram Part 5
    p5_diag = generate_class_diagram_part5()
    save_drawio(os.path.join(docs_dir, "ClassDiagram_Part5_Medical_Feedback_Dashboard.drawio"), p5_diag)

    # 5. Database ERD
    erd_diag = generate_database_erd()
    save_drawio(os.path.join(docs_dir, "Database_ERD_ChildrenCare.drawio"), erd_diag)

    print("ALL DRAW.IO FILES SUCCESSFULLY GENERATED!")

if __name__ == "__main__":
    main()
