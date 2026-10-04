// Notificaciones (toast) con SweetAlert2
const Toast = Swal.mixin({
    toast: true,
    position: 'top-end',
    showConfirmButton: false,
    timer: 4500,
    timerProgressBar: true
});

window.notify = (icon, title, text) => Toast.fire({ icon, title, text });

document.addEventListener('DOMContentLoaded', () => {
    // Mensajes enviados por el servidor (TempData)
    const flash = document.getElementById('flash');
    if (flash) {
        ['success', 'error', 'warning'].forEach(tipo => {
            if (flash.dataset[tipo]) notify(tipo, flash.dataset[tipo]);
        });
    }

    // Confirmación antes de eliminar un producto
    document.querySelectorAll('[data-confirm-delete]').forEach(btn => {
        btn.addEventListener('click', async () => {
            const { isConfirmed } = await Swal.fire({
                title: '¿Está seguro de eliminar este producto?',
                html: `Se eliminará <b></b>. Esta acción no se puede deshacer.`,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Sí, eliminar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#dc2626',
                reverseButtons: true,
                focusCancel: true,
                didOpen: popup => {
                    popup.querySelector('b').textContent = `${btn.dataset.name} (${btn.dataset.code})`; // textContent: evita inyección HTML
                }
            });
            if (isConfirmed) btn.closest('form').submit();
        });
    });
});
