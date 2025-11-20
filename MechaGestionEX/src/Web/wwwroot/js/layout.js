$(document).ready(function () {
    $('#tallerSelect').select2({
        theme: 'bootstrap-5',
        width: '100%',
        placeholder: 'Seleccione taller',
        allowClear: false,
        ajax: {
            url: '/Account/GetTalleresAsignados',
            dataType: 'json',
            delay: 250,
            data: function (params) {
                return { search: params.term };
            },
            processResults: function (data) {
                return { results: data };
            }
        }
    });

    $.get('/Account/GetTallerActivo', function (data) {
        if (data.id) {
            var option = new Option(data.text, data.id, true, true);
            $('#tallerSelect').append(option).trigger('change');
        }
    });

    $('#tallerSelect').on('select2:select', function (e) {
        var tallerId = e.params.data.id;
        console.log("seleccionamos");
        $.post('/Account/ChangeTaller', { tallerId: tallerId }, function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message);
            }
        });
    });
});