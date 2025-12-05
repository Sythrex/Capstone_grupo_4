(function () {
    $(document).ready(function () {
        var originalData = [];
        var cambios = [];
        var isEditMode = false;
        var table;
        var selectedIds = [];


        function initViewTable(dataSource) {
            return $('#inventarioTable').DataTable({
                responsive: true,
                ajax: dataSource === 'ajax' ? {
                    url: '/Inventario/GetInventarioData?categoriaId=' + ($('#categoriaSelect').val() || ''),
                    dataSrc: ''
                } : null,
                data: dataSource === 'data' ? originalData : null,
                columns: [
                    { data: 'sku' },
                    { data: 'nombre' },
                    { data: 'marca' },
                    { data: 'categoriaNombre' },
                    { data: 'stockDisponible' },
                    { data: 'stockReservado' },
                    {
                        data: 'precioUnitario',
                        render: function (data) {
                            return data.toLocaleString('es-CL', { style: 'currency', currency: 'CLP', maximumFractionDigits: 0 });
                        }
                    },
                    {
                        data: null,
                        render: function (data, type, row) {
                            return '<a href="/Inventario/Detalles/' + row.repuestoUnidadesId + '" class="btn btn-info btn-sm">Ver</a>';
                        },
                        orderable: false
                    }
                ],
                rowCallback: function (row, data) {
                    if (data.stockDisponible < 5) {
                        $(row).addClass('table-danger');
                    }
                },
                pageLength: 10,
                searching: true,
                ordering: true,
                language: {
                    url: 'https://cdn.datatables.net/plug-ins/2.1.7/i18n/es-ES.json'
                }
            });
        }

        table = initViewTable('ajax');

        $('#editarStockGlobal').click(function () {
            if (isEditMode) return;
            isEditMode = true;
            originalData = table.data().toArray();
            cambios = [];
            $('#guardarCambios, #cancelarEdicion').show();
            $('#editarStockGlobal').hide();

            table.destroy();
            table = $('#inventarioTable').DataTable({
                responsive: true,
                data: originalData,
                columns: [
                    { data: 'sku' },
                    { data: 'nombre' },
                    { data: 'marca' },
                    { data: 'categoriaNombre' },
                    {
                        data: null,
                        render: function (data, type, row) {
                            return '<div class="input-group input-group-sm">' +
                                '<button class="btn btn-outline-secondary decrementDisp" data-id="' + row.repuestoUnidadesId + '">-</button>' +
                                '<input type="number" class="form-control text-center stockDispInput" value="' + row.stockDisponible + '" data-id="' + row.repuestoUnidadesId + '">' +
                                '<button class="btn btn-outline-secondary incrementDisp" data-id="' + row.repuestoUnidadesId + '">+</button>' +
                                '</div>';
                        },
                        orderable: false
                    },
                    {
                        data: null,
                        render: function (data, type, row) {
                            return '<div class="input-group input-group-sm">' +
                                '<button class="btn btn-outline-secondary decrementRes" data-id="' + row.repuestoUnidadesId + '">-</button>' +
                                '<input type="number" class="form-control text-center stockResInput" value="' + row.stockReservado + '" data-id="' + row.repuestoUnidadesId + '">' +
                                '<button class="btn btn-outline-secondary incrementRes" data-id="' + row.repuestoUnidadesId + '">+</button>' +
                                '</div>';
                        },
                        orderable: false
                    },
                    {
                        data: 'precioUnitario',
                        render: function (data, type, row) {
                            return '<input type="number" class="form-control form-control-sm precioInput" value="' + data + '" data-id="' + row.repuestoUnidadesId + '">';
                        },
                        orderable: false
                    },
                    {
                        data: null,
                        render: function (data, type, row) {
                            return '<a href="/Inventario/Detalles/' + row.repuestoUnidadesId + '" class="btn btn-info btn-sm">Ver</a>';
                        },
                        orderable: false
                    }
                ],
                rowCallback: function (row, data) {
                    if (data.stockDisponible < 5) {
                        $(row).addClass('table-danger');
                    }
                },
                pageLength: 10,
                searching: true,
                ordering: true,
                language: {
                    url: 'https://cdn.datatables.net/plug-ins/2.1.7/i18n/es-ES.json'
                }
            });
        });

        $('#cancelarEdicion').click(function () {
            isEditMode = false;
            cambios = [];
            $('#guardarCambios, #cancelarEdicion').hide();
            $('#editarStockGlobal').show();

            table.destroy();
            table = initViewTable('data');
        });

        $('#guardarCambios').click(function () {
            if (cambios.length > 0) {
                $('#notaBatchModal').modal('show');
            } else {
                alert('No hay cambios para guardar.');
            }
        });

        $('#confirmarGuardarBatch').click(function () {
            var nota = $('#notaBatch').val();
            var data = { cambios: cambios, nota: nota };
            $.post('/Inventario/ActualizarStocksBatch', data, function (response) {
                if (response.success) {
                    $('#notaBatchModal').modal('hide');
                    location.reload();
                } else {
                    alert(response.message);
                }
            });
        });

        $(document).on('click', '.incrementDisp, .decrementDisp, .incrementRes, .decrementRes', function () {
            var id = $(this).data('id');
            var isDisp = $(this).hasClass('incrementDisp') || $(this).hasClass('decrementDisp');
            var input = isDisp ? $('.stockDispInput[data-id="' + id + '"]') : $('.stockResInput[data-id="' + id + '"]');
            var val = parseInt(input.val());
            var change = $(this).hasClass('incrementDisp') || $(this).hasClass('incrementRes') ? 1 : -1;
            input.val(val + change);
            trackChange(id, isDisp ? 'disp' : 'res', val + change);
        });

        $(document).on('change', '.stockDispInput, .stockResInput, .precioInput', function () {
            var id = $(this).data('id');
            var type = $(this).hasClass('stockDispInput') ? 'disp' : ($(this).hasClass('stockResInput') ? 'res' : 'precio');
            trackChange(id, type, parseInt($(this).val()));
        });

        function trackChange(id, type, newVal) {
            var cambio = cambios.find(c => c.id === id) || { id: id, disp: null, res: null, precio: null };
            if (type === 'disp') cambio.disp = newVal;
            if (type === 'res') cambio.res = newVal;
            if (type === 'precio') cambio.precio = newVal;
            if (!cambios.find(c => c.id === id)) cambios.push(cambio);
        }

        $('input[name="tipoAgregar"]').change(function () {
            if ($(this).val() === 'nuevo') {
                $('#nuevoRepuestoForm').show();
                $('#existenteRepuestoForm').hide();
            } else {
                $('#nuevoRepuestoForm').hide();
                $('#existenteRepuestoForm').show();
                if (!$.fn.DataTable.isDataTable('#repuestosTable')) {
                    var repuestosTable = $('#repuestosTable').DataTable({
                        responsive: true,
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
                                    return row.asignado ? '<button class="btn btn-danger btn-sm eliminarRepuesto" data-id="' + row.id + '">Eliminar</button>' :
                                        '<button class="btn btn-primary btn-sm agregarRepuesto" data-id="' + row.id + '">Agregar</button>';
                                }
                            }
                        ]
                    });
                }
            }
        });

        $('#agregarRepuestoModal').on('hidden.bs.modal', function () {
            $('#nuevoSku, #nuevoNombre, #nuevaMarca').val('');
            $('#nuevaCategoria').val('');
            if ($.fn.DataTable.isDataTable('#repuestosTable')) {
                $('#repuestosTable').DataTable().destroy();
                $('#repuestosTable tbody').empty();
            }
            selectedIds = [];
        });

        $(document).on('click', '.agregarRepuesto', function () {
            var button = $(this);
            var id = button.data('id');
            var repuestosTable = $('#repuestosTable').DataTable();
            var row = button.closest('tr');
            var rowData = repuestosTable.row(row).data();

            if (!selectedIds.includes(id) && !rowData.asignado) {
                selectedIds.push(id);
                rowData.asignado = true;
                repuestosTable.cell(row, 4).data(rowData).invalidate().draw(false);
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
                var url = '/Inventario/AgregarRepuestosBatch';
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
                    $('#agregarRepuestoModal').modal('hide');
                    location.reload();
                } else {
                    alert(response.message || 'Error al guardar.');
                }
            }).fail(function (xhr, status, error) {
                console.error('Error POST:', error);
                alert('Error en la solicitud: ' + error);
            });
        });

        $(document).on('click', '.eliminarRepuesto', function () {
            var button = $(this);
            var id = button.data('id');
            var repuestosTable = $('#repuestosTable').DataTable();
            var row = button.closest('tr');
            var rowData = repuestosTable.row(row).data();

            var index = selectedIds.indexOf(id);
            if (index > -1) {
                selectedIds.splice(index, 1);
                rowData.asignado = false; 
                repuestosTable.cell(row, 4).data(rowData).invalidate().draw(false);
            }
        });


        $('.verLogs').click(function () {
            var id = $(this).data('id');
            $.get('/Inventario/VerLogs/' + id, function (data) {
                $('#logsModalBody').html(data);
                $('#logsModal').modal('show');
            });
        });
    });
})();