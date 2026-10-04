// Pantalla de venta: búsqueda por código, detalle y cálculos en pantalla.
(() => {
    const root = document.getElementById('pos');
    const IVA = parseFloat(root.dataset.iva);
    const csrf = document.querySelector('meta[name="csrf-token"]').content;
    const AJAX = { 'X-Requested-With': 'XMLHttpRequest' };

    const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });
    const round2 = n => Math.round((n + Number.EPSILON) * 100) / 100;

    const $ = id => document.getElementById(id);
    const inputCodigo = $('codigo');
    const body = $('detalle-body');
    const btnRegistrar = $('btn-registrar');
    const btnLimpiar = $('btn-limpiar');
    const modalComprobante = new bootstrap.Modal($('modalComprobante'));

    let lineas = [];          // { id, codigo, nombre, precio, stock, cantidad }
    let ocupado = false;

    // ---------- Utilidades ----------
    const enfocar = () => setTimeout(() => inputCodigo.focus(), 0);

    /** Lee la respuesta JSON; si la sesión venció (401) redirige al login. */
    async function leer(resp) {
        if (resp.status === 401) {
            notify('warning', 'Su sesión expiró. Inicie sesión nuevamente.');
            setTimeout(() => (location.href = '/Account/Login'), 1500);
            return { success: false, handled: true };
        }
        try {
            return await resp.json();
        } catch {
            return { success: false, message: 'Respuesta inesperada del servidor.' };
        }
    }

    function mostrarError(j) {
        if (j.handled) return;
        notify('warning', j.message ?? 'No se pudo completar la operación.', (j.errors ?? []).join(' · '));
    }

    // ---------- Render ----------
    function render() {
        body.replaceChildren();

        if (lineas.length === 0) {
            const tr = document.createElement('tr');
            tr.innerHTML = '<td colspan="6" class="text-center text-muted py-5"><i class="bi bi-cart3 fs-2 d-block mb-2"></i>Busque un producto por código para agregarlo al detalle.</td>';
            body.appendChild(tr);
        } else {
            lineas.forEach(l => body.appendChild(crearFila(l)));
        }

        const articulos = lineas.reduce((s, l) => s + l.cantidad, 0);
        const subtotal = round2(lineas.reduce((s, l) => s + l.precio * l.cantidad, 0));
        const iva = round2(subtotal * IVA);
        const total = round2(subtotal + iva);

        $('r-articulos').textContent = articulos;
        $('r-subtotal').textContent = money.format(subtotal);
        $('r-iva').textContent = money.format(iva);
        $('r-total').textContent = money.format(total);

        btnRegistrar.disabled = ocupado || lineas.length === 0;
        btnLimpiar.disabled = ocupado || lineas.length === 0;
    }

    function crearFila(l) {
        const tr = document.createElement('tr');
        tr.dataset.id = l.id;
        // Los textos del producto se asignan con textContent (sin innerHTML) para evitar inyección.
        tr.innerHTML = `
            <td class="fw-semibold"></td>
            <td><div class="nombre"></div><div class="small text-muted stock"></div></td>
            <td class="text-end precio"></td>
            <td class="text-center">
                <div class="input-group input-group-sm qty">
                    <button class="btn btn-outline-secondary" type="button" data-act="menos"><i class="bi bi-dash"></i></button>
                    <input type="number" class="form-control text-center" min="1" max="${l.stock}" value="${l.cantidad}" />
                    <button class="btn btn-outline-secondary" type="button" data-act="mas"><i class="bi bi-plus"></i></button>
                </div>
            </td>
            <td class="text-end fw-semibold subtotal"></td>
            <td class="text-end"><button class="btn btn-sm btn-outline-danger" type="button" data-act="quitar"><i class="bi bi-trash"></i></button></td>`;
        tr.children[0].textContent = l.codigo;
        tr.querySelector('.nombre').textContent = l.nombre;
        tr.querySelector('.stock').textContent = `Stock disponible: ${l.stock}`;
        tr.querySelector('.precio').textContent = money.format(l.precio);
        tr.querySelector('.subtotal').textContent = money.format(l.precio * l.cantidad);
        return tr;
    }

    // ---------- Operaciones sobre el detalle ----------
    function agregar(p) {
        if (p.stock <= 0) {
            notify('warning', `"${p.nombre}" no tiene stock disponible.`);
            return;
        }
        const existente = lineas.find(l => l.id === p.id);
        if (existente) {
            if (existente.cantidad + 1 > p.stock) {
                notify('warning', `Stock máximo alcanzado para "${p.nombre}" (${p.stock}).`);
                return;
            }
            existente.cantidad++;
        } else {
            lineas.push({ id: p.id, codigo: p.codigo, nombre: p.nombre, precio: p.precio, stock: p.stock, cantidad: 1 });
        }
        render();
    }

    function fijarCantidad(linea, valor) {
        let cant = parseInt(valor, 10);
        if (isNaN(cant) || cant < 1) cant = 1;
        if (cant > linea.stock) {
            cant = linea.stock;
            notify('warning', `Stock máximo para "${linea.nombre}": ${linea.stock}.`);
        }
        linea.cantidad = cant;
        render();
    }

    // ---------- Eventos ----------
    $('form-buscar').addEventListener('submit', async e => {
        e.preventDefault();
        const codigo = inputCodigo.value.trim();
        if (!codigo || ocupado) return;

        ocupado = true;
        try {
            const resp = await fetch(`/Ventas/BuscarProducto?codigo=${encodeURIComponent(codigo)}`, { headers: AJAX });
            const j = await leer(resp);
            if (resp.ok && j.success) {
                agregar(j.data);
                inputCodigo.value = '';
            } else {
                mostrarError(j);
            }
        } catch {
            notify('error', 'No se pudo conectar con el servidor.');
        } finally {
            ocupado = false;
            render();
            enfocar();
        }
    });

    body.addEventListener('click', e => {
        const btn = e.target.closest('[data-act]');
        if (!btn) return;
        const linea = lineas.find(l => l.id === parseInt(btn.closest('tr').dataset.id, 10));
        if (!linea) return;

        if (btn.dataset.act === 'quitar') { lineas = lineas.filter(l => l !== linea); render(); }
        if (btn.dataset.act === 'mas') fijarCantidad(linea, linea.cantidad + 1);
        if (btn.dataset.act === 'menos') fijarCantidad(linea, linea.cantidad - 1);
    });

    body.addEventListener('change', e => {
        if (!e.target.matches('input[type="number"]')) return;
        const linea = lineas.find(l => l.id === parseInt(e.target.closest('tr').dataset.id, 10));
        if (linea) fijarCantidad(linea, e.target.value);
    });

    btnLimpiar.addEventListener('click', () => { lineas = []; render(); enfocar(); });

    btnRegistrar.addEventListener('click', async () => {
        if (ocupado || lineas.length === 0) return;
        ocupado = true;
        btnRegistrar.disabled = true;
        btnRegistrar.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Registrando...';

        try {
            const resp = await fetch('/Ventas/Registrar', {
                method: 'POST',
                headers: { ...AJAX, 'Content-Type': 'application/json', RequestVerificationToken: csrf },
                body: JSON.stringify({ items: lineas.map(l => ({ productoId: l.id, cantidad: l.cantidad })) })
            });
            const j = await leer(resp);

            if (resp.ok && j.success) {
                notify('success', j.message);
                lineas = [];

                // Comprobante con los totales oficiales calculados por el servidor
                const det = await fetch(`/Ventas/Detalle/${j.data.id}`, { headers: AJAX });
                if (det.ok) {
                    $('comprobante-body').innerHTML = await det.text(); // HTML generado por Razor (ya codificado)
                    modalComprobante.show();
                }
            } else {
                mostrarError(j);
            }
        } catch {
            notify('error', 'No se pudo conectar con el servidor.');
        } finally {
            ocupado = false;
            btnRegistrar.innerHTML = '<i class="bi bi-check-lg"></i> Registrar venta';
            render();
        }
    });

    $('modalComprobante').addEventListener('hidden.bs.modal', enfocar);

    render();
})();
