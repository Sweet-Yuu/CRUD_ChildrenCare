namespace CRUD_ChildrenCare.Services.Email;

public sealed record EmailMessage(string To, string Subject, string Body, bool IsHtml = true);
