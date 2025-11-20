$(document).ready(function () {
    $.get('/Inventario/VerLogs/' + $('#editRepuestoId').val(), function (data) {
        $('#logsContainer').html(data);
    });

    $('#guardarStockUnitario').click(function () {
        var data = {
            repuestoUnidadesId: $('#editRepuestoId').val(),
            variacionDisp: parseInt($('#variacionDisp').val()) || 0,
            variacionRes: parseInt($('#variacionRes').val()) || 0,
            nuevoPrecio: parseInt($('#nuevoPrecio').val()),
            nota: $('#notaStock').val()
        };
        if (isNaN(data.variacionDisp) || isNaN(data.variacionRes) || (data.nuevoPrecio && isNaN(data.nuevoPrecio))) {
            alert('Todos los valores deben ser numéricos válidos.');
            return;
        }
        $.post('/Inventario/ActualizarStockUnitario', data, function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message);
            }
        });
    });

    $('#eliminarAsignacion').click(function () {
        if (confirm('¿Eliminar este repuesto del inventario?')) {
            $.post('/Inventario/EliminarAsignacion', { repuestoUnidadesId: $('#editRepuestoId').val() }, function (response) {
                if (response.success) {
                    window.location.href = '/Inventario/Index';
                } else {
                    alert(response.message);
                }
            });
        }
    });

    function preventNonNumeric(e) {
        var key = e.which || e.keyCode;
        if (!((key >= 48 && key <= 57) || key == 8 || key == 46 || key == 9 || key == 13 || (key >= 37 && key <= 40) || key == 45)) {
            e.preventDefault();
        }
    }

    $(document).on('keypress', '#variacionDisp, #variacionRes, #nuevoPrecio', preventNonNumeric);

    $(document).on('input', '#variacionDisp, #variacionRes, #nuevoPrecio', function () {
        var input = $(this);
        var val = input.val();
        if (isNaN(val) || (input.attr('id') === 'nuevoPrecio' && parseFloat(val) < 0)) {
            alert('Por favor, ingrese solo valores numéricos válidos (positivos para precio).');
            input.val('');
        }
    });

    $.get('/Inventario/VerLogs/' + $('#editRepuestoId').val(), function (data) {
        $('#logsContainer').html(data);

        $('#logsTable').DataTable({
            searching: false,
            ordering: false,
            info: false,
            pageLength: 10
        });
    });
});