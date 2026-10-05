// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.querySelector('[data-admin-menu-toggle]')?.addEventListener('click', () => {
  document.querySelector('.admin-sidebar')?.classList.toggle('is-open');
});

document.querySelectorAll('form').forEach((form) => {
  form.addEventListener('submit', () => {
    if (typeof form.checkValidity === 'function' && !form.checkValidity()) {
      return;
    }

    form.querySelectorAll('button[type="submit"]').forEach((button) => {
      button.disabled = true;
      button.setAttribute('aria-busy', 'true');
    });
  });
});
