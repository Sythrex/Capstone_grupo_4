(function () {
    $(document).ready(function () {
        var originalData = [];
        var cambios = [];
        var isEditMode = false;
        var table;

        function initViewTable(dataSource) {
            return $('#inventarioTable').DataTable({
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
                                '<input type="number" class="form-control stockDisp no-spin" value="' + row.stockDisponible + '" min="0" data-original="' + row.stockDisponible + '" data-id="' + row.repuestoUnidadesId + '">' +
                                '<button class="btn btn-outline-secondary incrementDisp" data-id="' + row.repuestoUnidadesId + '">+</button>' +
                                '</div><span class="variacion ms-2"></span>';
                        }
                    },
                    {
                        data: null,
                        render: function (data, type, row) {
                            return '<div class="input-group input-group-sm">' +
                                '<button class="btn btn-outline-secondary decrementRes" data-id="' + row.repuestoUnidadesId + '">-</button>' +
                                '<input type="number" class="form-control stockRes no-spin" value="' + row.stockReservado + '" min="0" data-original="' + row.stockReservado + '" data-id="' + row.repuestoUnidadesId + '">' +
                                '<button class="btn btn-outline-secondary incrementRes" data-id="' + row.repuestoUnidadesId + '">+</button>' +
                                '</div><span class="variacion ms-2"></span>';
                        }
                    },
                    {
                        data: null,
                        render: function (data, type, row) {
                            return '<input type="number" class="form-control form-control-sm precioUnit no-spin" value="' + row.precioUnitario + '" min="0" data-original="' + row.precioUnitario + '" data-id="' + row.repuestoUnidadesId + '">' +
                                '<span class="variacion ms-2"></span>';
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
        });

        function updateVariacion(input) {
            var original = parseInt(input.data('original')) || 0;
            var current = parseInt(input.val()) || 0;
            var diff = current - original;
            var span;

            if (input.hasClass('stockDisp') || input.hasClass('stockRes')) {
                span = input.closest('.input-group').next('.variacion');
            } else if (input.hasClass('precioUnit')) {
                span = input.next('.variacion');
                if (diff > 0) {
                    span.text('+' + diff.toLocaleString('es-CL', { style: 'currency', currency: 'CLP', maximumFractionDigits: 0 })).removeClass('text-danger').addClass('text-success');
                } else if (diff < 0) {
                    span.text(diff.toLocaleString('es-CL', { style: 'currency', currency: 'CLP', maximumFractionDigits: 0 })).removeClass('text-success').addClass('text-danger');
                } else {
                    span.text('');
                }
                return;
            }

            if (diff > 0) {
                span.text('+' + diff).removeClass('text-danger').addClass('text-success');
            } else if (diff < 0) {
                span.text(diff).removeClass('text-success').addClass('text-danger');
            } else {
                span.text('');
            }
        }

        function preventNonNumeric(e) {
            var key = e.which || e.keyCode;
            if (!((key >= 48 && key <= 57) || key == 8 || key == 46 || key == 9 || key == 13 || (key >= 37 && key <= 40) || key == 45)) {
                e.preventDefault();
            }
        }

        function resetEditMode() {
            isEditMode = false;
            $('#guardarCambios, #cancelarEdicion').hide();
            $('#editarStockGlobal').show();
            table.destroy();
            table = $('#inventarioTable').DataTable({
                ajax: {
                    url: '/Inventario/GetInventarioData?categoriaId=' + ($('#categoriaSelect').val() || ''),
                    dataSrc: ''
                },
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
            cambios = [];
            originalData = [];
        }

        $('#cancelarEdicion').click(function () {
            resetEditMode();
        });

        $(document).on('keypress', '.stockDisp, .stockRes, .precioUnit', preventNonNumeric);

        $(document).on('input', '.stockDisp, .stockRes, .precioUnit', function () {
            var input = $(this);
            var val = input.val();
            if (isNaN(val) || (input.hasClass('precioUnit') && parseFloat(val) < 0)) {
                alert('Por favor, ingrese solo valores numéricos válidos (positivos para precio).');
                input.val(input.data('original'));
            } else {
                updateVariacion(input);
            }
        });

        $(document).on('click', '.incrementDisp, .decrementDisp, .incrementRes, .decrementRes', function () {
            var isInc = $(this).hasClass('incrementDisp') || $(this).hasClass('incrementRes');
            var isDisp = $(this).hasClass('incrementDisp') || $(this).hasClass('decrementDisp');
            var input = $(this).siblings(isDisp ? '.stockDisp' : '.stockRes');
            var oldVal = parseInt(input.val()) || 0;
            var newVal = isInc ? oldVal + 1 : oldVal - 1;
            if (newVal < 0) return;
            if (!isDisp && isInc) {
                var currentDisp = parseInt(input.closest('tr').find('.stockDisp').val()) || 0;
                if (currentDisp === 0) {
                    alert('No hay stock disponible para aumentar el reservado.');
                    return;
                }
            }
            input.val(newVal);
            updateCambios(input);
            updateVariacion(input);
        });

        $(document).on('change', '.stockDisp, .stockRes, .precioUnit', function () {
            var val = parseInt($(this).val()) || 0;
            if (val < 0) {
                $(this).val(0);
                return;
            }
            if ($(this).hasClass('stockRes')) {
                var originalRes = parseInt($(this).data('original')) || 0;
                var currentDisp = parseInt($(this).closest('tr').find('.stockDisp').val()) || 0;
                if (val > originalRes && currentDisp === 0) {
                    alert('No hay stock disponible para aumentar el reservado.');
                    $(this).val(originalRes);
                    return;
                }
            }
            updateCambios($(this));
            updateVariacion($(this));
        });

        function updateCambios(input) {
            var id = input.data('id');
            var cambio = cambios.find(c => c.repuestoUnidadesId === id) || { repuestoUnidadesId: id, variacionDisp: 0, variacionRes: 0, nuevoPrecio: null };
            var original = parseInt(input.data('original')) || 0;
            var current = parseInt(input.val()) || 0;
            if (input.hasClass('stockDisp')) {
                cambio.variacionDisp = current - original;
            } else if (input.hasClass('stockRes')) {
                cambio.variacionRes = current - original;
            } else if (input.hasClass('precioUnit')) {
                cambio.nuevoPrecio = current !== original ? current : null;
            }
            if (cambio.variacionDisp !== 0 || cambio.variacionRes !== 0 || cambio.nuevoPrecio !== null) {
                var index = cambios.findIndex(c => c.repuestoUnidadesId === id);
                if (index > -1) {
                    cambios[index] = cambio;
                } else {
                    cambios.push(cambio);
                }
            } else {
                cambios = cambios.filter(c => c.repuestoUnidadesId !== id);
            }
        }

        $('#guardarCambios').click(function () {
            if (cambios.length === 0) {
                alert('No hay cambios para guardar.');
                return;
            }
            $('#notaBatchModal').modal('show');
        });

        $('#confirmarGuardar').click(function () {
            var nota = $('#notaBatch').val();
            var data = { cambios: cambios, nota: nota };
            $.post('/Inventario/ActualizarStocksBatch', data, function (response) {
                if (response.success) {
                    $('#notaBatchModal').modal('hide');
                    resetEditMode();
                } else {
                    alert(response.message);
                }
            }).fail(function () {
                alert('Error al guardar.');
            });
        });

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
                                if (row.asignado) {
                                    return '<button class="btn btn-secondary btn-sm" disabled>Agregado</button>';
                                } else if (selectedIds.includes(row.id)) {
                                    return '<button class="btn btn-danger btn-sm eliminarRepuesto" data-id="' + row.id + '">Eliminar</button>';
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
            var repuestosTable = $('#repuestosTable').DataTable();
            var row = button.closest('tr');
            var rowData = repuestosTable.row(row).data();

            if (!selectedIds.includes(id) && !rowData.asignado) {
                selectedIds.push(id);
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