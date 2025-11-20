$(document).ready(function () {

    var select2Init = $('.select2').select2({
        theme: 'bootstrap-5',
        width: '100%',
        placeholder: function () {
            return $(this).data('placeholder') || 'Seleccione...';
        },
        allowClear: true,
        dropdownParent: $('body')
    });

    var currentTipoId = window.currentTipoId || 0; 
    if (currentTipoId > 0) {
        $('#tipoSelect').val(currentTipoId).trigger('change.select2');
    }

    let funcionarioId = null;

    $('#checkRutBtn').click(function () {
        const rut = $('#rutInput').val();  // Asume que tienes <input id="rutInput" asp-for="rut">
        if (!rut) {
            alert('Ingrese un RUT válido.');
            return;
        }

        $.get('/funcionarios/check-rut', { rut: rut }, function (data) {
            if (!data.exists) {
                alert('No se encontró funcionario con este RUT. Puede crear uno nuevo.');
                return;
            }

            funcionarioId = data.id;
            const details = $('#funcionarioDetails');  // Asume modal con id="funcionarioExistenteModal" y dl id="funcionarioDetails"
            details.html(`
                <dt>Nombre</dt><dd>${data.nombre}</dd>
                <dt>Especialidad</dt><dd>${data.especialidad}</dd>
                <dt>Activo</dt><dd>${data.activo ? 'Sí' : 'No'}</dd>
                <dt>Tipo</dt><dd>${data.tipo}</dd>
            `);
            $('#funcionarioExistenteModal').modal('show');
        });
    });

    $('#addToTallerBtn').click(function () {
        if (!funcionarioId) return;

        $.post('/funcionarios/add-to-taller', { funcionarioId: funcionarioId }, function (response) {
            if (response.success) {
                alert('Funcionario agregado al taller exitosamente.');
                window.location.href = '/funcionarios';
            } else {
                alert(response.message || 'Error al agregar.');
            }
            $('#funcionarioExistenteModal').modal('hide');
        });
    });
});
