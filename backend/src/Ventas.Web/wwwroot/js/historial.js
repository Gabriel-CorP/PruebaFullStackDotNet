// Historial de ventas: detalle en modal (vista parcial renderizada por el servidor)
(() => {
    const modal = new bootstrap.Modal(document.getElementById('modalDetalle'));
    const cuerpo = document.getElementById('detalle-modal-body');

    document.querySelectorAll('[data-detalle]').forEach(btn => {
        btn.addEventListener('click', async () => {
            try {
                const resp = await fetch(`/Ventas/Detalle/${btn.dataset.detalle}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                if (resp.status === 401) { location.href = '/Account/Login'; return; }
                if (!resp.ok) {
                    const j = await resp.json().catch(() => ({}));
                    notify('warning', j.message ?? 'No se pudo cargar el detalle de la venta.');
                    return;
                }
                cuerpo.innerHTML = await resp.text();
                modal.show();
            } catch {
                notify('error', 'No se pudo conectar con el servidor.');
            }
        });
    });
})();
