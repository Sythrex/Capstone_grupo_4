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

    var comunasByRegion = window.comunasByRegion || {};
    var currentRegionId = window.currentRegionId || 0;
    var currentComunaId = window.currentComunaId || 0;

    var regionSelect = $('#regionSelect');
    var comunaSelect = $('#comunaSelect');

    function populateComunas(regionId) {
        comunaSelect.empty().append('<option value="">Seleccione una comuna</option>');
        comunaSelect.prop('disabled', true);
        comunaSelect.val(null).trigger('change.select2');

        if (regionId && comunasByRegion[regionId] && comunasByRegion[regionId].length > 0) {
            $.each(comunasByRegion[regionId], function (i, comuna) {
                comunaSelect.append('<option value="' + comuna.id + '">' + comuna.nombre + '</option>');
            });
            comunaSelect.prop('disabled', false).trigger('change.select2');
        } else {
            comunaSelect.append('<option value="" disabled>No hay comunas disponibles</option>');
        }
    }
    regionSelect.on('select2:select', function (e) {
        var regionId = parseInt(e.params.data.id) || 0;
        populateComunas(regionId);
    });

    regionSelect.on('change', function () {
        if (!$(this).hasClass('select2-hidden-accessible')) return;
        var regionId = parseInt($(this).val()) || 0;
        populateComunas(regionId);
    });

    if (currentRegionId > 0) {
        regionSelect.val(currentRegionId).trigger('change.select2');
        setTimeout(function () {
            if (currentComunaId > 0 && !comunaSelect.prop('disabled')) {
                comunaSelect.val(currentComunaId).trigger('change.select2');
            } else {
            }
        }, 300);
    }

    let clienteId = null;

    $('#checkRutBtn').click(function () {
        const rut = $('#rutInput').val();
        if (!rut) {
            alert('Ingrese un RUT válido.');
            return;
        }

        $.get('/clientes/check-rut', { rut: rut }, function (data) {
            if (!data.exists) {
                alert('No se encontró cliente con este RUT. Puede crear uno nuevo.');
                return;
            }

            clienteId = data.id;
            const details = $('#clienteDetails');
            details.html(`
                    <dt>Nombre</dt><dd>${data.nombre}</dd>
                    <dt>Correo</dt><dd>${data.correo}</dd>
                    <dt>Teléfono</dt><dd>${data.telefono}</dd>
                    <dt>Dirección</dt><dd>${data.direccion}</dd>
                    <dt>Comuna</dt><dd>${data.comuna}</dd>
                    <dt>Región</dt><dd>${data.region}</dd>
                `);
            $('#clienteExistenteModal').modal('show');
        });
    });

    $('#addToTallerBtn').click(function () {
        if (!clienteId) return;

        $.post('/clientes/add-to-taller', { clienteId: clienteId }, function (response) {
            if (response.success) {
                alert('Cliente agregado al taller exitosamente.');
                // Opcional: Redirige o actualiza form con datos del cliente
                window.location.href = '/clientes'; // Ej: redirige a index
            } else {
                alert(response.message || 'Error al agregar.');
            }
            $('#clienteExistenteModal').modal('hide');
        });
    });
});
