$(document).ready(function () {
    $('.select2').select2({
        ajax: {
            url: '/Inventario/GetRepuestosGlobales',
            dataType: 'json',
            delay: 250,
            data: function (params) { return { search: params.term }; },
            processResults: function (data) { return { results: data }; },
            cache: true
        }
    });

    $('input[name="tipoAgregar"]').change(function () {
        if ($(this).val() === 'existente') {
            $('#existenteForm').show();
            $('#nuevoForm').hide();
        } else {
            $('#existenteForm').hide();
            $('#nuevoForm').show();
        }
    });

    $('#guardarAgregar').click(function () {
        var tipo = $('input[name="tipoAgregar"]:checked').val();
        var data = {};
        if (tipo === 'existente') {
            data.repuestoId = $('#repuestoSelect').val();
        } else {
            data.repuestoId = 0;
            data.nuevoSku = $('#nuevoSku').val();
            data.nuevoNombre = $('#nuevoNombre').val();
            data.nuevaMarca = $('#nuevaMarca').val();
            data.nuevaCategoriaId = $('#nuevaCategoria').val();
        }

        $.post('/Inventario/AgregarRepuesto', data, function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message);
            }
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