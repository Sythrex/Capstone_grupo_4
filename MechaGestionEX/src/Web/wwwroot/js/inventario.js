$(document).ready(function () {

    var selectedIds = [];

    $('input[name="tipoAgregar"]').change(function () {
        var tipo = $(this).val();
        if (tipo === 'existente') {
            $('#existenteForm').show();
            $('#nuevoForm').hide();
            $('#guardarAgregar').show();
        } else {
            $('#existenteForm').hide();
            $('#nuevoForm').show();
            $('#guardarAgregar').show();
        }
    });

    $('#agregarRepuestoModal').on('shown.bs.modal', function () {
        var tipo = $('input[name="tipoAgregar"]:checked').val();
        if (tipo === 'existente' && !$.fn.DataTable.isDataTable('#repuestosTable')) {
            $('#repuestosTable').DataTable({
                ajax: {
                    url: '/Inventario/GetRepuestosGlobales',
                    dataSrc: ''
                },
                columns: [
                    { data: 'sku' },
                    { data: 'nombre' },
                    { data: 'marca' },
                    { data: 'categoriaNombre' },
                    {
                        data: null,
                        render: function (data, type, row) {
                            if (row.asignado || selectedIds.includes(row.id)) {
                                return '<button class="btn btn-secondary btn-sm" disabled>Agregado</button>';
                            } else {
                                return '<button class="btn btn-primary btn-sm agregarRepuesto" data-id="' + row.id + '">Agregar</button>';
                            }
                        },
                        orderable: false
                    },
                    {
                        data: 'asignado',
                        visible: false
                    }
                ],
                pageLength: 10,
                searching: true,
                ordering: true,
                order: [[5, 'asc']],
                language: {
                    url: 'https://cdn.datatables.net/plug-ins/2.1.7/i18n/es-ES.json'
                }
            });
        }
    });

    $('#agregarRepuestoModal').on('hidden.bs.modal', function () {
        if ($.fn.DataTable.isDataTable('#repuestosTable')) {
            $('#repuestosTable').DataTable().destroy();
            $('#repuestosTable tbody').empty();
        }
        selectedIds = [];
    });

    $(document).on('click', '.agregarRepuesto', function () {
        var button = $(this);
        var id = button.data('id');
        var table = $('#repuestosTable').DataTable();
        var row = button.closest('tr');
        var rowData = table.row(row).data();

        if (!selectedIds.includes(id) && !rowData.asignado) {
            selectedIds.push(id);
            // Actualizar celda de acción localmente
            table.cell(row, 4).data(rowData).invalidate().draw(false);
        }
    });

    $('#guardarAgregar').click(function () {
        var tipo = $('input[name="tipoAgregar"]:checked').val();
        var data = {};

        if (tipo === 'existente') {
            if (selectedIds.length === 0) {
                alert('No hay repuestos seleccionados para agregar.');
                return;
            }
            data.repuestosIds = selectedIds;
            var url = '/Inventario/AgregarRepuestosBatch'; // Nuevo endpoint para batch
        } else {
            data.repuestoId = 0;
            data.nuevoSku = $('#nuevoSku').val();
            data.nuevoNombre = $('#nuevoNombre').val();
            data.nuevaMarca = $('#nuevaMarca').val();
            data.nuevaCategoriaId = $('#nuevaCategoria').val();
            if (!data.nuevoSku || !data.nuevoNombre) {
                alert('Completa SKU y Nombre.');
                return;
            }
            var url = '/Inventario/AgregarRepuesto';
        }

        $.post(url, data, function (response) {
            if (response.success) {
                $('#agregarRepuestoModal').modal('hide'); // Cierra modal
                location.reload(); // Recarga página para ver cambios
            } else {
                alert(response.message || 'Error al guardar.');
            }
        }).fail(function (xhr, status, error) {
            console.error('Error POST:', error);
            alert('Error en la solicitud: ' + error);
        });
    });

    $('.editarStock').click(function () {
        var id = $(this).data('id');
        $('#editRepuestoId').val(id);
        $('#editarStockModal').modal('show');
    });

    $('#guardarStock').click(function () {
        var data = {
            repuestoUnidadesId: $('#editRepuestoId').val(), // Nota: Debes mapear esto correctamente; asume que pasas unidades.id en data-id si lo cambias
            variacion: $('#variacionStock').val(),
            nota: $('#notaStock').val(),
            nuevoPrecio: $('#nuevoPrecio').val()
        };

        $.post('/Inventario/ActualizarStock', data, function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message);
            }
        });
    });

    $('.verLogs').click(function () {
        var id = $(this).data('id'); // Asume data-id es repuestoUnidadesId
        $.get('/Inventario/VerLogs/' + id, function (data) {
            // Muestra en modal o div
            $('#logsModalBody').html(data); // Agrega un modal para logs
            $('#logsModal').modal('show');
        });
    });

    // Búsqueda client-side (para simplicidad; o recarga con query params)
    $('#searchInput').on('keyup', function () {
        var value = $(this).val().toLowerCase();
        $('table tbody tr').filter(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1);
        });
    });

    // Filtro categoría (recarga página)
    $('#categoriaSelect').change(function () {
        var catId = $(this).val();
        window.location.href = '/Inventario/Index?categoriaId=' + catId;
    });
});